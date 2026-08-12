using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("event_checkins")]
    public class EventCheckin
    {
        [Key]
        [Column("checkin_id")]
        public int CheckinId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("event_id")]
        public int EventId { get; set; }

        [Column("checked_in_at")]
        public DateTime CheckedInAt { get; set; }

        [Column("points_awarded")]
        public int PointsAwarded { get; set; }

        // Связь с таблицей Events
        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }
    }
}