using Microsoft.EntityFrameworkCore;
using ChatEmotionBackend.Models; // User ve Message sınıfları burada

namespace ChatEmotionBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        // Kullanıcı tablosu
        public DbSet<User> Users { get; set; }
        // Mesaj tablosu
        public DbSet<Message> Messages { get; set; }
    }
}
