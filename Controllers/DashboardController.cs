using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KurusMatik.Data;
using KurusMatik.Models;
using KurusMatik.ViewModels;
using System.Globalization;
using KurusMatik.Services;

namespace KurusMatik.Controllers
{
    // Dashboard sadece giriş yapmış kullanıcılara görünsün
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FinancialAnalysisService _analysisService;



        public DashboardController(AppDbContext context, UserManager<ApplicationUser> userManager,
            FinancialAnalysisService analysisService)
        {
            _context = context;
            _userManager = userManager;
            _analysisService = analysisService;
        }

        // Ana dashboard sayfası - burada LINQ sorgularım var
        public async Task<IActionResult> Index(int? month, int? year)
        {
            // Giriş yapmış kullanıcının ID'sini alıyorum
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            // Filtre için ay/yıl belirliyorum, parametre gelmezse bu ayı göster
            var selectedMonth = month ?? DateTime.Now.Month;
            var selectedYear = year ?? DateTime.Now.Year;

            // --- LINQ SORGUSU 1: Seçili ayın işlemlerini çekiyorum ---
            // Kullanıcıya ait, seçili aya ait işlemleri kategori bilgisiyle birlikte çekiyorum
            var monthlyTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId
                         && t.Date.Month == selectedMonth
                         && t.Date.Year == selectedYear)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            // --- LINQ SORGUSU 2: Toplam gelir ve gider hesabı ---
            // CategoryType.Gelir olanları Sum ile topluyorum
            var totalIncome = monthlyTransactions
                .Where(t => t.Category?.Type == CategoryType.Gelir)
                .AsEnumerable().Sum(t => (double)t.Amount);

            var totalExpense = monthlyTransactions
                .Where(t => t.Category?.Type == CategoryType.Gider)
                .AsEnumerable().Sum(t => (double)t.Amount);

            // --- LINQ SORGUSU 3: Kategori bazlı gider gruplandırması ---
            // Burada GroupBy kullandım, her kategori için toplam tutar hesaplıyorum
            var categoryExpenses = monthlyTransactions
                .Where(t => t.Category?.Type == CategoryType.Gider)
                .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category.ColorHex, t.Category.IconClass })
                .Select(g => new CategoryExpenseSummary
                {
                    CategoryName = g.Key.Name,
                    ColorHex = g.Key.ColorHex,
                    IconClass = g.Key.IconClass,
                    TotalAmount = g.AsEnumerable().Sum(t => (double)t.Amount),
                    TransactionCount = g.Count(),
                    // Yüzde hesabı: bu kategorinin toplam harcama içindeki payı
                    Percentage = totalExpense > 0 ? Math.Round((g.AsEnumerable().Sum(t => (double)t.Amount) / totalExpense) * 100, 1) : 0
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            // Aynı şeyi gelirler için de yapıyorum
            var categoryIncomes = monthlyTransactions
                .Where(t => t.Category?.Type == CategoryType.Gelir)
                .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category.ColorHex, t.Category.IconClass })
                .Select(g => new CategoryExpenseSummary
                {
                    CategoryName = g.Key.Name,
                    ColorHex = g.Key.ColorHex,
                    IconClass = g.Key.IconClass,
                    TotalAmount = g.AsEnumerable().Sum(t => (double)t.Amount),
                    TransactionCount = g.Count(),
                    Percentage = totalIncome > 0 ? Math.Round((g.AsEnumerable().Sum(t => (double)t.Amount) / totalIncome) * 100, 1) : 0
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            // --- LINQ SORGUSU 4: Son 5 işlem (hızlı bakış için) ---
            var recentTransactions = monthlyTransactions.Take(5).ToList();

            // --- LINQ SORGUSU 5: Bütçe hedeflerini kontrol ediyorum ---
            var budgetAlerts = await GetBudgetAlertsAsync(userId, selectedMonth, selectedYear);

            // --- AI Coach analizi: backend'de hesaplanıyor ---
            // Frontend'e decimal gönderiyorum, string parse sorunu olmayacak
            var financialInsight = await _analysisService.AnalyzeAsync(
                userId, selectedMonth, selectedYear,
                monthlyTransactions, budgetAlerts);

            // --- LINQ SORGUSU 6: Son 6 ayın trend verisi ---
            // Bu sorguyu biraz daha karmaşık yapmak zorunda kaldım
            var monthlyTrend = await GetMonthlyTrendAsync(userId, selectedYear);

            // Tüm verileri ViewModel'e doldurup View'a gönderiyorum
            var viewModel = new DashboardViewModel
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                SelectedMonth = selectedMonth,
                SelectedYear = selectedYear,
                CategoryExpenses = categoryExpenses,
                CategoryIncomes = categoryIncomes,
                RecentTransactions = recentTransactions,
                BudgetAlerts = budgetAlerts,
                MonthlyTrend = monthlyTrend,
                FinancialInsight = financialInsight
            };

            return View(viewModel);
        }

        // --- YARDIMCI METOT: Bütçe Uyarılarını Hesapla ---
        // Bu metodu ayrı yazdım çünkü Dashboard action'ı çok büyümeye başladı
        private async Task<List<BudgetAlertViewModel>> GetBudgetAlertsAsync(string userId, int month, int year)
        {
            // Bu ay aktif olan bütçe hedeflerini çekiyorum
            var budgetGoals = await _context.BudgetGoals
                .Include(b => b.Category)
                .Where(b => b.UserId == userId
                         && b.IsActive
                         && b.StartDate <= new DateTime(year, month, 1).AddMonths(1).AddDays(-1))
                .ToListAsync();

            var alerts = new List<BudgetAlertViewModel>();

            foreach (var goal in budgetGoals)
            {
                // Bu kategori için seçili aydaki harcamayı hesaplıyorum
                var spending = await _context.Transactions
                    .Where(t => t.UserId == userId
                             && t.CategoryId == goal.CategoryId
                             && t.Date.Month == month
                             && t.Date.Year == year
                             && t.Category!.Type == CategoryType.Gider)
                    .AsEnumerable().Sum(t => (double)t.Amount);

                alerts.Add(new BudgetAlertViewModel
                {
                    CategoryName = goal.Category!.Name,
                    ColorHex = goal.Category.ColorHex,
                    MonthlyLimit = goal.MonthlyLimit,
                    CurrentSpending = spending
                });
            }

            // Sadece %70'in üzerinde olanları göstereyim, çok gürültü olmasın
            return alerts
                .Where(a => a.UsagePercentage >= 70)
                .OrderByDescending(a => a.UsagePercentage)
                .ToList();
        }

        // --- YARDIMCI METOT: Aylık Trend Verisi ---
        // Son 6 ayın gelir-gider verilerini çizgi grafik için hazırlıyorum

        //versiyon 2

        // Tek sorguda tüm yılın verisini çek, sonra bellekte grupla
        private async Task<List<MonthlyTrendData>> GetMonthlyTrendAsync(string userId, int year)
        {
            var turkishMonths = new[] { "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
                                 "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

            // Tek sorgu - bu yılın tüm işlemleri
            var allTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.Date.Year == year)
                .ToListAsync();

            var trend = new List<MonthlyTrendData>();

            for (int m = 1; m <= DateTime.Now.Month; m++)
            {
                var monthData = allTransactions.Where(t => t.Date.Month == m).ToList();
                trend.Add(new MonthlyTrendData
                {
                    MonthLabel = turkishMonths[m],
                    Income = monthData.Where(t => t.Category?.Type == CategoryType.Gelir).AsEnumerable().Sum(t => (double)t.Amount),
                    Expense = monthData.Where(t => t.Category?.Type == CategoryType.Gider).AsEnumerable().Sum(t => (double)t.Amount)
                });
            }

            return trend;
        }




        //private async Task<List<MonthlyTrendData>> GetMonthlyTrendAsync(string userId, int year)
        //{
        //    var trend = new List<MonthlyTrendData>();
        //    var turkishMonths = new[] { "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        //                                 "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

        //    // Bu yılın her ayı için veri hesaplıyorum
        //    for (int m = 1; m <= DateTime.Now.Month; m++)
        //    {
        //        var monthTransactions = await _context.Transactions
        //            .Include(t => t.Category)
        //            .Where(t => t.UserId == userId
        //                     && t.Date.Month == m
        //                     && t.Date.Year == year)
        //            .ToListAsync();

        //        trend.Add(new MonthlyTrendData
        //        {
        //            MonthLabel = turkishMonths[m],
        //            Income = monthTransactions.Where(t => t.Category?.Type == CategoryType.Gelir).Sum(t => t.Amount),
        //            Expense = monthTransactions.Where(t => t.Category?.Type == CategoryType.Gider).Sum(t => t.Amount)
        //        });
        //    }

        //    return trend;
        //}
    }
}
