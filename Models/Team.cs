using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("teams")]
    public class Team
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public required string Name { get; set; }

        [Column("creator_id")]
        public int CreatorId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("team_members")]
    public class TeamMember
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("team_id")]
        public int TeamId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("status")]
        public required string Status { get; set; } = "pending";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}