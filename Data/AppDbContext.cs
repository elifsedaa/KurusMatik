using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KurusMatik.Models;

namespace KurusMatik.Data
{
    // IdentityDbContext kullanıyorum çünkü Identity tablolarını da bu context üzerinden yönetmek istiyorum
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet'lerimi tanımlıyorum - bunlar veritabanındaki tablolara karşılık geliyor
        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<BudgetGoal> BudgetGoals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Üst sınıfın OnModelCreating metodunu çağırmayı unutmuyorum,
            // yoksa Identity tabloları düzgün oluşmuyor
            base.OnModelCreating(modelBuilder);

            // --- İLİŞKİ TANIMLARI ---

            // Transaction -> ApplicationUser ilişkisi
            // Kullanıcı silinirse onun tüm işlemleri de silinsin (Cascade Delete)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Transaction -> Category ilişkisi
            // Kategori silinirse işlemler silinmesin, null kalsın (bunu kasıtlı yaptım,
            // admin bir kategori silerse kullanıcının geçmiş işlemleri kaybolmasın)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // BudgetGoal -> ApplicationUser ilişkisi
            // Kullanıcı silinirse bütçe hedefleri de silinsin
            modelBuilder.Entity<BudgetGoal>()
                .HasOne(b => b.User)
                .WithMany(u => u.BudgetGoals)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // BudgetGoal -> Category ilişkisi
            // Aynı mantık, kategori silinirse bütçe hedefi kaybolmasın
            modelBuilder.Entity<BudgetGoal>()
                .HasOne(b => b.Category)
                .WithMany(c => c.BudgetGoals)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- SEED DATA (Başlangıç Verileri) ---
            // Uygulama ilk açıldığında bazı hazır kategoriler ekleyeyim
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Maaş", Type = CategoryType.Gelir, ColorHex = "#28a745", IconClass = "fa-money-bill-wave", Description = "Aylık maaş geliri" },
                new Category { Id = 2, Name = "Freelance", Type = CategoryType.Gelir, ColorHex = "#17a2b8", IconClass = "fa-laptop-code", Description = "Serbest çalışma gelirleri" },
                new Category { Id = 3, Name = "Kira Geliri", Type = CategoryType.Gelir, ColorHex = "#6f42c1", IconClass = "fa-home", Description = "Kira gelirleri" },
                new Category { Id = 4, Name = "Diğer Gelir", Type = CategoryType.Gelir, ColorHex = "#fd7e14", IconClass = "fa-plus-circle", Description = "Diğer gelir kalemleri" },
                new Category { Id = 5, Name = "Market & Yiyecek", Type = CategoryType.Gider, ColorHex = "#dc3545", IconClass = "fa-shopping-cart", Description = "Market alışverişi ve yemek giderleri" },
                new Category { Id = 6, Name = "Ulaşım", Type = CategoryType.Gider, ColorHex = "#ffc107", IconClass = "fa-car", Description = "Toplu taşıma, yakıt, taksi" },
                new Category { Id = 7, Name = "Faturalar", Type = CategoryType.Gider, ColorHex = "#6c757d", IconClass = "fa-file-invoice", Description = "Elektrik, su, internet, doğalgaz" },
                new Category { Id = 8, Name = "Sağlık", Type = CategoryType.Gider, ColorHex = "#20c997", IconClass = "fa-heartbeat", Description = "Sağlık ve ilaç giderleri" },
                new Category { Id = 9, Name = "Eğlence", Type = CategoryType.Gider, ColorHex = "#e83e8c", IconClass = "fa-film", Description = "Sinema, konsert, aktiviteler" },
                new Category { Id = 10, Name = "Eğitim", Type = CategoryType.Gider, ColorHex = "#007bff", IconClass = "fa-graduation-cap", Description = "Kurs, kitap, eğitim materyalleri" },
                new Category { Id = 11, Name = "Giyim", Type = CategoryType.Gider, ColorHex = "#fd7e14", IconClass = "fa-tshirt", Description = "Kıyafet ve aksesuar" },
                new Category { Id = 12, Name = "Diğer Gider", Type = CategoryType.Gider, ColorHex = "#343a40", IconClass = "fa-minus-circle", Description = "Diğer gider kalemleri" }
            );
        }
    }
}
