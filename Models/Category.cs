using System.ComponentModel.DataAnnotations;

namespace KurusMatik.Models
{
    // Gelir/Gider kategorilerini tutmak için bu modeli kullandım.
    // Admin bu tabloyu yönetecek, kullanıcılar sadece okuyabilecek.
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı boş bırakılamaz.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Kategori adı 2 ile 100 karakter arasında olmalıdır.")]
        [Display(Name = "Kategori Adı")]
        public string Name { get; set; } = string.Empty;

        // Açıklama isteğe bağlı olsun
        [StringLength(250, ErrorMessage = "Açıklama en fazla 250 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        // Bu enum ile kategori tipi belirliyorum: Gelir mi Gider mi?
        [Required(ErrorMessage = "Kategori tipi seçilmelidir.")]
        [Display(Name = "Kategori Tipi")]
        public CategoryType Type { get; set; }

        // Arayüzde renk göstermek için bir hex renk kodu saklıyorum (ör: "#FF5733")
        [StringLength(7)]
        public string ColorHex { get; set; } = "#6c757d";

        // İkon için Font Awesome class adı tutuyorum (ör: "fa-utensils")
        [StringLength(50)]
        public string IconClass { get; set; } = "fa-tag";

        // Bu kategorinin aktif mi pasif mi olduğunu takip ediyorum
        public bool IsActive { get; set; } = true;

        // Navigation property - bu kategorideki işlemlere buradan ulaşabilirim
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<BudgetGoal> BudgetGoals { get; set; } = new List<BudgetGoal>();
    }

    // Kategori tipini enum olarak tanımladım, string yerine int saklanacak DB'de
    public enum CategoryType
    {
        Gelir = 0,
        Gider = 1
    }
}
