using System;
using GoG.Shared.Engine;

namespace GoG.Board.Interface
{
    public interface IGenerateMoves
    {
        event EventHandler<GoMoveResultEventArgs> GoMoveChangedEvent;
    }


    public class GoMoveResultEventArgs : EventArgs
    {
        public GoMoveResult MoveResult { get; set; }
    }
}
