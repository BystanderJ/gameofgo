using GoG.Shared.Engine;
using Microsoft.EntityFrameworkCore;

namespace GoG.WinRT.Model
{
    public class GameContext : DbContext
    {
        public DbSet<GoGame> GoGame { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=temp3.db");
        }
    }
}