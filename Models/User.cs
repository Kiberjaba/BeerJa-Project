using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("user_name")]
        public string Username { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("Photo")]
        public string? Photo { get; set; }

        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("is_email_confirmed")]
        public bool IsEmailConfirmed { get; set; } = false;

        // Токен подтверждения email
        [Column("email_verification_token")]
        public string? EmailVerificationToken { get; set; }

        // Срок действия токена подтверждения email
        [Column("email_verification_token_expires_at")]
        public DateTime? EmailVerificationTokenExpiresAt { get; set; }

        // Токен для сброса пароля
        [Column("password_reset_token")]
        public string? PasswordResetToken { get; set; }

        // Срок действия токена сброса пароля
        [Column("password_reset_token_expires_at")]
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        [Column("total_points")]
        public int TotalPoints { get; set; }

        [Column("level")]
        public int Level { get; set; }

        // Новые поля для поиска игроков и команд
        [Column("city")]
        public string? City { get; set; }

        [Column("games_played")]
        public int GamesPlayed { get; set; }

        [Column("is_looking_for_team")]
        public bool IsLookingForTeam { get; set; } = false;

        [Column("created_at")]
        public long CreatedAtUnix { get; set; }

        [NotMapped]
        public DateTime CreatedAt =>
            DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnix).UtcDateTime;
    }
}