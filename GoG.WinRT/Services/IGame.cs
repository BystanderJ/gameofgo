using System;
using System.Threading.Tasks;
using GoG.Infrastructure.Engine;
using GoG.Infrastructure.Services.Engine;


namespace GoG.WinRT.Services
{
    public interface IGame
    {
        #region Fuego
        Task<GoGameStateResponse> GetGameStateAsync();
        Task<GoGameStateResponse> StartAsync(GoGameState state);
        Task<GoMoveResponse> PlayAsync(GoMove move);
        Task<GoHintResponse> HintAsync(GoColor color);
        Task<GoMoveResponse> GenMoveAsync(GoColor color);
        Task<GoGameStateResponse> UndoAsync();
        #endregion Fuego
    }
}
