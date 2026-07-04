using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeejaServer.Models
{
    [Table("price_history")] // Указываем имя таблицы в базе
    public class PriceHistory
    {
        [Key] // Указываем, что это первичный ключ
        public int HistoryId { get; set; }
        
        public int ProductId { get; set; }
        
        public decimal Price { get; set; }
        
        public DateTime RecordedAt { get; set; }
    }
}