using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoG.Shared.Engine;

namespace GoG.WinRT.Services
{
    public interface IRepository
    {
        Task Initialize();
        Task AddGameAsync(GoGame game);
        Task UpdateGameAsync(GoGame game);
        Task DeleteGameAsync(Guid id);
        Task<IEnumerable<GoGame>> GetGamesAsync();
    }
}
