using FuegoLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization; 
using System.Linq;
using System.Threading.Tasks;
using GoG.Shared;
using GoG.Shared.Engine;
using GoG.Shared.Services.Engine;

// ReSharper disable RedundantCatchClause

namespace GoG.WinRT.Services
{
    /// <summary>
    /// A single player game engine.
    /// </summary>
    public class FuegoGameEngine : IGameEngine
    {
        #region Data

        // After every operation, we use this to save the game's state to our database.
        private readonly IRepository _repository;

        // _fuego is the AI component, written in C++.  It uses threads, but it also
        // becomes unresponsive during computation, so every time we call methods 
        // on it we must create a thread.
        private FuegoInstance _fuego;

        // The current game.  If its Id does not match the desired state, must coerse the
        // _fuego instance to the desired game state, then set _state to the new game.
        private GoGame _state;

        // The other games we might need to switch to.  The key is the GoGame.Id.
        private readonly Dictionary<Guid, GoGame> _states = new Dictionary<Guid, GoGame>();

        // Used so that LoadState() will only happen once.
        private bool _isLoaded;

        #endregion Data

        #region Ctor
        public FuegoGameEngine(IRepository repository)
        {
            _repository = repository;
        }
        #endregion Ctor

        #region Fuego Implementation

        public async Task<GoResponse> CreateGameAsync(GoGame state)
        {
            try
            {
                Debug.Assert(state != null && state.Id != Guid.Empty, "state != null && state.Id != Guid.Empty");

                await LoadState();

                state.Created = DateTime.UtcNow;

                if (!_states.ContainsKey(state.Id))
                    _states.Add(state.Id, state);

                await _repository.AddGameAsync(state);

                // The following method sets _state and coerses the FuegoInstance
                // to match that state through a series of GTP commands.
                await EnsureFuegoStartedAndMatchingGame(state.Id);

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

        public async Task<GoResponse> DeleteGameAsync(Guid id)
        {
            await LoadState();

            if (_states.ContainsKey(id))
            {
                await _repository.DeleteGameAsync(id);
                _states.Remove(id);
            }

            return new GoResponse(GoResultCode.Success);
        }

        public async Task<GoGameStateResponse> GetGameStateAsync(Guid id, bool loadGame = false)
        {
            await LoadState();

            if (!_states.TryGetValue(id, out var state))
                return new GoGameStateResponse(GoResultCode.GameDoesNotExist, null);
            try
            {
                await EnsureFuegoStartedAndMatchingGame(id);
            }
            catch (GoEngineException gex)
            {
                return new GoGameStateResponse(gex.Code, null);
            }
            catch (Exception)
            {
                return new GoGameStateResponse(GoResultCode.InternalError, null);
            }
            return new GoGameStateResponse(GoResultCode.Success, state);
        }

        public async Task<GoMoveResponse> GenMoveAsync(Guid id, GoColor color)
        {
            await LoadState();

            GoMoveResponse rval = null;

            try
            {
                await Task.Run(
                        async () =>
                        {
                            await EnsureFuegoStartedAndMatchingGame(id);

                            _state.Operation = GoOperation.GenMove;

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

                            // Using kgs-genmove_cleanup for AI is important because
                            // it will allow the final_status_list dead to be calculated
                            // properly more often.
                            //
                            // This command captures the more obvious dead
                            // stones before passing, though contrary to documentation
                            // it does not capture ALL dead stones.
                            // 
                            // Using just "genmove" will more often generate PASS prematurely
                            // because it guesses too much about dead stones.  Similarly,
                            // "genmove" causes "final_status_list dead" to more often
                            // generate strange results.
                            var result = ParseResponse(WriteCommand("kgs-genmove_cleanup", color == GoColor.Black ? "black" : "white"));

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

                            await SaveState();

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
            await LoadState();

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

                            moveResult = AddMoveAndUpdateState(move);

                            await SaveState();
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

                            // This throws a GoEngineException on any failure.
                            ParseResponse(WriteCommand("play",
                                    (move.Color == GoColor.Black ? "black" : "white") + ' ' + position));

                            // Add to move history and persist new game state so user can 
                            // see what happened.
                            moveResult = AddMoveAndUpdateState(move);
                            _state.Operation = GoOperation.Idle;

                            await SaveState();
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
            await LoadState();

            GoHintResponse rval = null;
            try
            {
                await Task.Run(
                    async () =>
                    {
                        await EnsureFuegoStartedAndMatchingGame(id);

                        _state.Operation = GoOperation.Hint;

                        // Remove limitation so we can get a good hint, even on lower
                        // difficulty levels.
                        SetDifficulty(true);

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

                        await SaveState();
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
            finally
            {
                try
                {
                    // Reset to configured difficulty level.
                    SetDifficulty();
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            Debug.Assert(rval != null, "rval != null");
            return rval;
        }

        public async Task<GoGameStateResponse> UndoAsync(Guid id)
        {
            await LoadState();

            GoGameStateResponse rval = null;
            try
            {
                await Task.Run(
                    async () =>
                    {
                        await EnsureFuegoStartedAndMatchingGame(id);

                        _state.Operation = GoOperation.Undo;

                        // Note that resignation is stored as a single move, but fuego.exe doesn't know about resignations so
                        // no need to send an undo command to the engine.

                        int undo = 0;

                        if (_state.Status == GoGameStatus.Ended)
                        {
                            // GenMoveAsync() records the last PASS move, but it UNDOES the last generated PASS
                            // in our _fuego instance so that final_status_list dead will still reliably
                            // calculate dead stones (by simulation).
                            // Therefore, if status is Ended, we only need to remove one _fuege move
                            // while popping two items off of GoMoveHistory.
                            undo = 2;
                            ParseResponse(WriteCommand("gg-undo", "1"));
                        }
                        else if (_state.Status == GoGameStatus.BlackWonDueToResignation)
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

                        rval = new GoGameStateResponse(GoResultCode.Success, _state);

                        await SaveState();
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

        public async Task<GoAreaResponse> GetArea(Guid id, bool estimateDead)
        {
            await LoadState();

            if (!_states.ContainsKey(id))
                return new GoAreaResponse(GoResultCode.GameDoesNotExist);
            try
            {
                return await Task.Run(async () =>
                {

                    await EnsureFuegoStartedAndMatchingGame(id);

                    // Get dead stones.
                    if (estimateDead)
                    {
                        var dead = ParseResponse(WriteCommand("final_status_list", "dead"));
                        return CalculateAreaValues(string.Join(" ", dead.Lines));
                    }
                    return CalculateAreaValues(string.Join(" ", new string[0]));
                });
            }
            catch (GoEngineException gex)
            {
                return new GoAreaResponse(gex.Code);
            }
            catch (Exception)
            {
                return new GoAreaResponse(GoResultCode.InternalError);
            }
        }

        #endregion Fuego Implementation

        #region Private Helpers

        private void UndoMoves(int moves)
        {
            try
            {
                GetStones(); // Gets the new _state.BlackPositions and _state.WhitePositions.

                if (_state.GoMoveHistory == null)
                {
                    _state.GoMoveHistory = new List<GoMoveHistoryItem>();
                }
                for (int i = 0; i < moves; i++)
                {
                    _state.GoMoveHistory.RemoveAt(_state.GoMoveHistory.Count - 1);
                }

                // Change turn if an odd number of moves were undone.
                if (moves % 2 == 1)
                    _state.WhoseTurn = _state.WhoseTurn == GoColor.Black ? GoColor.White : GoColor.Black;

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
            { }

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

            public string Code { get; }
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
        private static MyResponse ParseEngineOutput(string it)
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

        private async Task SaveState()
        {
            try
            {
                await _repository.UpdateGameAsync(_state);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task LoadState()
        {
            if (_isLoaded)
                return;

            try
            {
                var games = await _repository.GetGamesAsync();

                _isLoaded = true;

                foreach (var game in games)
                    _states.Add(game.Id, game);
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
                        // If previous move was a pass also, mark end game, but don't record
                        // the game in the _fuego instance so that the end game 
                        // dead stone calculation will still work properly.
                        var moveCount = _state.GoMoveHistory.Count;
                        var previousMoveWasPass = moveCount >= 2 &&
                                                  _state.GoMoveHistory[moveCount - 2].Move.MoveType == MoveType.Pass;
                        if (previousMoveWasPass)
                        {
                            _state.Status = GoGameStatus.Ended;
                            ParseResponse(WriteCommand("gg-undo", "1"));
                        }
                        break;
                }

                rval.Status = _state.Status;
            }
            catch (Exception)
            {
                throw;
            }

            return rval;
        }

        /// <summary>
        /// Call after each move to save the stone positions.
        /// </summary>
        private void GetStones()
        {
            var result = ParseResponse(WriteCommand("list_stones", "black"));
            _state.BlackPositions = result.Msg;

            result = ParseResponse(WriteCommand("list_stones", "white"));
            _state.WhitePositions = result.Msg;
        }

        //private string CalculateGameResult()
        //{
        //    var result = ParseResponse(WriteCommand("final_score"));

        //    return result.Msg;
        //}

        private async Task EnsureFuegoStartedAndMatchingGame(Guid id)
        {
            await Task.Run(() =>
            {
                // We don't want callers to handle GameDoesNotExist errors.
                _states.TryGetValue(id, out var state);
                Debug.Assert(state != null,
                    $"Don't call {nameof(EnsureFuegoStartedAndMatchingGame)} with the ID of a game that does not exist!");

                var needSaveAndRestartEngine = false;
                if (_fuego == null)
                {
                    _fuego = new FuegoInstance();
                    needSaveAndRestartEngine = true;
                }
                else if (_state == null || _state.Id != id)
                    needSaveAndRestartEngine = true;

                if (!needSaveAndRestartEngine)
                    return;

                _state = state;

                Debug.Assert(
                    _states.ContainsKey(_state.Id) &&
                    _states[_state.Id] == _state,
                    "_state or _state.Id is not in _states dct!");

                _fuego.StartGame(_state.Size);

                // Set up parameters and clear board.
                //await WriteCommand("uct_max_memory", (1024 * 1024 * 250).ToString());


                SetDifficulty();

                ParseResponse(WriteCommand("komi", _state.Player2.Komi.ToString(CultureInfo.InvariantCulture)));
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
            });
        }

        private void SetDifficulty(bool removeLimit = false)
        {
            var level = _state.Player1.PlayerType == PlayerType.Ai
                ? _state.Player1.Level
                : _state.Player2.Level;

            if (!removeLimit && level < 9)
            {
                var maxGames = Math.Round(Math.Pow(level + 1, 4.3)) + 2;
                ParseResponse(WriteCommand("uct_param_player max_games",
                    maxGames.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                // No limitation for hardest level.
                // No limitation is also used when generating hints.
                ParseResponse(WriteCommand("uct_param_player max_games",
                    int.MaxValue.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private GoAreaResponse CalculateAreaValues(string dead)
        {
            var rval = new GoAreaResponse(GoResultCode.Success);

            // Split into arrays, and work out dead/undead white and black subsets.
            var deads = dead.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var blacks = _state.BlackPositions.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            rval.BlackDead = deads.Where(d => blacks.Any(b => b == d)).ToList();
            var nonDeadBlacks = blacks.Where(b => deads.All(p => p != b)).Select(p => p.EncodePosition(_state.Size)).ToArray();
            var whites = _state.WhitePositions.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            rval.WhiteDead = deads.Where(d => whites.Any(w => w == d)).ToList();
            var nonDeadWhites = whites.Where(w => deads.All(p => p != w)).Select(p => p.EncodePosition(_state.Size)).ToArray();

            // Build game grid based on _state.BlackPositions and _state.WhitePositions,
            // excluding any dead stones.
            var grid = new GoColor?[_state.Size, _state.Size];
            foreach (var p in nonDeadWhites)
                grid[p.X, p.Y] = GoColor.White;
            foreach (var p in nonDeadBlacks)
                grid[p.X, p.Y] = GoColor.Black;

            // For each held position, determine the number of neighboring liberties
            // that are empty and don't eventually touch a different color.
            rval.BlackArea = GetContiguousAreaForHeldPoints(GoColor.Black, nonDeadBlacks);
            rval.WhiteArea = GetContiguousAreaForHeldPoints(GoColor.White, nonDeadWhites);

            // Local functions are cool.
            List<string> GetContiguousAreaForHeldPoints(GoColor color, Point[] heldPoints)
            {
                var result = new List<string>(_state.Size ^ 2);

                foreach (var held in heldPoints)
                {
                    foreach (var p in GetNeighborPositions(held))
                    {
                        // If is a liberty (no color), traverse it.
                        if (grid[p.X, p.Y] == null)
                        {
                            Debug.Assert(grid[held.X, held.Y].HasValue, "grid[held.X, held.Y].HasValue");

                            var liberties = new List<Point>(_state.Size ^ 2);
                            if (!AllNeighborsAreLibertiesOrColor(p, color, liberties))
                                continue;
                            foreach (var l in liberties)
                            {
                                var s = EngineHelpers.DecodePosition(l.X, l.Y, _state.Size);
                                if (!result.Contains(s))
                                    result.Add(s);
                            }
                        }
                    }

                    // Add held point (which is alive) because we want 
                    // area not just territory.
                    var heldDecoded = EngineHelpers.DecodePosition(held.X, held.Y, _state.Size);
                    if (!result.Contains(heldDecoded))
                        result.Add(heldDecoded);
                }

                return result;
            }

            // Local functions are cool.
            bool AllNeighborsAreLibertiesOrColor(Point point, GoColor color, List<Point> visited)
            {
                visited.Add(point);

                // Traverse through not already visited neighbors.
                foreach (var neighbor in GetNeighborPositions(point))
                {
                    if (visited.Contains(neighbor))
                        continue;

                    var neighborColor = grid[neighbor.X, neighbor.Y];

                    // If neighbors is a liberty (no color), traverse it.
                    if (neighborColor == null)
                    {
                        // Self-recursive call.
                        if (!AllNeighborsAreLibertiesOrColor(neighbor, color, visited))
                            // A neighbor was opposite color, abort up the chain.
                            return false;
                    }
                    else if (neighborColor == OppositeColor(color))
                    {
                        // Neighbor is opposite color, abort all the way up the chain.
                        return false;
                    }

                    // Neighbor is same color, this is a boundary, don't traverse it
                    // but continue to next neighbor.
                }
                return true;
            }

            // Local functions are cool.
            // Iterates over the neighbors.  Using yield
            // allows caller to abort without iterating
            // all neighboring points.  References
            // _state.Size to determine edges of the board.
            IEnumerable<Point> GetNeighborPositions(Point p)
            {
                // has a left
                if (p.X > 0)
                    yield return new Point(p.X - 1, p.Y);
                // has a right
                if (p.X < _state.Size - 1)
                    yield return new Point(p.X + 1, p.Y);

                // has an upper
                if (p.Y > 0)
                    yield return (new Point(p.X, p.Y - 1));
                // has a lower
                if (p.Y < _state.Size - 1)
                    yield return new Point(p.X, p.Y + 1);
            }

            return rval;
        }

        private static GoColor OppositeColor(GoColor color)
        {
            return color == GoColor.Black ? GoColor.White : GoColor.Black;
        }

        #endregion Private Helpers
    }
}