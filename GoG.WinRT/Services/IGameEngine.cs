using System;
using System.Threading.Tasks;
using GoG.Infrastructure.Engine;
using GoG.Infrastructure.Services.Engine;

namespace GoG.WinRT.Services
{
    /// <summary>
    /// Performs Go operations for multiple simultanous games.  We use GoResponse 
    /// objects since these operations could fail, and they might need to happen 
    /// partially across the wire.  However, to calculate things locally it is 
    /// likely that a FuegoInstance will still need to be utilized internally.
    /// Operations should store state of each game constantly so if we are suspended
    /// or crash or quit, games can be restored.
    /// </summary>
    public interface IGameEngine
    {
        #region Fuego
        Task<GoGameStateResponse> GetGameStateAsync(Guid id, bool loadGame = false);
        Task<GoResponse> CreateGameAsync(GoGame state);
        Task<GoResponse> DeleteGameAsync(Guid id);
        Task<GoMoveResponse> PlayAsync(Guid id, GoMove move);
        Task<GoHintResponse> HintAsync(Guid id, GoColor color);
        Task<GoMoveResponse> GenMoveAsync(Guid id, GoColor color);
        Task<GoGameStateResponse> UndoAsync(Guid id);
        Task<GoAreaResponse> GetArea(Guid id, bool active);

        #endregion Fuego
    }
}
