using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("users")] // Имя таблицы в базе
    public class User
    {
        [Key]
        [Column("user_id")] 
        public int UserId { get; set; }

        [Column("username")]
        public string? Username { get; set; }

        [Column("table_number")] 
        public int TableNumber { get; set; }

        [Column("current_game_id")] 
        public int CurrentGameId { get; set; }
    }
}