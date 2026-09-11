using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("game_room_codes")]
    public class GameRoomCode
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("event_id")]
        public int EventId { get; set; }

        [Column("game_id")]
        public int GameId { get; set; }

        [Column("room_code")]
        public string RoomCode { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}