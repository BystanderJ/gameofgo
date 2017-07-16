using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GoG.Infrastructure.Engine
{
    /// <summary>
    /// Game state contains information about the game and previous moves.
    /// </summary>
    [DataContract]
    public class GoGame
    {
        /// <summary>
        /// Serialization ctor only.
        /// </summary>
        public GoGame() { }

        public GoGame(
            byte size,
            GoPlayer player1, GoPlayer player2,
            GoGameStatus status,
            GoColor whoseTurn, 
            string blackPositions, string whitePositions,
            List<GoMoveHistoryItem> goMoveHistory, decimal winMargin)
        {
            Size = size;
            Player1 = player1;
            Player2 = player2;
            Status = status;
            WhoseTurn = whoseTurn;
            BlackPositions = blackPositions;
            WhitePositions = whitePositions;
            GoMoveHistory = goMoveHistory;
            WinMargin = winMargin;
        }

        [DataMember]
        public GoGameStatus Status { get; set; }
        [DataMember]
        public decimal WinMargin { get; set; }
        [DataMember]
        public GoPlayer Player1 { get; set; }
        [DataMember]
        public GoPlayer Player2 { get; set; }
        [DataMember]
        public GoOperation Operation { get; set; }

        /// <summary>
        /// Board edge size, usually 9x9, 13x13, or 19x19.
        /// </summary>
        [DataMember]
        public byte Size { get; set; }

        ///// <summary>
        ///// Number of seconds alloted for each side to play each turn.  0 for no limit.
        ///// </summary>
        //public int SecondsPerTurn { get; set; }

        /// <summary>
        /// Whose turn is it?  Black or White?
        /// </summary>
        [DataMember]
        public GoColor WhoseTurn { get; set; }

        /// <summary>
        /// Position of all the black stones.
        /// </summary>
        [DataMember]
        public string BlackPositions { get; set; }

        /// <summary>
        /// Position of all the white stones.
        /// </summary>
        [DataMember]
        public string WhitePositions { get; set; }
        [DataMember]
        public List<GoMoveHistoryItem> GoMoveHistory { get; set; }
    }
}