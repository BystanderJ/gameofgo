using System.Collections.Generic;
using GoG.Shared.Engine;

namespace GoG.Shared.Services.Engine
{
    /// <summary>
    /// A list of positions response.
    /// </summary>
    public class GoAreaResponse : GoResponse
    {
        public GoAreaResponse(GoResultCode code) : base(code)
        { }

        public List<string> BlackDead { get; set; }
        public List<string> WhiteDead { get; set; }
        public List<string> BlackArea { get; set; }
        public List<string> WhiteArea { get; set; }
    }

    public struct Point
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }

}