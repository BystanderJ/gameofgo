using System;
using System.Threading.Tasks;
using GoG.Infrastructure.Engine;
using GoG.Infrastructure.Services.Engine;

namespace GoG.WinRT.Services
{
    public interface IGameEngine
    {
        #region Fuego
        Task<GoGameStateResponse> GetGameStateAsync(Guid id);
        Task<GoResponse> CreateGameAsync(GoGame state);
        Task<GoResponse> DeleteGameAsync(Guid id);
        Task<GoMoveResponse> PlayAsync(Guid id, GoMove move);
        Task<GoHintResponse> HintAsync(Guid id, GoColor color);
        Task<GoMoveResponse> GenMoveAsync(Guid id, GoColor color);
        Task<GoGameStateResponse> UndoAsync(Guid id);
        #endregion Fuego
    }
}
