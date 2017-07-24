using GoG.Infrastructure.Engine;
using Microsoft.EntityFrameworkCore;

namespace GoG.WinRT.Model
{
    public class GameContext : DbContext
    {
        public DbSet<GoGame> GoGame { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=temp2.db");
        }
    }
}