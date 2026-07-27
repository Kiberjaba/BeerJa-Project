using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("users")] // Убедитесь, что таблица в базе называется именно users
    public class User
    {
        [Key]
        [Column("user_id")] // Проверьте, так ли назван id в базе (может быть просто id)
        public int UserId { get; set; }

        [Column("user_name")] // <-- Указали точное имя колонки в базе данных!
        public string Username { get; set; } = string.Empty;

        [Column("email")] // Проверьте имя колонки email
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")] // Проверьте, как называется колонка с паролем (может быть password)
        public string PasswordHash { get; set; } = string.Empty;

        [Column("created_at")] // Проверьте имя колонки с датой
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}