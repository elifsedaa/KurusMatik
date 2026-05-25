using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KurusMatik.Models
{
    // Gelir ve gider işlemlerini tutan ana modelim bu.
    // Hem geliri hem gideri aynı tabloda tutuyorum, CategoryType ile ayırt edeceğim.
    public class Transaction
    {
        public int Id { get; set; }

        // Hocam negatif değer girilmesin demişti, Range ile bunu garantiye aldım
        [Required(ErrorMessage = "Tutar zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalıdır. Negatif değer girilemez!")]
        [Display(Name = "Tutar")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Açıklama kısmı opsiyonel ama 500 karakterle sınırladım
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        // İşlem tarihi, varsayılan olarak bugünü koyuyorum
        [Required(ErrorMessage = "Tarih zorunludur.")]
        [Display(Name = "İşlem Tarihi")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        // Hangi kategoride olduğunu tutmak için foreign key
        [Required(ErrorMessage = "Kategori seçilmelidir.")]
        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }

        // Hangi kullanıcıya ait olduğunu tutuyorum, string çünkü Identity'de Id string
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Kayıt zamanını da tutayım, belki sonradan işe yarar
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Son güncelleme zamanı
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties - ilişkili tablolara buradan erişiyorum
        public Category? Category { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
