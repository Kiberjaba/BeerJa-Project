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

        [Column("total_points")]
        public int TotalPoints { get; set; }

        [Column("level")]
        public int Level { get; set; }

        [Column("created_at")]
        public long CreatedAtUnix { get; set; }

        [NotMapped]
        public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnix).UtcDateTime;
    }
}