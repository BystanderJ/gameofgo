﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoG.Shared
{
    public static class StringExtensions
    {
        public static string DetermineCapturedStones(string before, string after)
        {
            string[] b = before.Split(' ');
            string[] a = after.Split(' ');

            var rval = new List<string>();
            foreach (var s in b)
                if (!a.Contains(s))
                    rval.Add(s);
            return rval.CombineStrings();
        }

        /// <summary>
        /// Takes a string[], returns a single, space separated string.  Used for taking an array
        /// of stone positions e.g. { "Q1", "F13", "B9" } and changing it into the database
        /// representation which is a simple space-separated string like: "Q1 F13 B9". 
        /// </summary>
        public static string CombineStrings(this IEnumerable<string> strings, string separator = " ")
        {
            var rval = new StringBuilder(1500);
            var list = strings as List<string> ?? strings.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                rval.Append(s);
                if (i < list.Count - 1)
                    rval.Append(separator);
            }
            return rval.ToString();
        }
    }
}
