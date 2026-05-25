using KurusMatik.Models;

namespace KurusMatik.ViewModels
{
    // Dashboard sayfasına gerekli tüm verileri taşımak için bu ViewModel'i kullandım.
    // Controller'da hesaplanan her şey buraya doluyor, View sadece gösteriyor.
    public class DashboardViewModel
    {
        // Temel finansal özet
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal CurrentBalance => TotalIncome - TotalExpense; // Bakiye hesabı otomatik

        // Aylık filtre - hangi ay gösteriliyor
        public int SelectedMonth { get; set; } = DateTime.Now.Month;
        public int SelectedYear { get; set; } = DateTime.Now.Year;

        // Grafik için kategori bazlı harcama verileri
        // Frontend developer bunu alıp Chart.js'e verecek
        public List<CategoryExpenseSummary> CategoryExpenses { get; set; } = new();
        public List<CategoryExpenseSummary> CategoryIncomes { get; set; } = new();

        // Son 5 işlem - hızlı bakış için
        public List<Transaction> RecentTransactions { get; set; } = new();

        // Bütçe hedefi uyarıları - aşılan hedefler burada
        public List<BudgetAlertViewModel> BudgetAlerts { get; set; } = new();

        // Aylık gelir/gider trend verisi (son 6 ay) - çizgi grafik için
        public List<MonthlyTrendData> MonthlyTrend { get; set; } = new();
    }

    // Kategori bazlı özet için yardımcı sınıf
    public class CategoryExpenseSummary
    {
        public string CategoryName { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        // Toplam içindeki yüzde hesabı - grafik tooltip'i için
        public decimal Percentage { get; set; }
    }

    // Bütçe aşımı uyarısı için yardımcı model
    public class BudgetAlertViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public decimal MonthlyLimit { get; set; }
        public decimal CurrentSpending { get; set; }
        public decimal RemainingAmount => MonthlyLimit - CurrentSpending;
        // Yüzde hesabı: 100'ü geçerse aşım var demektir
        public decimal UsagePercentage => MonthlyLimit > 0 ? (CurrentSpending / MonthlyLimit) * 100 : 0;
        public bool IsExceeded => CurrentSpending > MonthlyLimit;
        // Uyarı seviyesi: %80'in üzeri sarı, %100 kırmızı
        public string AlertLevel => UsagePercentage >= 100 ? "danger" : UsagePercentage >= 80 ? "warning" : "success";
    }

    // Aylık trend için yardımcı model
    public class MonthlyTrendData
    {
        public string MonthLabel { get; set; } = string.Empty; // "Ocak 2025" gibi
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }

    // Transaction oluşturma/düzenleme formu için ViewModel
    public class TransactionViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public int CategoryId { get; set; }

        // Dropdown'da gösterilecek kategoriler listesi
        public List<Category> AvailableCategories { get; set; } = new();
    }

    // İşlem listesi sayfasında filtreleme için
    public class TransactionFilterViewModel
    {
        public int? CategoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
        public CategoryType? Type { get; set; } // Gelir mi Gider mi filtresi

        public List<Transaction> Transactions { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public decimal FilteredTotal { get; set; }
    }
}
