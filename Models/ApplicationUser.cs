using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace KurusMatik.Models
{
    // IdentityUser'dan kalıtım alarak kendi kullanıcı modelimi oluşturuyorum.
    // Burada isim ve soyisim gibi ekstra bilgileri tutacağım.
    public class ApplicationUser : IdentityUser
    {
        // Kullanıcının adını tutuyorum, zorunlu alan yaptım
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        // Soyisim de zorunlu
        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        // Kayıt tarihi - otomatik set edeceğim
        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        // Navigation properties - bu kullanıcıya ait işlemler ve bütçe hedefleri
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<BudgetGoal> BudgetGoals { get; set; } = new List<BudgetGoal>();

        // Tam adı döndüren bir yardımcı property ekledim, layout'ta işe yarayacak
        public string FullName => $"{FirstName} {LastName}";
    }
}
