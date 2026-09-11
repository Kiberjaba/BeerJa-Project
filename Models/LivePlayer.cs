using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("live_players")]
    public class LivePlayer
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("game_id")]
        public int GameId { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.Now;
        
        [Column("user_id")]
        public int UserId { get; set; }
    }
}