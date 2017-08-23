using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoG.Shared.Engine;
using GoG.WinRT.Model;
using Microsoft.Data.Sqlite;
using Microsoft.Data.Sqlite.Internal;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

// ReSharper disable RedundantCatchClause
#pragma warning disable 168

namespace GoG.WinRT.Services
{
    public class SqliteRepository : IRepository
    {
        public async Task Initialize()
        {
            // Configure library to use SDK version of SQLite, which is shared
            // therefore possibly faster load.
            SqliteEngine.UseWinSqlite3();

            using (var db = new GameContext())
                await db.Database.MigrateAsync();
        }

        public async Task AddGameAsync(GoGame game)
        {
            try
            {
                using (var db = new GameContext())
                {
                    db.GoGame.Add(game);
                    await db.SaveChangesAsync();
                }
            }
            catch (JsonException jex)
            {
                //throw;
            }
            catch (SqliteException slex)
            {
                //throw;
            }
            catch (Exception ex)
            {
                //throw;
            }
        }
        public async Task UpdateGameAsync(GoGame game)
        {
            try
            {
                using (var db = new GameContext())
                {
                    db.GoGame.Update(game);
                    await db.SaveChangesAsync();
                }
            }
            catch (JsonException jex)
            {
                //throw;
            }
            catch (SqliteException slex)
            {
                //throw;
            }
            catch (Exception ex)
            {
                //throw;
            }
        }

        public async Task DeleteGameAsync(Guid id)
        {
            try
            {
                using (var db = new GameContext())
                {
                    await db.Database.ExecuteSqlCommandAsync($"DELETE FROM GoGame WHERE Id = \"{id}\";");
                }
            }
            catch (JsonException jex)
            {
                //throw;
            }
            catch (SqliteException slex)
            {
                //throw;
            }
            catch (Exception ex)
            {
                //throw;
            }
        }

        public async Task<IEnumerable<GoGame>> GetGamesAsync()
        {
            try
            {
                using (var db = new GameContext())
                    return await db.GoGame
                        .Include(game => game.GoMoveHistory)
                        .ThenInclude(gmh => gmh.Move)
                        .Include(game => game.GoMoveHistory)
                        .ThenInclude(gmh => gmh.Result)
                        .Include(game => game.Player1)
                        .Include(game => game.Player2)
                        .ToListAsync();
            }
            catch (JsonException jex)
            {
                //throw;
            }
            catch (SqliteException slex)
            {
                //throw;
            }
            catch (Exception ex)
            {
                //throw;
            }
            return new GoGame[0];
        }
    }
}
