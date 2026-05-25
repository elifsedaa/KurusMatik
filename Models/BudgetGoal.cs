using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KurusMatik.Models
{
    // Bütçe hedeflerini tutacak model. Kullanıcı belirli bir kategoride
    // aylık ne kadar harcayabileceğini burada belirleyecek.
    public class BudgetGoal
    {
        public int Id { get; set; }

        // Hangi kullanıcının bütçe hedefi
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Hangi kategori için bütçe hedefi (ör: Yemek kategorisi)
        [Required(ErrorMessage = "Kategori seçilmelidir.")]
        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }

        // Aylık limit - yine negatif olamaz diye Range koydum
        [Required(ErrorMessage = "Aylık limit tutarı zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Limit tutarı 0'dan büyük olmalıdır.")]
        [Display(Name = "Aylık Limit")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyLimit { get; set; }

        // Bu hedef hangi tarihten itibaren geçerli
        [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
        [Display(Name = "Başlangıç Tarihi")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        // Bitiş tarihi opsiyonel, belirsiz süreyle de hedef kurulabilsin
        [Display(Name = "Bitiş Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        // Hedef aktif mi değil mi
        public bool IsActive { get; set; } = true;

        // Oluşturulma zamanı
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Category? Category { get; set; }
    }
}
