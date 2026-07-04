using Microsoft.EntityFrameworkCore;
using BeejaServer.Models;

namespace BeejaServer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Это дорога к SQL таблицам 
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<PriceHistory> PriceHistory { get; set; }
        public DbSet<User> Users { get; set; }
    }
}