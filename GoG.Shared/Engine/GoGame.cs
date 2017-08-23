using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoG.Shared.Engine
{
    /// <summary>
    /// Game state contains information about the game and previous moves.
    /// </summary>
    public class GoGame
    {
        /// <summary>
        /// Serialization ctor only.
        /// </summary>
        public GoGame()
        {
            
        }

        public GoGame(
            byte size,
            GoPlayer player1, GoPlayer player2,
            GoGameStatus status,
            GoColor whoseTurn, 
            string blackPositions, string whitePositions,
            List<GoMoveHistoryItem> goMoveHistory)//, decimal winMargin)
            : this()
        {
            Size = size;
            Player1 = player1;
            Player2 = player2;
            Status = status;
            WhoseTurn = whoseTurn;
            BlackPositions = blackPositions;
            WhitePositions = whitePositions;
            GoMoveHistory = goMoveHistory;
            Id = Guid.NewGuid();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }

        public bool ShowingArea { get; set; }

        public GoGameStatus Status { get; set; }
        //public decimal WinMargin { get; set; }
        public GoPlayer Player1 { get; set; }
        public GoPlayer Player2 { get; set; }
        public GoOperation Operation { get; set; }

        /// <summary>
        /// Board edge size, usually 9x9, 13x13, or 19x19.
        /// </summary>
        public byte Size { get; set; }

        ///// <summary>
        ///// Number of seconds alloted for each side to play each turn.  0 for no limit.
        ///// </summary>
        //public int SecondsPerTurn { get; set; }

        /// <summary>
        /// Whose turn is it?  Black or White?
        /// </summary>
        public GoColor WhoseTurn { get; set; }

        /// <summary>
        /// Position of all the black stones.
        /// </summary>
        public string BlackPositions { get; set; }

        /// <summary>
        /// Position of all the white stones.
        /// </summary>
        public string WhitePositions { get; set; }

        public DateTime Created { get; set; }

        public List<GoMoveHistoryItem> GoMoveHistory { get; set; }
    }
}