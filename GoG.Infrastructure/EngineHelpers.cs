using GoG.Infrastructure.Engine;
using System;
using System.Diagnostics;
using System.Text;
using GoG.Infrastructure.Services.Engine;

namespace GoG.Infrastructure
{
    public static class EngineHelpers
    {
        public static Point EncodePosition(this string p, int edgeSize)
        {
            p.EncodePosition(edgeSize, out var x, out var y);
            return new Point(x, y);
        }

        /// <summary>
        /// Converts a Go position like A15 to a board point, which is indexed at 0 and the Y axis is inverted.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="edgeSize"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static void EncodePosition(this string p, int edgeSize, out int x, out int y)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            try
            {
                var firstCharArray = Encoding.UTF8.GetBytes(new[] { p[0] });
                x = firstCharArray[0] - 65;
                // I is skipped.
                if (x >= 8)
                    x--;
                var y2 = p.Substring(1);
                y = edgeSize - Convert.ToInt32(y2);

                Debug.Assert(x < edgeSize && y < edgeSize 
                    && x >= 0 && y >= 0, $"{nameof(EncodePosition)}({p}) yielded out of bounds X:{x} Y:{y}");
            }
            catch (Exception)
            {
                throw new Exception("Parameter p is not a valid Go coordinate.  Value was: " + p);
            }
        }

        /// <summary>
        /// Inverts the Y axis and moves to index base A and 1 instead of 0,0.
        /// </summary>
        /// <returns></returns>
        public static string DecodePosition(int x, int y, int edgeSize)
        {
            string rval = GetColumnLetter(x) + (edgeSize - y);
            return rval;
        }

        public static string GetColumnLetter(int x)
        {
            // I is skipped.
            if (x >= 8)
                x++;

            char[] charsOf = Encoding.UTF8.GetChars(new[] { (byte)(x + 65) });
            string rval = charsOf[0].ToString();

            return rval;
        }

        public static string GetResultCodeFriendlyMessage(GoResultCode code)
        {
            string msg;
            switch (code)
            {
                case GoResultCode.CommunicationError:
                    msg = "Communication error.  Are you connected to the Internet?";
                    break;
                case GoResultCode.EngineBusy:
                    msg = "Server is playing too many simultaneous games.  Please wait a minute and try again.";
                    break;
                case GoResultCode.OtherEngineError:
                case GoResultCode.InternalError:
                    msg = "Something blew up!  Please try again.";
                    break;
                case GoResultCode.GameAlreadyExists:
                    msg = "Game already exists.  Please try again.";
                    break;
                case GoResultCode.GameDoesNotExist:
                    msg = "Your game was aborted due to inactivity.  Please start another one.";
                    break;
                case GoResultCode.ClientOutOfSync:
                    msg = "Your game was out of sync.";
                    break;
                case GoResultCode.SimultaneousRequests:
                    msg =
                        "Are you playing this game on another device right now?  If so, please leave and re-enter the game.";
                    break;
                case GoResultCode.Success:
                    msg = String.Empty;
                    break;
                case GoResultCode.IllegalMoveSpaceOccupied:
                    msg = "That space is occupied.";
                    break;
                case GoResultCode.IllegalMoveSuicide:
                    msg = "That move is suicide, which is not allowed.";
                    break;
                case GoResultCode.IllegalMoveSuperKo:
                    msg = "That move would replicate a previous board position, which violates the \"superko\" rule.";
                    break;
                case GoResultCode.OtherIllegalMove:
                    msg = "That move is not legal.";
                    break;
                case GoResultCode.CannotScore:
                    msg = "There are one or more stones that may be dead (or not).  Please continue playing until this situation is resolved.";
                    break;
                case GoResultCode.CannotSaveSgf:
                    msg = "Cannot save SGF.";
                    break;
                default:
                    throw new Exception("Unsupported value for GoResultCode: " + code);
            }
            return msg;
        }
    }
}
