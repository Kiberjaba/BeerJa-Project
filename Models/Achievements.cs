using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("achievements")]
    public class Achievement
    {
        [Key]
        [Column("achievement_id")]
        public int AchievementId { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("condition")]
        public string Condition { get; set; } = string.Empty;

        [Column("points_reward")]
        public int PointsReward { get; set; }
    }
}