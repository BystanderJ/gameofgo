﻿using System.Collections.Generic;
using System.Linq;

namespace GoG.Shared.Engine
{
    /// <summary>
    /// This represents the result of a move.  This information is to be passed back to the client
    /// so the client can update the UI.
    /// </summary>
    public class GoMoveResult
    {
        public GoMoveResult() { }

        /// <summary>
        /// This constructor calculates the captured stones for you.
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        public GoMoveResult(string before, string after) => CapturedStones = DetermineCapturedStones(before, after);

        public GoMoveResult(string capturedStones) => CapturedStones = capturedStones;

        private static string DetermineCapturedStones(string before, string after)
        {
            var b = before.Split(' ');
            var a = after.Split(' ');

            var rval = new List<string>();
            foreach (var s in b)
                if (!a.Contains(s))
                    rval.Add(s);

            return rval.CombineStrings();
        }

        public int Id { get; set; }

        public string CapturedStones { get; set; }
        
        public GoGameStatus Status { get; set; }
        
        //public decimal WinMargin { get; set; }
    }
}