using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [Column("order_id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("game_id")]
        public int GameId { get; set; }

        [Column("final_price")]
        public decimal FinalPrice { get; set; }

        [Column("ordered_at")]
        public DateTime OrderedAt { get; set; } = DateTime.Now;
    }
}