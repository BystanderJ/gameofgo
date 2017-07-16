using FuegoLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using GoG.Infrastructure;
using GoG.Infrastructure.Engine;
using GoG.Infrastructure.Services.Engine;
using Prism.Windows.AppModel;

// ReSharper disable RedundantCatchClause

namespace GoG.WinRT.Services
{
    /// <summary>
    /// A single player game engine.
    /// </summary>
    public class FuegoGameEngine : IGameEngine
    {
        #region Data

        const string GamesStateKey = "GameEngineState";

        readonly ISessionStateService _sessionStateService;

        // _fuego is the AI component, written in C++.  It uses threads, but it also
        // hangs, so every time we call methods on it we must create a thread.
        private FuegoInstance _fuego;

        // The current game.  If its Id does not match the desired state, must coerse the
        // _fuego instance to the desired game then set _state to the new game.
        private GoGame _state;

        // The other games we might need to switch to.  The key is the GoGame.Id.
        private readonly Dictionary<Guid, GoGame> _states = new Dictionary<Guid, GoGame>();

        #endregion Data

        #region Ctor
        public FuegoGameEngine(ISessionStateService sessionStateService)
        {
            _sessionStateService = sessionStateService;

            if (sessionStateService.SessionState.ContainsKey(GamesStateKey))
                LoadState();
        }
        #endregion Ctor

        #region Fuego Implementation

        public async Task<GoResponse> CreateOrSyncToGameAsync(Guid id, GoGame state)
        {
            try
            {
                Debug.Assert(state != null, "state != null");

                if (!_states.ContainsKey(id))
                    _states.Add(id, state);
                
                // The following method sets _state and coerses the FuegoInstance
                // to match that state through a series of GTP commands.
                await EnsureFuegoStartedAndMatchingGame(id);

                return new GoResponse(GoResultCode.Success);
            }
            catch (GoEngineException gex)
            {
                return new GoGameStateResponse(gex.Code, null);
            }
            catch (Exception)
            {
                return new GoGameStateResponse(GoResultCode.InternalError, null);
            }
        }

        public Task<GoResponse> DeleteGameAsync(Guid id)
        {
            if (_states.ContainsKey(id))
                _states.Remove(id);
            return Task.FromResult(new GoResponse(GoResultCode.Success));
        }

        public Task<GoGameStateResponse> GetGameStateAsync(Guid id)
        {
            if (!_states.TryGetValue(id, out var state))
                return Task.FromResult(new GoGameStateResponse(GoResultCode.GameDoesNotExist, null));
            return Task.FromResult(new GoGameStateResponse(GoResultCode.Success, state));
        }

        public async Task<GoMoveResponse> GenMoveAsync(Guid id, GoColor color)
        {
            GoMoveResponse rval = null;

            try
            {
                await Task.Run(
                        async () =>
                        {
                            await EnsureFuegoStartedAndMatchingGame(id);

                            _state.Operation = GoOperation.GenMove;
                            SaveState();

                            // This debug code generates a resign from the AI randomly.
                            //int x = r.Next(5);
                            //if (x == 0)
                            //{
                            //    newMove = new GoMove(MoveType.Resign, color, null);
                            //    //WriteCommand("play", color == GoColor.Black ? "black resign" : "white resign");
                            //    //ReadResponse();
                            //    result = AddMoveAndUpdateStateAndSaveToDatabase(newMove);
                            //    return;
                            //}

                            var result = ParseResponse(WriteCommand("genmove", color == GoColor.Black ? "black" : "white"));

                            GoMove newMove;
                            switch (result.Msg)
                            {
                                case "PASS":
                                    newMove = new GoMove(MoveType.Pass, color, null);
                                    break;
                                case "resign":
                                    newMove = new GoMove(MoveType.Resign, color, null);
                                    break;
                                default:
                                    newMove = new GoMove(MoveType.Normal, color, result.Msg);
                                    break;
                            }

                            // Add to move history and record new game state in database so user can 
                            // see what happened.
                            var moveResult = AddMoveAndUpdateState(newMove);

                            _state.Operation = GoOperation.Idle;
                            SaveState();

                            rval = new GoMoveResponse(GoResultCode.Success, newMove, moveResult);
                        });
            }
            catch (GoEngineException gex)
            {
                rval = new GoMoveResponse(gex.Code, null, null);
            }
            catch (Exception)
            {
                rval = new GoMoveResponse(GoResultCode.InternalError, null, null);
            }

            Debug.Assert(rval != null, "rval != null");

            return rval;
        }

        public async Task<GoMoveResponse> PlayAsync(Guid id, GoMove move)
        {
            GoMoveResponse rval = null;

            GoMoveResult moveResult;
            try
            {
                await Task.Run(
                    async () =>
                    {
                        await EnsureFuegoStartedAndMatchingGame(id);

                        if (move.MoveType == MoveType.Resign)
                        {
                            // Fuego doesn't support the command to resign.
                            _state.Operation = GoOperation.Resign;
                            SaveState();

                            moveResult = AddMoveAndUpdateState(move);
                        }
                        else
                        {
                            string position;
                            switch (move.MoveType)
                            {
                                case MoveType.Normal:
                                    position = move.Position;
                                    _state.Operation = GoOperation.NormalMove;
                                    break;
                                case MoveType.Pass:
                                    position = "PASS";
                                    _state.Operation = GoOperation.Pass;
                                    break;
                                default:
                                    throw new ArgumentException("Unrecognized move type: " + move.MoveType);
                            }

                            SaveState();

                            // This throws a GoEngineException on any failure.
                            ParseResponse(WriteCommand("play",
                                    (move.Color == GoColor.Black ? "black" : "white") + ' ' + position));

                            // Add to move history and persist new game state so user can 
                            // see what happened.
                            moveResult = AddMoveAndUpdateState(move);
                            _state.Operation = GoOperation.Idle;
                            SaveState();

                        }

                        Debug.Assert(moveResult != null, "moveResult != null");
                        rval = new GoMoveResponse(GoResultCode.Success, move, moveResult);
                    });
            }
            catch (GoEngineException gex)
            {
                rval = new GoMoveResponse(gex.Code, null, null);
            }
            catch (Exception)
            {
                rval = new GoMoveResponse(GoResultCode.InternalError, null, null);
            }
            Debug.Assert(rval != null, "rval != null");
            return rval;
        }


        public async Task<GoHintResponse> HintAsync(Guid id, GoColor color)
        {
            GoHintResponse rval = null;
            try
            {
                await Task.Run(
                    async () =>
                    {
                        await EnsureFuegoStartedAndMatchingGame(id);

                        _state.Operation = GoOperation.Hint;
                        SaveState();

                        var result =
                            ParseResponse(WriteCommand("reg_genmove", color == GoColor.Black ? "black" : "white"));

                        GoMove hint;
                        switch (result.Msg)
                        {
                            case "PASS":
                                hint = new GoMove(MoveType.Pass, color, null);
                                break;
                            case "resign":
                                hint = new GoMove(MoveType.Resign, color, null);
                                break;
                            default:
                                hint = new GoMove(MoveType.Normal, color, result.Msg);
                                break;
                        }
                        rval = new GoHintResponse(GoResultCode.Success, hint);

                        _state.Operation = GoOperation.Idle;
                        SaveState();
                    });
            }
            catch (GoEngineException gex)
            {
                rval = new GoHintResponse(gex.Code, null);
            }
            catch (Exception)
            {
                rval = new GoHintResponse(GoResultCode.InternalError, null);
            }

            Debug.Assert(rval != null, "rval != null");
            return rval;
        }

        public async Task<GoGameStateResponse> UndoAsync(Guid id)
        {
            GoGameStateResponse rval = null;
            try
            {
                await Task.Run(
                    async () =>
                    {
                        await EnsureFuegoStartedAndMatchingGame(id);

                        _state.Operation = GoOperation.Undo;
                        SaveState();

                        // Note that resignation is stored as a single move, but fuego.exe doesn't know about resignations so
                        // no need to send an undo command to the engine.

                        int undo = 0;

                        if (_state.Status == GoGameStatus.BlackWonDueToResignation)
                        {
                            var humanColor = _state.Player1.PlayerType == PlayerType.Human ? GoColor.Black : GoColor.White;
                            undo = humanColor == GoColor.Black ? 2 : 1;

                            if (_state.GoMoveHistory.Count > 1 &&
                                _state.GoMoveHistory[_state.GoMoveHistory.Count - 2].Move.Color == humanColor)
                                ParseResponse(WriteCommand("gg-undo", "1"));
                        }
                        else if (_state.Status == GoGameStatus.WhiteWonDueToResignation)
                        {
                            var humanColor = _state.Player1.PlayerType == PlayerType.Human ? GoColor.Black : GoColor.White;
                            undo = humanColor == GoColor.White ? 2 : 1;

                            if (_state.GoMoveHistory.Count > 1 &&
                                _state.GoMoveHistory[_state.GoMoveHistory.Count - 2].Move.Color == humanColor)
                                ParseResponse(WriteCommand("gg-undo", "1"));
                        }
                        else
                        {
                            var his = _state.GoMoveHistory;
                            var count = his.Count;

                            var humanColor = _state.Player1.PlayerType == PlayerType.Human ? GoColor.Black : GoColor.White;

                            // Reverse to before most recent human move.
                            for (int i = count - 1; i >= 0; i--)
                            {
                                if (his[i].Move.Color == humanColor)
                                {
                                    undo = count - i;
                                    break;
                                }
                            }

                            if (undo == 0)
                                throw new Exception("Can't undo because there are no human moves yet.");

                            ParseResponse(WriteCommand("gg-undo", undo.ToString(CultureInfo.InvariantCulture)));
                        }

                        UndoMoves(undo);

                        _state.Operation = GoOperation.Idle;
                        SaveState();

                        rval = new GoGameStateResponse(GoResultCode.Success, _state);
                    });
            }
            catch (GoEngineException gex)
            {
                rval = new GoGameStateResponse(gex.Code, null);
            }
            catch (Exception)
            {
                rval = new GoGameStateResponse(GoResultCode.InternalError, null);
            }

            Debug.Assert(rval != null, "rval != null");
            return rval;
        }

        #endregion Fuego Implementation

        #region Private Helpers

        private void UndoMoves(int moves)
        {
            try
            {
                GetStones(); // Gets the new _state.BlackPositions and _state.WhitePositions.

                if (_state.GoMoveHistory == null)
                    _state.GoMoveHistory = new List<GoMoveHistoryItem>();
                for (int i = 0; i < moves; i++)
                    _state.GoMoveHistory.RemoveAt(_state.GoMoveHistory.Count - 1);

                // Change turn if an odd number of moves were undone.
                if (moves % 2 == 1)
                    _state.WhoseTurn = _state.WhoseTurn == GoColor.Black ? GoColor.White : GoColor.Black;

                _state.WinMargin = 0;
                _state.Status = GoGameStatus.Active;
            }
            catch (Exception)
            {
                throw;
            }
        }


        private string WriteCommand(string cmd, string value = null)
        {
            var s = cmd;
            if (value != null)
                s += ' ' + value + "\n\n";
#if DEBUG
            Debug.Write("WRITING COMMAND: " + s);
#endif
            var result = _fuego.HandleCommand(s);
            return result;
        }

        private class MyResponse
        {
            public MyResponse()
            {

            }

            //public MyResponse(string code, IEnumerable<string> lines)
            //{
            //    Code = code;
            //    Lines = new List<string>(lines);
            //}

            public MyResponse(string code, string msg)
            {
                Code = code;
                Lines = new List<string> { msg };
            }

            public string Code { get; set; }
            public List<string> Lines { get; }
            public string Msg => Lines?[0];
        }

        /// <summary>
        /// Puts the returned id and message into variables _id and _message.  Throws a 
        /// GoEnginException if Fuego complains.
        /// </summary>
        private MyResponse ParseResponse(string str)
        {
            var rval = new MyResponse();

            foreach (var line in str.Split('\n'))
            {
#if DEBUG
                Debug.WriteLine("Read: " + (line ?? "(NULL)"));
#endif

                // If empty line, eats it, otherwise parses the line.
                if (!string.IsNullOrEmpty(line))
                {
                    switch (line[0])
                    {
                        case '?':
                            // If line starts with '?', indicates an error has occurred in Fuego.
                            rval = ParseEngineOutput(line);
                            ParseErrorAndThrowException(rval.Code, rval.Msg);
                            break;
                        case '=':
                            // If line starts with '=', no error.
                            rval = ParseEngineOutput(line);
                            break;
                        default:
                            // If line starts with something else, save it.
                            rval.Lines.Add(line);
                            break;
                    }
                }
            }

            return rval;
        }

        private void ParseErrorAndThrowException(string errorId, string errorMessage)
        {
            GoResultCode? code = null;

            if (errorMessage.StartsWith("illegal move:"))
            {
                if (errorMessage.EndsWith("(occupied)"))
                    code = GoResultCode.IllegalMoveSpaceOccupied;
                else if (errorMessage.EndsWith("(suicide)"))
                    code = GoResultCode.IllegalMoveSuicide;

                else if (errorMessage.EndsWith("(superko)"))
                    code = GoResultCode.IllegalMoveSuperKo;
                else
                    code = GoResultCode.OtherIllegalMove;
            }

            if (errorMessage == "cannot score")
                code = GoResultCode.CannotScore;

            var result = ParseResponse(WriteCommand("showboard"));

            if (code == null)
            {
                result.Lines.Add("Error " + errorId + ": " + errorMessage);
                code = GoResultCode.OtherEngineError;
            }
            else
                result.Lines.Add(errorMessage);

            throw new GoEngineException(code.Value, result.Lines.CombineStrings("\n"));
        }

        // Parses everything after the first character on a response line.
        private MyResponse ParseEngineOutput(string it)
        {
            if (it[1] == ' ')
            {
                // code is not present
                return new MyResponse(null, it.Substring(2));
            }
            else
            {
                // code is present
                var strpos = it.IndexOf(' ', 2);
                return new MyResponse(it.Substring(1, strpos - 1), it.Substring(strpos + 1));
            }
        }

        private void SaveState()
        {
            try
            {
                _sessionStateService.SessionState[GamesStateKey] = _state;

                //var str = JsonConvert.SerializeObject(_state);

                //var storageFolder = ApplicationData.Current.LocalFolder;
                //var file = await storageFolder.CreateFileAsync(StateFilename, CreationCollisionOption.ReplaceExisting);
                //await FileIO.WriteTextAsync(file, str);
            }
            catch (Exception)
            {
                throw;
            }

        }

        private void LoadState()
        {
            try
            {
                if (_sessionStateService.SessionState.ContainsKey(GamesStateKey))
                {
                    _state = (GoGame)_sessionStateService.SessionState[GamesStateKey];
                }

                //var storageFolder = ApplicationData.Current.LocalFolder;
                //var file = await storageFolder.GetFileAsync(StateFilename);
                //var str = await FileIO.ReadTextAsync(file);

                //_state = JsonConvert.DeserializeObject<GoGameState>(str);
            }
            catch (Exception)
            {
                throw;
            }

        }

        private GoMoveResult AddMoveAndUpdateState(GoMove move)
        {
            GoMoveResult rval;

            try
            {
                var beforeBlack = _state.BlackPositions;
                var beforeWhite = _state.WhitePositions;

                GetStones(); // Gets the new _state.BlackPositions and _state.WhitePositions.

                if (move.Color == GoColor.Black)
                    rval = new GoMoveResult(beforeWhite, _state.WhitePositions);
                else
                    rval = new GoMoveResult(beforeBlack, _state.BlackPositions);

                _state.GoMoveHistory.Add(new GoMoveHistoryItem { Move = move, Result = rval });

                // Change turn.
                _state.WhoseTurn = _state.WhoseTurn == GoColor.Black ? GoColor.White : GoColor.Black;

                switch (move.MoveType)
                {
                    case MoveType.Resign:
                        _state.Status = move.Color == GoColor.Black
                            ? GoGameStatus.WhiteWonDueToResignation
                            : GoGameStatus.BlackWonDueToResignation;
                        break;
                    case MoveType.Pass:
                        // If previous move was a pass also, calculate winner.
                        var moveCount = _state.GoMoveHistory.Count;
                        bool previousMoveWasPass = moveCount >= 2 &&
                                                    _state.GoMoveHistory[moveCount - 2].Move.MoveType == MoveType.Pass;
                        if (previousMoveWasPass)
                        {
                            var gameResult2 = CalculateGameResult();
                            _state.WinMargin = decimal.Parse(gameResult2.Substring(1));
                            _state.Status = gameResult2.StartsWith("B") ? GoGameStatus.BlackWon : GoGameStatus.WhiteWon;
                        }
                        break;
                }

                rval.Status = _state.Status;
                rval.WinMargin = _state.WinMargin;
            }
            catch (Exception)
            {
                throw;
            }

            return rval;
        }

//        private async Task UpdateStateFromExeAndSaveState()
//        {
//#if DEBUG
//            var result = ParseResponse(WriteCommand("showboard"));
//#endif

//            GetStones(); // Gets the new _state.BlackPositions and _state.WhitePositions.

//            // Determine whose turn.
//            _state.WhoseTurn = _state.WhoseTurn == GoColor.Black ? GoColor.White : GoColor.Black;

//            // Save to database.
//            SaveState();
//        }

        /// <summary>
        /// Call after each move to save state.
        /// </summary>
        private void GetStones()
        {
            var result = ParseResponse(WriteCommand("list_stones", "black"));
            _state.BlackPositions = result.Msg;

            result = ParseResponse(WriteCommand("list_stones", "white  "));
            _state.WhitePositions = result.Msg;
        }

        private string CalculateGameResult()
        {
            var result = ParseResponse(WriteCommand("final_score"));

            return result.Msg;
        }

        private async Task EnsureFuegoStartedAndMatchingGame(Guid id)
        {
            // We don't want callers to handle GameDoesNotExist errors.
            _states.TryGetValue(id, out var state);
            Debug.Assert(state != null, $"Don't call {nameof(EnsureFuegoStartedAndMatchingGame)} with the ID of a game that does not exist!");

            if (_fuego == null)
            {
                _state = state;
                _fuego = new FuegoInstance();
                await Task.Run(() => RestartEngine(state));
            }
            else
            {
                if (_state != state)
                {
                    _state = state;
                    await Task.Run(() => RestartEngine(state));
                }
            }
        }

        // Call only from EnsureFuegoStartedAndMatchingGame() because that method
        // ensures the _states is updated.
        private void RestartEngine(GoGame state)
        {
            _fuego.StartGame(_state.Size);

            var level = _state.Player1.PlayerType == PlayerType.AI
                ? _state.Player1.Level
                : _state.Player2.Level;

            // Set up parameters and clear board.
            //await WriteCommand("uct_max_memory", (1024 * 1024 * 250).ToString());

            if (level < 3)
            {
                ParseResponse(WriteCommand("uct_param_player max_games",
                    ((level + 1) * 5).ToString(CultureInfo.InvariantCulture)));
            }
            else if (level < 6)
            {
                ParseResponse(WriteCommand("uct_param_player max_games",
                    (level * 100).ToString(CultureInfo.InvariantCulture)));
            }
            else if (level < 9)
            {
                ParseResponse(WriteCommand("uct_param_player max_games",
                    (level * 1000).ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                ParseResponse(WriteCommand("uct_param_player max_games",
                    int.MaxValue.ToString(CultureInfo.InvariantCulture)));
            }

            ParseResponse(WriteCommand("komi", state.Player2.Komi.ToString(CultureInfo.InvariantCulture)));
            ParseResponse(WriteCommand("clear_board"));
            ParseResponse(WriteCommand("go_param_rules", "capture_dead 1"));

            // Set up board with some pre-existing moves.
            if (_state.GoMoveHistory.Count > 0)
            {
                // Must actually play every move back because otherwise undo operations
                // won't work.
                foreach (var m in _state.GoMoveHistory)
                {
                    string position;
                    switch (m.Move.MoveType)
                    {
                        case MoveType.Normal:
                            position = m.Move.Position;
                            break;
                        case MoveType.Pass:
                            position = "PASS";
                            break;
                        default:
                            throw new ArgumentException("Unrecognized move type: " + m.Move.MoveType);
                    }

                    ParseResponse(WriteCommand("play",
                        (m.Move.Color == GoColor.Black ? "black" : "white") + ' ' + position));
                }
            }

            _state = state;
            SaveState();
        }


        #endregion Private Helpers
    }
}
