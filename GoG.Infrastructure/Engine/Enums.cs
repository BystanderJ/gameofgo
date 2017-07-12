﻿using System.Runtime.Serialization;

namespace GoG.Infrastructure.Engine
{
    
    public enum GoOperation
    {
        
        Idle,
        
        Starting,
        
        NormalMove,
        
        Pass,
        
        Resign,
        
        GenMove,
        
        Hint,
        
        Undo
    }

    /// <summary>
    /// These enumerations correspond to the values in the Moves table MoveType field in the database.
    /// DO NOT change these Id values.
    /// </summary>
    
    public enum MoveType
    {
        
        Normal = 0,
        
        Pass = 1,
        
        Resign = 2
    }

    
    public enum GoGameStatus
    {
        
        Active = 0,
        
        BlackWon = 1,
        
        WhiteWon = 2,
        
        BlackWonDueToResignation = 3,
        
        WhiteWonDueToResignation = 4
    }

    
    public enum PlayerType
    {
        
        Human = 0,
        
        AI = 1,
        
        Remote = 2
    }

    /// <summary>
    /// A code to be sent to the client.
    /// </summary>
    
    public enum GoResultCode
    {
        /// <summary>
        /// No error.
        /// </summary>
        
        Success,

        /// <summary>
        /// Client side also generates these when there are connectivity errors, but 
        /// could be generated server side, too.
        /// </summary>
        
        CommunicationError,

        /// <summary>
        /// An error happened on the server that the client shouldn't know about.
        /// </summary>
        
        InternalError,

        /// <summary>
        /// The GameId passed in doesn't exist (or doesn't belong to the current user).
        /// </summary>
        
        GameDoesNotExist,

        /// <summary>
        /// The GameId passed in belongs to a game that already exists.
        /// </summary>
        
        GameAlreadyExists,

        /// <summary>
        /// Game state between server and client does not match.  This usually means the client needs to correct the situation
        /// (or the client is trying to cheat).
        /// </summary>
        
        ClientOutOfSync,

        /// <summary>
        /// Only one move or action can be performed for a single game at one time.  If multiple calls are made per game,
        /// they are rejected.
        /// </summary>
        
        SimultaneousRequests,
        
        IllegalMoveSpaceOccupied,
        
        IllegalMoveSuicide,
        
        IllegalMoveSuperKo,
        
        OtherIllegalMove,

        /// <summary>
        /// Other error output by the engine.
        /// </summary>
        
        OtherEngineError,

        /// <summary>
        /// This engine has reached the maximum number of players and can't handle any more right now.
        /// Currently this means the server can only start a certain number of instances of fuego.exe to
        /// move requests.  Clients should simply show a busy signal for 5 seconds, then
        /// try again.
        /// </summary>
        
        EngineBusy,

        /// <summary>
        /// This an odd error output by the engine sometimes.  I suspect it may be when the engine is restricted
        /// to a very low number of "maximum simulated games" (used to limit its strength), but that's a guess.
        /// It's not clear how client client should handle this, so let's hope it doesn't happen too often.
        /// </summary>
        
        CannotScore,
        
        CannotSaveSGF
    }
}