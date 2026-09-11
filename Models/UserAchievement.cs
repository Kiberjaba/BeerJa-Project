using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("user_achievements")]
    public class UserAchievement
    {
        [Key]
        [Column("user_achievement_id")]
        public int UserAchievementId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("achievement_id")]
        public int AchievementId { get; set; }

        [Column("earned_at")]
        public DateTime EarnedAt { get; set; }
    }
}