using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("articles")]
    public class Article
    {
        [Key]
        [Column("text_id")]
        public string TextId { get; set; } = string.Empty;

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("excerpt")]
        public string Excerpt { get; set; } = string.Empty;

        [Column("content")]
        public string? Content { get; set; }

        [Column("tag")]
        public string Tag { get; set; } = string.Empty;

        [Column("read_time")]
        public string ReadTime { get; set; } = string.Empty;

        [Column("is_featured")]
        public bool IsFeatured { get; set; } = false;

        [Column("published_at")]
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    }
}