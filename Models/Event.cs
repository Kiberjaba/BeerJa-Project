using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("events")] 
    public class Event
    {
        [Key]
        [Column("event_id")]
        public int EventId { get; set; }

        [Required]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("format")]
        public string? Format { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("lat")]
        public decimal? Lat { get; set; }

        [Column("lng")]
        public decimal? Lng { get; set; }

        [Column("points_reward")]
        public int PointsReward { get; set; }

        [Column("starts_at")]
        public DateTime StartsAt { get; set; }

        [Column("ends_at")]
        public DateTime EndsAt { get; set; }

        [Column("organiser_user_id")]
        public int? OrganiserUserId { get; set; }

        [Column("qr_code")]
        public string? QrCode { get; set; }

        [Column("status")]
        public string Status { get; set; } = "draft";

        // Новые поля, которые мы добавляли для личного кабинета ведущего:

        [Column("progress_percentage")]
        public int ProgressPercentage { get; set; } = 0;

        [Column("game_type")]
        public int GameType { get; set; } = 2;
    }
}