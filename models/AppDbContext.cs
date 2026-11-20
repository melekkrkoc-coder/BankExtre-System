using Microsoft.EntityFrameworkCore;

namespace BankaExtreAnaliz.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Veritabanında oluşacak tablolarımız
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CategoryRule> CategoryRules { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        // İlk kurulumda temel kategorileri otomatik ekleyelim
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Market & Gıda" },
                new Category { Id = 2, Name = "Fatura & Abonelik" },
                new Category { Id = 3, Name = "Ulaşım & Akaryakıt" },
                new Category { Id = 4, Name = "Yeme - İçme" },
                new Category { Id = 5, Name = "Maaş / Gelir" },
                new Category { Id = 6, Name = "Diğer" }
            );
        }
    }
}