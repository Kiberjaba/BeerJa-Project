using Microsoft.EntityFrameworkCore;
using BeejaServer.Models;

namespace BeejaServer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<EventCheckin> EventCheckins { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }
        public DbSet<GameSession> GameSessions { get; set; }
        public DbSet<LiveStepResponse> LiveStepResponses { get; set; }
        public DbSet<GameQuestion> GameQuestions { get; set; }
        public DbSet<GameRoomCode> GameRoomCodes { get; set; }
        public DbSet<LivePlayer> LivePlayers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                // Индексы уникальности
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();

                // Разрешаем NULL для PasswordHash, чтобы Yandex OAuth не падала
                entity.Property(u => u.PasswordHash).IsRequired(false);
            });
        }
    }
}