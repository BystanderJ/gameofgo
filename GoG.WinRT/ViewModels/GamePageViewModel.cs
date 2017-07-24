using GoG.Board;
using GoG.Infrastructure;
using GoG.Infrastructure.Engine;
using GoG.Infrastructure.Services.Engine;
using GoG.WinRT.Services;
using Prism.Commands;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable RedundantCatchClause
// ReSharper disable ExplicitCallerInfoArgument

namespace GoG.WinRT.ViewModels
{
    public class GamePageViewModel : PageViewModel
    {
        #region Data

        private IRepository _repository;
        private bool _showedDeadStoneMessage;
        private GoColor? _savedColor;
        private string _hint;
        private PlayerViewModel[] _players;
        private GoGame _activeGame;
        private Guid _activeGameId;
        
        #endregion Data

        #region Ctor
        public GamePageViewModel(INavigationService navigationService,
            ISessionStateService sessionStateService,
            IGameEngine engine,
            IRepository repository)
            : base(navigationService, sessionStateService, engine)
        {
            _repository = repository;
        }
        #endregion Ctor

        #region Properties

        #region ShowingArea
        private bool _showingArea;
        [RestorableState]
        public bool ShowingArea
        {
            get => _showingArea;
            set
            {
                if (SetProperty(ref _showingArea, value))
                {
                    if (_activeGame != null)
                        _activeGame.ShowingArea = ShowingArea;
                }
            }
        }

        #endregion ShowingArea

        #region MessageText
        private string _messageText;
        public string MessageText
        {
            get => _messageText;
            set
            {
                if (_messageText == value)
                    return;

                _messageText = value;
                RaisePropertyChanged();
            }
        }
        #endregion Message

        #region Status
        private GoGameStatus _status;
        public GoGameStatus Status
        {
            get => _status; set
            {
                if (_status == value)
                    return;
                _status = value;
                RaisePropertyChanged(nameof(Status));
                RaiseCommandsChanged();
                if (value != GoGameStatus.Active)
                    ClearHint();
            }
        }
        #endregion Status

        #region BoardEdgeSize
        private int _boardEdgeSize;
        public int BoardEdgeSize
        {
            get => _boardEdgeSize; set => SetProperty(ref _boardEdgeSize, value);
        }
        #endregion BoardEdgeSize

        #region Pieces
        private Dictionary<string, PieceStateViewModel> _pieces;
        public Dictionary<string, PieceStateViewModel> Pieces
        {
            get => _pieces; set { _pieces = value; RaisePropertyChanged(); }
        }
        #endregion Pieces

        #region CurrentTurnColor
        private GoColor _currentTurnColor = GoColor.Black;
        public GoColor CurrentTurnColor
        {
            get => _currentTurnColor; set => SetProperty(ref _currentTurnColor, value);
        }
        #endregion CurrentTurnColor

        #region History
        private ObservableCollection<GoMoveHistoryItem> _history;
        public ObservableCollection<GoMoveHistoryItem> History
        {
            get => _history; set { _history = value; RaisePropertyChanged(); }
        }
        #endregion History

        #region WhoseTurn
        private int _whoseTurn;
        public int WhoseTurn
        {
            get => _whoseTurn; set
            {
                SetProperty(ref _whoseTurn, value);
                CurrentTurnColor = _players[_whoseTurn].Color;
                RaisePropertyChanged();
            }
        }
        #endregion WhoseTurn

        #region CurrentPlayer
        public PlayerViewModel CurrentPlayer => _players[WhoseTurn];
        #endregion CurrentPlayer

        #region Player1
        private PlayerViewModel _player1;
        public PlayerViewModel Player1
        {
            get => _player1; set => SetProperty(ref _player1, value);
        }
        #endregion Player1

        #region Player2
        private PlayerViewModel _player2;
        public PlayerViewModel Player2
        {
            get => _player2; set => SetProperty(ref _player2, value);
        }
        #endregion Player2

        #endregion Properties

        #region Methods


        #endregion Methods

        #region Commands

        #region GetHintCommand
        DelegateCommand _getHintCommand;
        public DelegateCommand GetHintCommand => _getHintCommand ?? (_getHintCommand = new DelegateCommand(ExecuteGetHint, CanGetHint));

        public bool CanGetHint()
        {
            return IsBusy == false && Status == GoGameStatus.Active;
        }
        public async void ExecuteGetHint()
        {
            ClearHint();

            MessageText = "Getting hint...";
            IsBusy = true;
            var resp = await GameEngine.HintAsync(_activeGameId, _players[WhoseTurn].Color);
            IsBusy = false;
            MessageText = null;

            if (resp.ResultCode == GoResultCode.Success)
            {
                if (resp.Move.MoveType == MoveType.Normal)
                {
                    _hint = resp.Move.Position;
                    if (Pieces.ContainsKey(_hint))
                    {
                        Pieces[_hint].IsHint = true;
                        Pieces[_hint].RaiseMultiplePropertiesChanged();
                    }
                    //InvokeFleetingMessage("How about " + resp.Move.Position + '?');
                }
                else
                    InvokeFleetingMessage("Perhaps you should " + resp.Move.ToString().ToLower() + '.', 1500).Forget();
            }
            else if (resp.ResultCode == GoResultCode.CommunicationError)
                await HandleCommunicationError("Syncronizing...");
            else
                await DisplayErrorCode(resp.ResultCode);

            RaiseCommandsChanged();
        }

        private async Task InvokeFleetingMessage(string msg, int delayMilliseconds)
        {
            MessageText = msg;
            await Task.Delay(delayMilliseconds);
            MessageText = null;
        }

        private void ClearHint()
        {
            if (_hint != null)
            {
                if (Pieces.ContainsKey(_hint))
                {
                    var hintPiece = Pieces[_hint];
                    if (hintPiece != null)
                    {
                        hintPiece.IsHint = false;
                        hintPiece.RaiseMultiplePropertiesChanged();
                    }
                }
                _hint = null;
            }
        }

        #endregion GetHintCommand

        #region PassCommand
        DelegateCommand<object> _passCommand;
        public DelegateCommand<object> PassCommand => _passCommand ?? (_passCommand = new DelegateCommand<object>(ExecutePassCommand, CanPassCommand));
        public bool CanPassCommand(object position)
        {
            // No validation required in version 1.0. Since the human player is always activated.
            return IsBusy == false && Status == GoGameStatus.Active;
        }
        public async void ExecutePassCommand(object position)
        {
            MessageText = "Passing...";
            IsBusy = true;
            var move = new GoMove(MoveType.Pass,
                                  _players[WhoseTurn].Color, null);
            var resp = await GameEngine.PlayAsync(_activeGameId, move);
            if (resp.ResultCode == GoResultCode.Success && 
                resp.MoveResult.Status != GoGameStatus.Active)
            {
                await CalculateArea(true);
            }
            IsBusy = false;
            MessageText = null;

            if (resp.ResultCode == GoResultCode.Success)
            {
                AddMoveToHistory(resp.Move, resp.MoveResult);
                SwapTurns();
                
                AdjustToState(resp.MoveResult.Status);
                var oldStatus = Status;
                if (Status == GoGameStatus.Active)
                    await PlayCurrentUser();
                // PlayCurrentUser() can change the Status, so we may need to recalc.
                if (oldStatus != Status)
                {
                    IsBusy = true;
                    MessageText = "Calculating...";
                    await CalculateArea(Status != GoGameStatus.Active);
                    IsBusy = false;
                    MessageText = null;
                    AdjustToState(Status);
                }
            }
            else if (resp.ResultCode == GoResultCode.CommunicationError)
                await HandleCommunicationError("Passing...");
            else
            {
                await DisplayErrorCode(resp.ResultCode);
                LoadGameFromEngineAsync("Syncronizing...");
            }

            RaiseCommandsChanged();
        }
        #endregion

        #region PressedCommand
        DelegateCommand<string> _pressedCommand;
        public DelegateCommand<string> PressedCommand => _pressedCommand ?? (_pressedCommand = new DelegateCommand<string>(ExecutePressedCommand, CanPressedCommand));

        public bool CanPressedCommand(string position)
        {
            return position != null &&
                   IsBusy == false &&
                   Status == GoGameStatus.Active &&
                   Pieces != null &&
                   Pieces.ContainsKey(position) &&
                   Pieces[position].Color == null &&
                   _players != null &&
                   _players.Length == 2 &&
                   _players[WhoseTurn].PlayerType == PlayerType.Human;
        }
        public async void ExecutePressedCommand(string position)
        {
            // Note: position is the Go position such as "A15" where the user pressed.

            if (!Pieces.ContainsKey(position))
                return;

            _savedColor = Pieces[position].Color;
            Pieces[position].Color = _players[WhoseTurn].Color;
            Pieces[position].RaiseMultiplePropertiesChanged();

            MessageText = "Moving...";
            IsBusy = true;
            var move = new GoMove(MoveType.Normal,
                                  _players[WhoseTurn].Color,
                                  position);
            GoMoveResponse resp = await GameEngine.PlayAsync(_activeGameId, move);
            await CalculateArea();
            IsBusy = false;
            MessageText = null;

            if (resp.ResultCode == GoResultCode.Success)
            {
                AddMoveToHistory(resp.Move, resp.MoveResult);
                RemoveCapturedPieces(resp);
                CurrentPlayer.Captured += resp.MoveResult.CapturedStones.Split(' ').Count(x => x != string.Empty);
                Pieces[position].RaiseMultiplePropertiesChanged();
                SwapTurns();

                AdjustToState(resp.MoveResult.Status);
                var oldStatus = Status;
                if (Status == GoGameStatus.Active)
                    await PlayCurrentUser();
                if (oldStatus != Status)
                {
                    IsBusy = true;
                    MessageText = "Calculating...";
                    await CalculateArea(Status != GoGameStatus.Active);
                    IsBusy = false;
                    MessageText = null;
                    AdjustToState(Status);
                }
            }
            else if (resp.ResultCode == GoResultCode.CommunicationError)
                await HandleCommunicationError("Moving...");
            else
            {
                Pieces[position].Color = _savedColor;
                Pieces[position].RaiseMultiplePropertiesChanged();
                await DisplayErrorCode(resp.ResultCode);
            }

            RaiseCommandsChanged();
        }

        private void SwapTurns()
        {
            ClearHint();
            WhoseTurn = WhoseTurn == 0 ? 1 : 0;
        }

        #endregion PressedCommand

        #region ResignCommand
        DelegateCommand _resignCommand;
        public DelegateCommand ResignCommand => _resignCommand ?? (_resignCommand = new DelegateCommand(ExecuteResign, CanResign));

        public bool CanResign()
        {
            return IsBusy == false &&
                   Status == GoGameStatus.Active &&
                   _players != null &&
                   _players.Length == 2 &&
                   CurrentPlayer.PlayerType == PlayerType.Human;
        }
        public async void ExecuteResign()
        {
            MessageText = "Resigning...";
            IsBusy = true;
            var move = new GoMove(MoveType.Resign,
                                  _players[WhoseTurn].Color,
                                  null);
            var resp = await GameEngine.PlayAsync(_activeGameId, move);
            //await CalculateArea();
            IsBusy = false;
            MessageText = null;

            if (resp.ResultCode == GoResultCode.Success)
            {
                AdjustToState(resp.MoveResult.Status);
                await Task.Delay(2000);
                //AddMoveToHistory(resp.Move, resp.MoveResult);
                SwapTurns();
                IsBusy = true;
                MessageText = "Calculating...";
                await CalculateArea(Status != GoGameStatus.Active);
                IsBusy = false;
                MessageText = null;
                AdjustToState(resp.MoveResult.Status);
            }
            else if (resp.ResultCode == GoResultCode.CommunicationError)
                await HandleCommunicationError("Resigning...");
            else
            {
                await DisplayErrorCode(resp.ResultCode);
                LoadGameFromEngineAsync("Syncronizing...");
            }

            RaiseCommandsChanged();
        }
        #endregion ResignCommand

        #region UndoCommand
        private DelegateCommand _undoCommand;
        public DelegateCommand UndoCommand => _undoCommand ?? (_undoCommand = new DelegateCommand(ExecuteUndo, CanUndo));

        public bool CanUndo()
        {
            if (_player1 == null || _player2 == null)
                return false;
            var humanColor = Player1.PlayerType == PlayerType.Human ? GoColor.Black : GoColor.White;
            if (humanColor == GoColor.Black && Status == GoGameStatus.WhiteWonDueToResignation)
                return true;
            if (humanColor == GoColor.White && Status == GoGameStatus.BlackWonDueToResignation)
                return true;
            var firstHumanMove = History.FirstOrDefault(h => h.Move.Color == humanColor);
            return IsBusy == false &&
                   Pieces != null &&
                   History != null &&
                   firstHumanMove != null &&
                   _players != null &&
                   _players.Length == 2 &&
                   (_players[WhoseTurn].PlayerType == PlayerType.Human || Status != GoGameStatus.Active);
        }
        public async void ExecuteUndo()
        {
            ClearHint();

            MessageText = "Undoing...";
            IsBusy = true;
            var resp = await GameEngine.UndoAsync(_activeGameId);
            IsBusy = false;
            MessageText = null;

            if (resp.ResultCode == GoResultCode.Success)
            {
                _activeGame = resp.GameState;

                ContinueFromActiveGameState();
                await PlayCurrentUser();
                await CalculateArea();

                //// Removes a resign or removes the last two moves, restoring any captured pieces and reducing
                //// prisoner count as necessary.
                //UndoMovesFromHistory();

                //// When we end a game, we set the victor to be the current turn so his/her ball
                //// will bounce.  To undo this, we set the active turn to the human player.
                //if (Status != GoGameStatus.Active)
                //    WhoseTurn = Player1.PlayerType == PlayerType.Human ? 0 : 1;
                //Status = GoGameStatus.Active;
            }
            else if (resp.ResultCode == GoResultCode.CommunicationError)
                await HandleCommunicationError("Undoing...");
            else
            {
                await DisplayErrorCode(resp.ResultCode);
                LoadGameFromEngineAsync("Undoing...");
            }

            RaiseCommandsChanged();
        }
        #endregion UndoCommand

        #region ShowAreaCommand
        private DelegateCommand _showAreaCommand;
        public DelegateCommand ShowAreaCommand => _showAreaCommand ?? (_showAreaCommand = new DelegateCommand(ExecuteShowArea, CanShowArea));
        public bool CanShowArea()
        {
            if (_player1 == null || _player2 == null)
                return false;
            return IsBusy == false && Status == GoGameStatus.Active;
        }
        public async void ExecuteShowArea()
        {
            ShowingArea = !ShowingArea;
            await _repository.UpdateGameAsync(_activeGame);
            RaiseCommandsChanged();

            IsBusy = true;
            MessageText = "Calculating...";
            await CalculateArea();
            IsBusy = false;
            MessageText = null;
        }

        #endregion ShowAreaCommand

        #region EstimateDeadCommand
        private DelegateCommand _estimateDeadCommand;
        public DelegateCommand EstimateDeadCommand => _estimateDeadCommand ?? (_estimateDeadCommand = new DelegateCommand(ExecuteEstimateDeadCommand, CanEstimateDeadCommand));
        public bool CanEstimateDeadCommand()
        {
            return IsBusy == false && Status == GoGameStatus.Active;
        }
        public async void ExecuteEstimateDeadCommand()
        {
            IsBusy = true;
            MessageText = "Calculating...";
            if (_showedDeadStoneMessage || await DisplayQuery("Dead Stone Estimation",
                    "THIS IS A VERY LENGTHY CALCULATION ON LARGER BOARD SIZES.\n\nDead stone estimation will estimate the current dead stones based on 10,000 simulated games.  The result may be completely wrong, or it may fail to find the dead stones altogether, so please use this feature with that in mind.  When completed, the Area will include dead stones, but only until the next move.\n\nA dead stone is one that is suspected of being close to being captured, or will inevitably be captured.\n\nFinal scoring *will* include dead stones, but it may miss some opposing stones you consider to be dead.  Therefore, to ensure you get points for them, you *must* capture all dead stones before you pass!",
                    "Continue", "Cancel") == "Continue")
            {
                await CalculateArea(true);
                _showedDeadStoneMessage = true;
            }

            IsBusy = false;
            MessageText = null;
        }
        #endregion EstimateDeadCommand

        #endregion Commands

        #region Virtuals

        protected override void OnIsBusyChanged()
        {
            RaiseCommandsChanged();
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e,
            Dictionary<string, object> viewModelState)
        {
            try
            {
                AbortOperation = false;

                base.OnNavigatedTo(e, viewModelState);

                if (e.Parameter is Guid)
                {
                    // Resume a single player game using the navigation parameter sent in.
                    _activeGameId = (Guid)e.Parameter;
                }

                if (_activeGameId == Guid.Empty)
                {
                    GoBackDeferred();
                    return;
                }

                LoadGameFromEngineAsync("Syncronizing...");
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override bool CanGoBack()
        {
            return !IsBusy;
        }

        #endregion Virtuals

        #region Private

        protected override void RaiseCommandsChanged()
        {
            base.RaiseCommandsChanged();

            GetHintCommand.RaiseCanExecuteChanged();
            PassCommand.RaiseCanExecuteChanged();
            PressedCommand.RaiseCanExecuteChanged();
            ResignCommand.RaiseCanExecuteChanged();
            UndoCommand.RaiseCanExecuteChanged();
            ShowAreaCommand.RaiseCanExecuteChanged();
            EstimateDeadCommand.RaiseCanExecuteChanged();
        }

        private async Task PlayCurrentUser()
        {
            switch (CurrentPlayer.PlayerType)
            {
                case PlayerType.Ai:
                    await PlayOpponent();
                    break;
                case PlayerType.Human:
                    // Nothing to do, just wait for user to make his move.
                    break;
                case PlayerType.Remote:
                    // Other player's turn.
                    // TODO: Implement this.
                    throw new NotImplementedException();
            }
        }

        private async Task PlayOpponent()
        {
            MessageText = _players[WhoseTurn].Name + " is thinking...";
            IsBusy = true;
            var resp = await GameEngine.GenMoveAsync(_activeGameId, _players[WhoseTurn].Color);
            IsBusy = false;
            MessageText = null;

            if (AbortOperation)
                return;

            if (resp.ResultCode == GoResultCode.Success)
            {
                ClearHint();

                switch (resp.Move.MoveType)
                {
                    case MoveType.Normal:
                        CurrentPlayer.Captured += resp.MoveResult.CapturedStones.Split(' ').Count(x => x != String.Empty);
                        if (Pieces.ContainsKey(resp.Move.Position))
                        {
                            var p = Pieces[resp.Move.Position];
                            if (p != null)
                            {
                                p.Color = CurrentPlayer.Color;
                                p.IsNewPiece = true;
                                AddMoveToHistory(resp.Move, resp.MoveResult);
                                RemoveCapturedPieces(resp);
                                SwapTurns();
                                await PlayCurrentUser();
                                await CalculateArea();
                            }
                        }
                        break;
                    case MoveType.Pass:

                        AddMoveToHistory(resp.Move, resp.MoveResult);
                        await InvokeFleetingMessage(CurrentPlayer.Name + " passes.", 2000);
                        if (Status != GoGameStatus.Active)
                        {
                            IsBusy = true;
                            MessageText = "Calculating...";
                            await CalculateArea(true);
                            IsBusy = false;
                            MessageText = null;
                        }
                        AdjustToState(resp.MoveResult.Status);
                        SwapTurns();
                        if (Status == GoGameStatus.Active)
                        {
                            await CalculateArea();
                            await PlayCurrentUser();
                        }
                        break;
                    case MoveType.Resign:
                        AdjustToState(resp.MoveResult.Status);
                        await Task.Delay(2000);
                        IsBusy = true;
                        MessageText = "Calculating...";
                        await CalculateArea(true);
                        IsBusy = false;
                        AdjustToState(Status);
                        SwapTurns();
                        break;
                }
            }
            else if (resp.ResultCode == GoResultCode.CommunicationError)
                await HandleCommunicationError(_players[WhoseTurn].Name + " is thinking...");
            else
                await DisplayErrorCode(resp.ResultCode);

            RaiseCommandsChanged();
        }

        private async Task HandleCommunicationError(string msg)
        {
            await DisplayErrorCode(GoResultCode.CommunicationError);
            LoadGameFromEngineAsync(msg);
        }

        private void RaiseAllPiecesChanged()
        {
            foreach (var p in Pieces.Values)
                p.RaiseMultiplePropertiesChanged();
        }

        private void AddMoveToHistory(GoMove move, GoMoveResult result)
        {
            try
            {
                if (move.MoveType == MoveType.Normal)
                {
                    var latestPiece = GetLatestNormalMovePieceFromHistory();
                    if (latestPiece != null)
                    {
                        latestPiece.IsNewPiece = false;
                        latestPiece.RaiseMultiplePropertiesChanged();
                    }
                }

                History.Insert(0, new GoMoveHistoryItem
                {
                    Move = move,
                    Result = result,
                });

                if (move.MoveType == MoveType.Normal)
                {
                    var latestPiece = GetLatestNormalMovePieceFromHistory();
                    if (latestPiece != null)
                    {
                        latestPiece.IsNewPiece = true;
                        latestPiece.RaiseMultiplePropertiesChanged();
                    }
                }
            }
            catch (Exception)
            {
                // Eat a winrt bug where the UI sees the inserted item and throws an exception if the user
                // already navigated away from the game page.
                throw;
            }
        }

        //private void UndoMovesFromHistory()
        //{
        //    if (Status == GoGameStatus.BlackWonDueToResignation || Status == GoGameStatus.WhiteWonDueToResignation)
        //        History.RemoveAt(0);
        //    else
        //    {
        //        Debug.Assert(History.Count < 2, "Can't undo more moves than have been made.");

        //        int count = 2;
        //        if (History.Count % 2 == 1)
        //            count = 1;

        //        // Pop off the top move.
        //        for (int i = 0; i < count; i++)
        //        {
        //            var his = History[0];
        //            var pos = his.Move.Position;
        //            if (his.Move.MoveType == MoveType.Normal)
        //            {
        //                if (Pieces.ContainsKey(pos))
        //                {
        //                    var piece = Pieces[pos];
        //                    piece.IsNewPiece = false;
        //                    piece.Color = null;
        //                    piece.RaiseMultiplePropertiesChanged();
        //                }

        //                var player = _players.First(p => p.Color == his.Move.Color);

        //                // Restore any captured pieces.
        //                foreach (var cap in his.Result.CapturedStones.Split(' '))
        //                {
        //                    if (cap != String.Empty && Pieces.ContainsKey(cap))
        //                    {
        //                        player.Prisoners--;

        //                        var piece = Pieces[cap];
        //                        piece.IsNewPiece = false;
        //                        piece.Color = his.Move.Color == GoColor.Black ? GoColor.White : GoColor.Black;
        //                        piece.RaiseMultiplePropertiesChanged();
        //                    }
        //                }
        //            }

        //            History.RemoveAt(0);
        //        }

        //        var latestPiece = GetLatestNormalMovePieceFromHistory();
        //        if (latestPiece != null)
        //        {
        //            latestPiece.IsNewPiece = true;
        //            latestPiece.RaiseMultiplePropertiesChanged();
        //        }
        //    }
        //}

        private PieceStateViewModel GetLatestNormalMovePieceFromHistory()
        {
            var latestMove = History.FirstOrDefault(hi => hi.Move.MoveType == MoveType.Normal);
            if (latestMove != null)
            {
                if (Pieces.ContainsKey(latestMove.Move.Position))
                {
                    var piece = Pieces[latestMove.Move.Position];
                    return piece;
                }
            }

            return null;
        }

        private void RemoveCapturedPieces(GoMoveResponse resp)
        {
            foreach (var piece in Pieces.Values)
                if (piece.IsNewCapture)
                {
                    piece.IsNewCapture = false;
                    piece.RaiseMultiplePropertiesChanged();
                }

            foreach (var capturedPosition in resp.MoveResult.CapturedStones.Split(' '))
                if (!string.IsNullOrWhiteSpace(capturedPosition))
                {
                    if (!Pieces.ContainsKey(capturedPosition))
                        continue;

                    var piece = Pieces[capturedPosition];
                    piece.Color = null;
                    piece.IsNewCapture = true;
                    piece.RaiseMultiplePropertiesChanged();
                }
        }

        // Tries to get state from engine.  Then, calls ContinueGameFromState()
        // to sync our state with it.  Displays appropriate messages and retries 
        // as necessary.
        private async void LoadGameFromEngineAsync(string msg)
        {
            MessageText = msg;
            IsBusy = true;

            if (AbortOperation)
                return;

            var resp = await GameEngine.GetGameStateAsync(_activeGameId);
            IsBusy = false;
            MessageText = null;

            Debug.Assert(resp != null, "resp != null");

            if (AbortOperation)
                return;

            if (resp.ResultCode == GoResultCode.Success)
            {
                _activeGame = resp.GameState;
                ShowingArea = _activeGame.ShowingArea;

                ContinueFromActiveGameState();

                // Reflect actual operation happening in engine and retry sync.
                switch (resp.GameState.Operation)
                {
                    case GoOperation.GenMove:
                        await WaitAndRetryLoadGameFromEngineAsync(5000, "Fuego is thinking...");
                        break;
                    case GoOperation.Starting:
                        await WaitAndRetryLoadGameFromEngineAsync(5000, "Starting game...");
                        break;
                    case GoOperation.Hint:
                    case GoOperation.NormalMove:
                    case GoOperation.Pass:
                    case GoOperation.Resign:
                    case GoOperation.Idle:
                        if (AbortOperation)
                            break;
                        RunOnUIThread(async () =>
                        {
                            AdjustToState(resp.GameState.Status);
                            if (Status == GoGameStatus.Active)
                                await PlayCurrentUser();
                            IsBusy = true;
                            MessageText = "Calculating...";
                            await CalculateArea(Status != GoGameStatus.Active);
                            IsBusy = false;
                            MessageText = null;
                            AdjustToState(Status);
                        });
                        break;
                }
            }
            else
            {
                await DisplayErrorCode(resp.ResultCode);
                GoBackDeferred();
            }
        }

        // This method is used by LoadGameFromServerAsync() to call itself recursively
        // (on another thread) after a given delay.
        private async Task WaitAndRetryLoadGameFromEngineAsync(int delay, string msg)
        {
            IsBusy = true;
            MessageText = msg;

            // Wait a few seconds, then recursively check state again.
            await Task.Run(() =>
                     {
                         if (!AbortOperation)
                             Task.Delay(delay).Wait();
                         if (!AbortOperation)
                             RunOnUIThread(() => LoadGameFromEngineAsync(msg));
                     });
        }

        private void ContinueFromActiveGameState()
        {
            // Player1 is always black, Player2 is always white.
            Player1 = new PlayerViewModel(_activeGame.Player1, GoColor.Black);
            Player2 = new PlayerViewModel(_activeGame.Player2, GoColor.White);

            _players = new[] { Player1, Player2 };

            WhoseTurn = _activeGame.WhoseTurn == GoColor.Black ? 0 : 1;
            RaisePropertyChanged(nameof(WhoseTurn));
            CurrentTurnColor = _players[_whoseTurn].Color;

            AdjustToState(_activeGame.Status);

            // Note that setting BoardEdgeSize triggers the board control to generate.
            BoardEdgeSize = _activeGame.Size;

            // Build a temporary dictionary with ALL the piece states in it, all set to
            // contain an empty PieceState initially.
            var tmpPieces = new Dictionary<string, PieceStateViewModel>();
            for (int x = 0; x < _activeGame.Size; x++)
                for (int y = 0; y < _activeGame.Size; y++)
                {
                    var position = EngineHelpers.DecodePosition(x, y, _activeGame.Size);
                    tmpPieces.Add(position, new PieceStateViewModel(position, null, null, false, false, false));
                }

            // This actually updates the UI.  Note that we can't add anything to Pieces after
            // this point because Pieces is a Dictionary, which can't be observed by the GameBoard
            // control.  From here forward, we simply update individual pieces.
            Pieces = tmpPieces;

            int blackPrisoners = 0;
            int whitePrisoners = 0;

            // Save history.
            History = new ObservableCollection<GoMoveHistoryItem>();
            foreach (var h in _activeGame.GoMoveHistory)
            {
                History.Insert(0, h);
                if (h.Move.Color == GoColor.Black)
                    blackPrisoners += h.Result.CapturedStones.Split(' ').Count(x => x != string.Empty);
                else
                    whitePrisoners += h.Result.CapturedStones.Split(' ').Count(x => x != string.Empty);
            }

            Player1.Captured = Player1.Color == GoColor.Black ? blackPrisoners : whitePrisoners;
            Player2.Captured = Player2.Color == GoColor.Black ? blackPrisoners : whitePrisoners;

            // Set piece states for all the existing white and black pieces.
            SetPieces(_activeGame.BlackPositions, GoColor.Black);
            SetPieces(_activeGame.WhitePositions, GoColor.White);

            var latestNormalPiece = GetLatestNormalMovePieceFromHistory();
            if (latestNormalPiece != null)
                latestNormalPiece.IsNewPiece = true;

            // Correct Sequence value.
            //FixSequenceValuesForColor(state, GoColor.Black);
            //FixSequenceValuesForColor(state, GoColor.White);

            RaiseAllPiecesChanged();
            RaiseCommandsChanged();
        }

        private void AdjustToState(GoGameStatus status)
        {
            Status = status;

            var humanPlayer = Player1.PlayerType == PlayerType.Human ? Player1 : Player2;
            var aiPlayer = Player1.PlayerType == PlayerType.Human ? Player2 : Player1;

            var margin = humanPlayer.Score - aiPlayer.Score;

            switch (Status)
            {
                case GoGameStatus.Ended:
                    if (margin > 0)
                        MessageText = "You win by " + margin + "!";
                    else
                        MessageText = aiPlayer.Name + " wins by " + -margin + ".";
                    break;
                case GoGameStatus.BlackWonDueToResignation:
                    if (humanPlayer.Color == GoColor.Black)
                        MessageText = aiPlayer.Name + " resigned.  You win!";
                    else
                        MessageText = "You resigned.  " + aiPlayer.Name + " wins.";
                    break;
                case GoGameStatus.WhiteWonDueToResignation:
                    if (humanPlayer.Color == GoColor.White)
                        MessageText = aiPlayer.Name + " resigned.  You win!";
                    else
                        MessageText = "You resigned.  " + aiPlayer.Name + " wins.";
                    break;
            }
        }

        private void SetPieces(string p, GoColor goColor)
        {
            var split = p.Split(' ');
            foreach (var pos in split)
            {
                if (!string.IsNullOrWhiteSpace(pos) && Pieces.ContainsKey(pos))
                {
                    Pieces[pos].Color = goColor;
                }
                else if (!string.IsNullOrWhiteSpace(pos))
                {
                    // Can't set the sequence yet because index is not the correct value.
                    Pieces.Add(pos, new PieceStateViewModel(pos, null, goColor, false, false, false));
                }
            }
        }

        private async Task CalculateArea(bool estimateDead = false)
        {
            var areaResponse = await GameEngine.GetArea(_activeGameId, estimateDead);
            if (areaResponse.ResultCode != GoResultCode.Success)
            {
                foreach (var p in Pieces.Values)
                    p.Territory = null;
            }
            else
            {
                if (ShowingArea || estimateDead)
                {
                    foreach (var p in Pieces.Keys)
                    {
                        if (areaResponse.BlackArea.Contains(p) ||
                            areaResponse.WhiteDead.Contains(p))
                            Pieces[p].Territory = GoColor.Black;
                        else if (areaResponse.WhiteArea.Contains(p) ||
                                 areaResponse.BlackDead.Contains(p) ||
                                 Pieces[p].Color == GoColor.White)
                            Pieces[p].Territory = GoColor.White;
                        else
                            Pieces[p].Territory = null;
                    }
                }
                else
                    foreach (var p in Pieces.Values)
                        p.Territory = null;
            }

            Player1.Area = areaResponse.BlackArea?.Count ?? 0;
            Player1.Dead = areaResponse.WhiteDead?.Count ?? 0;
            Player2.Area = areaResponse.WhiteArea?.Count ?? 0;
            Player2.Dead = areaResponse.BlackDead?.Count ?? 0;
        }

        #endregion Private
    }
}