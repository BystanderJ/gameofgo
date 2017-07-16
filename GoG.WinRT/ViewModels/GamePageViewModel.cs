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
        #region Ctor
        public GamePageViewModel(INavigationService navigationService,
            ISessionStateService sessionStateService,
            IGameEngine engine) : base(navigationService, sessionStateService, engine)
        {
        }
        #endregion Ctor

        #region Data

        private GoColor? _savedColor;
        private string _hint;
        private PlayerViewModel[] _players;
        private GoGame _activeGame;
        private Guid _activeGameId;

        #endregion Data

        #region Properties

        #region ShowingArea
        private bool _showingArea;
        [RestorableState]
        public bool ShowingArea
        {
            get => _showingArea; set => SetProperty(ref _showingArea, value);
        }
        #endregion ShowingArea

        #region MessageText
        private string _messageText;
        public string MessageText
        {
            get => _messageText; set
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
        public DelegateCommand GetHintCommand
        {
            get { if (_getHintCommand == null) _getHintCommand = new DelegateCommand(ExecuteGetHint, CanGetHint); return _getHintCommand; }
        }
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
            //Novalidation required at in version 1.0. Since the human player si always activated
            return IsBusy == false && Status == GoGameStatus.Active;
        }
        public async void ExecutePassCommand(object position)
        {
            MessageText = "Passing...";
            IsBusy = true;
            var move = new GoMove(MoveType.Pass,
                                  _players[WhoseTurn].Color, null);
            var resp = await GameEngine.PlayAsync(_activeGameId, move);
            IsBusy = false;
            MessageText = null;

            if (resp.ResultCode == GoResultCode.Success)
            {
                AddMoveToHistory(resp.Move, resp.MoveResult);
                SwapTurns();
                AdjustToState(resp.MoveResult.Status, resp.MoveResult.WinMargin);
                if (Status == GoGameStatus.Active)
                    PlayCurrentUser();
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
            IsBusy = false;
            MessageText = null;

            if (resp.ResultCode == GoResultCode.Success)
            {
                AddMoveToHistory(resp.Move, resp.MoveResult);
                //Pieces[resp.Move.Position].IsNewPiece = true;
                RemoveCapturedPieces(resp);
                CurrentPlayer.Prisoners += resp.MoveResult.CapturedStones.Split(' ').Count(x => x != String.Empty);
                Pieces[position].RaiseMultiplePropertiesChanged();
                SwapTurns();

                AdjustToState(resp.MoveResult.Status, resp.MoveResult.WinMargin);
                if (Status == GoGameStatus.Active)
                    PlayCurrentUser();
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
        public DelegateCommand ResignCommand
        {
            get { if (_resignCommand == null) _resignCommand = new DelegateCommand(ExecuteResign, CanResign); return _resignCommand; }
        }
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
            IsBusy = false;
            MessageText = null;

            if (resp.ResultCode == GoResultCode.Success)
            {
                //AddMoveToHistory(resp.Move, resp.MoveResult);
                SwapTurns();
                AdjustToState(resp.MoveResult.Status, resp.MoveResult.WinMargin);
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
        DelegateCommand _undoCommand;
        public DelegateCommand UndoCommand
        {
            get { if (_undoCommand == null) _undoCommand = new DelegateCommand(ExecuteUndo, CanUndo); return _undoCommand; }
        }
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
                ContinueGameFromState(resp.GameState);
                PlayCurrentUser();

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
        DelegateCommand _showAreaCommand;
        public DelegateCommand ShowAreaCommand
        {
            get { if (_showAreaCommand == null) _showAreaCommand = new DelegateCommand(ExecuteShowArea, CanShowArea); return _showAreaCommand; }
        }
        public bool CanShowArea()
        {
            if (_player1 == null || _player2 == null)
                return false;
            return true;
        }
        public void ExecuteShowArea()
        {
            ShowingArea = !ShowingArea;
            
            RaiseCommandsChanged();
        }
        #endregion ShowAreaCommand
        
        #endregion Commands

        #region Virtuals

        protected override void OnIsBusyChanged()
        {
            RaiseCommandsChanged();
        }

        public override async void OnNavigatedTo(NavigatedToEventArgs e, 
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

                var resp = await GameEngine.GetGameStateAsync(_activeGameId);
                if (resp.ResultCode != GoResultCode.Success)
                {
                    GoBackDeferred();
                    return;
                }
                _activeGame = resp.GameState;
                
                LoadGameFromEngineAsync("Syncronizing...");
            }
            catch (Exception)
            {
                throw;
            }
        }
        
        #endregion Virtuals

        #region Private

        private void RaiseCommandsChanged()
        {
            GetHintCommand.RaiseCanExecuteChanged();
            PassCommand.RaiseCanExecuteChanged();
            PressedCommand.RaiseCanExecuteChanged();
            ResignCommand.RaiseCanExecuteChanged();
            UndoCommand.RaiseCanExecuteChanged();
            ShowAreaCommand.RaiseCanExecuteChanged();
        }

        private void PlayCurrentUser()
        {
            switch (CurrentPlayer.PlayerType)
            {
                case PlayerType.AI:
                    PlayAi();
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

        private async void PlayAi()
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
                        CurrentPlayer.Prisoners += resp.MoveResult.CapturedStones.Split(' ').Count(x => x != String.Empty);
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
                                PlayCurrentUser();
                            }
                        }
                        break;
                    case MoveType.Pass:
                        AddMoveToHistory(resp.Move, resp.MoveResult);
                        await InvokeFleetingMessage(CurrentPlayer.Name + " passes.", 1500);
                        SwapTurns();
                        AdjustToState(resp.MoveResult.Status, resp.MoveResult.WinMargin);
                        if (Status == GoGameStatus.Active)
                            PlayCurrentUser();
                        break;
                    case MoveType.Resign:
                        Status = CurrentPlayer.Color == GoColor.Black ? GoGameStatus.WhiteWon : GoGameStatus.BlackWon;
                        MessageText = CurrentPlayer.Name + " resigns.  You win!";
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
                // eat a winrt bug where the UI sees the inserted item and throws an exception if the user
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
                if (!String.IsNullOrWhiteSpace(capturedPosition))
                {
                    if (Pieces.ContainsKey(capturedPosition))
                    {
                        var piece = Pieces[capturedPosition];
                        piece.Color = null;
                        piece.IsNewCapture = true;
                        piece.RaiseMultiplePropertiesChanged();
                    }
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

            await GameEngine.CreateOrSyncToGameAsync(_activeGameId, _activeGame);

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
                ContinueGameFromState(resp.GameState);

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
                        RunOnUIThread(() =>
                        {
                            AdjustToState(resp.GameState.Status, resp.GameState.WinMargin);
                            if (Status == GoGameStatus.Active)
                                    PlayCurrentUser();
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

        private void ContinueGameFromState(GoGame state)
        {
            _activeGame = state;

            // Player1 is always black, Player2 is always white.
            Player1 = new PlayerViewModel(state.Player1, GoColor.Black);
            Player2 = new PlayerViewModel(state.Player2, GoColor.White);

            _players = new[] { Player1, Player2 };

            WhoseTurn = state.WhoseTurn == GoColor.Black ? 0 : 1;
            RaisePropertyChanged(nameof(WhoseTurn));
            CurrentTurnColor = _players[_whoseTurn].Color;

            AdjustToState(state.Status, state.WinMargin);

            // Note that setting BoardEdgeSize triggers the board control to generate.
            BoardEdgeSize = state.Size;

            // Build a temporary dictionary with ALL the piece states in it, all set to
            // contain an empty PieceState initially.
            var tmpPieces = new Dictionary<string, PieceStateViewModel>();
            for (int x = 0; x < state.Size; x++)
                for (int y = 0; y < state.Size; y++)
                {
                    var position = EngineHelpers.DecodePosition(x, y, state.Size);
                    tmpPieces.Add(position, new PieceStateViewModel(position, null, null, false, false, false));
                }

            // This actually updates the UI.  Note that we can't add anything to Pieces after
            // this point because Pieces is a Dictionary, which can't be observed by the GameBoard
            // control.  From here forward, we simply update individual pieces inside Pieces; we 
            // don't add or remove from it.
            Pieces = tmpPieces;

            int blackPrisoners = 0;
            int whitePrisoners = 0;

            // Save history.
            History = new ObservableCollection<GoMoveHistoryItem>();
            foreach (var h in state.GoMoveHistory)
            {
                History.Insert(0, h);
                if (h.Move.Color == GoColor.Black)
                    blackPrisoners += h.Result.CapturedStones.Split(' ').Count(x => x != String.Empty);
                else
                    whitePrisoners += h.Result.CapturedStones.Split(' ').Count(x => x != String.Empty);
            }

            Player1.Prisoners = Player1.Color == GoColor.Black ? blackPrisoners : whitePrisoners;
            Player2.Prisoners = Player2.Color == GoColor.Black ? blackPrisoners : whitePrisoners;

            // Set piece states for all the existing white and black pieces.
            SetPieces(state.BlackPositions, GoColor.Black);
            SetPieces(state.WhitePositions, GoColor.White);

            var latestNormalPiece = GetLatestNormalMovePieceFromHistory();
            if (latestNormalPiece != null)
                latestNormalPiece.IsNewPiece = true;

            // Correct Sequence value.
            //FixSequenceValuesForColor(state, GoColor.Black);
            //FixSequenceValuesForColor(state, GoColor.White);

            RaiseAllPiecesChanged();
            RaiseCommandsChanged();
        }

        private void AdjustToState(GoGameStatus status, decimal margin)
        {
            Status = status;
            
            var humanPlayer = Player1.PlayerType == PlayerType.Human ? Player1 : Player2;
            var aiPlayer = Player1.PlayerType == PlayerType.Human ? Player2 : Player1;

            switch (Status)
            {
                case GoGameStatus.BlackWon:
                    if (humanPlayer.Color == GoColor.Black)
                        MessageText = "You win by " + margin + "!";
                    else
                        MessageText = aiPlayer.Name + " wins by " + margin + ".";
                    break;
                case GoGameStatus.WhiteWon:
                    if (humanPlayer.Color == GoColor.White)
                        MessageText = "You win by " + margin + "!";
                    else
                        MessageText = aiPlayer.Name + " wins by " + margin + ".";
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
            for (int index = 0; index < split.Length; index++)
            {
                var pos = split[index];
                if (!String.IsNullOrWhiteSpace(pos) && Pieces.ContainsKey(pos))
                {
                    Pieces[pos].Color = goColor;
                }
                else if (!String.IsNullOrWhiteSpace(pos))
                {
                    // Can't set the sequence yet because index is not the correct value.
                    Pieces.Add(pos, new PieceStateViewModel(pos, null, goColor, false, false, false));
                }
            }
        }



        #endregion Private
    }
}