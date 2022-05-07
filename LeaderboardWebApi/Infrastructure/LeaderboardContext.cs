using LeaderboardWebApi.Models;
using Microsoft.EntityFrameworkCore;
using MinimalLeaderboardWebAPI.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaderboardWebApi.Infrastructure
{
    public class LeaderboardContext : DbContext
    {
        public LeaderboardContext(DbContextOptions<LeaderboardContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Use entity configuration
            modelBuilder.ApplyConfiguration(new GamerConfiguration());

            // Or configure entity here
            modelBuilder.Entity<Score>()
                .ToTable("Scores")
                .HasData(
                    new Score() { Id = 1, GamerId = 1, Points = 1234, Game = "Pac-man" },
                    new Score() { Id = 2, GamerId = 2, Points = 424242, Game = "Donkey Kong" }
                );
        }

        public DbSet<Gamer> Gamers { get; set; }
        public DbSet<Score> Scores { get; set; }
    }
}
