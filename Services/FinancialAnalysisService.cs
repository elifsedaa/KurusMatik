using Microsoft.EntityFrameworkCore;
using KurusMatik.Data;
using KurusMatik.Models;
using KurusMatik.ViewModels;

namespace KurusMatik.Services
{
    // Tüm finansal analiz hesaplarını burada yapıyorum.
    // Controller şişmesin diye ayrı servise aldım.
    public class FinancialAnalysisService
    {
        private readonly AppDbContext _context;

        public FinancialAnalysisService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FinancialInsightViewModel> AnalyzeAsync(
            string userId, int month, int year,
            List<Transaction> monthlyTransactions,
            List<BudgetAlertViewModel> budgetAlerts)
        {
            var insight = new FinancialInsightViewModel();

            // --- Temel hesaplar ---
            // Burada decimal ile çalışıyorum, string formatı yok
            insight.TotalIncome = monthlyTransactions
                .Where(t => t.Category?.Type == CategoryType.Gelir)
                .Sum(t => t.Amount);

            insight.TotalExpense = monthlyTransactions
                .Where(t => t.Category?.Type == CategoryType.Gider)
                .Sum(t => t.Amount);

            // --- En yüksek harcama kategorisi ---
            var topCat = monthlyTransactions
                .Where(t => t.Category?.Type == CategoryType.Gider)
                .GroupBy(t => t.Category!.Name)
                .Select(g => new { Name = g.Key, Total = g.Sum(t => t.Amount) })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            if (topCat != null)
            {
                insight.TopExpenseCategoryName = topCat.Name;
                insight.TopExpenseCategoryAmount = topCat.Total;
                insight.TopExpenseCategoryPct = insight.TotalIncome > 0
                    ? Math.Round((topCat.Total / insight.TotalIncome) * 100, 1)
                    : 0;
            }

            // --- Bütçe istatistikleri ---
            insight.TotalBudgetGoals = budgetAlerts.Count;
            insight.ExceededBudgets = budgetAlerts.Count(a => a.IsExceeded);
            insight.NearLimitBudgets = budgetAlerts.Count(a => !a.IsExceeded && a.UsagePercentage >= 80);

            // --- Geçen ayın gider verisi (trend için) ---
            var prevMonth = month == 1 ? 12 : month - 1;
            var prevYear = month == 1 ? year - 1 : year;

            insight.PreviousMonthExpense = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId
                         && t.Date.Month == prevMonth
                         && t.Date.Year == prevYear
                         && t.Category!.Type == CategoryType.Gider)
                .SumAsync(t => t.Amount);

            // --- Finansal skor hesabı ---
            insight.FinancialScore = CalculateScore(insight, budgetAlerts);

            // --- AI mesajlarını backend'de üretiyorum ---
            // Böylece frontend'de parse sorunu olmaz
            insight.MainMessage = GenerateMainMessage(insight);
            insight.InsightCards = GenerateInsightCards(insight);

            return insight;
        }

        // Finansal skor algoritması: 100 puandan başla, duruma göre ekle/çıkar
        private int CalculateScore(FinancialInsightViewModel insight, List<BudgetAlertViewModel> budgetAlerts)
        {
            int score = 50; // Başlangıç puanı

            // Tasarruf oranına göre puan
            if (insight.SavingsRate >= 30) score += 30;
            else if (insight.SavingsRate >= 20) score += 20;
            else if (insight.SavingsRate >= 10) score += 10;
            else if (insight.SavingsRate >= 0) score += 0;
            else score -= 20; // Açık vermişse büyük eksi

            // Bütçe aşımına göre ceza
            score -= insight.ExceededBudgets * 8;
            score -= insight.NearLimitBudgets * 3;

            // Gelir varsa küçük bonus (en azından kullanıyor)
            if (insight.TotalIncome > 0) score += 5;

            // Gider trendi iyiyse bonus
            if (insight.PreviousMonthExpense > 0 && !insight.ExpenseTrendUp) score += 10;

            // 0-100 aralığında tut
            return Math.Max(0, Math.Min(100, score));
        }

        // Ana AI mesajını backend'de üretiyorum, sayıları decimal olarak kullanıyorum
        private string GenerateMainMessage(FinancialInsightViewModel insight)
        {
            if (insight.TotalIncome == 0)
                return "Bu ay henüz gelir girişi yapılmamış. Gelirlerinizi ekleyin, size daha detaylı analiz yapayım.";

            // Tasarruf oranına göre farklı mesajlar
            if (insight.SavingsRate < 0)
            {
                var overPct = Math.Abs(insight.SavingsRate);
                return $"Bu ay gelirinizin %{overPct}'i kadar fazla harcama yaptınız. " +
                       $"Net: -{FormatAmount(Math.Abs(insight.NetBalance))} ₺. Harcamalarınızı gözden geçirmenizi öneririm.";
            }

            if (insight.SavingsRate < 10)
                return $"Tasarruf oranınız %{insight.SavingsRate} — bu oldukça düşük. " +
                       $"Aylık {FormatAmount(insight.NetBalance)} ₺ biriktiriyorsunuz. Hedef en az %20 olmalı.";

            if (insight.SavingsRate < 20)
                return $"Tasarruf oranınız %{insight.SavingsRate}. " +
                       $"Bu ay {FormatAmount(insight.NetBalance)} ₺ biriktirdiniz — iyi bir başlangıç, biraz daha artırabilirsiniz.";

            if (insight.SavingsRate < 40)
                return $"Gelirinizin %{insight.SavingsRate}'ini biriktiriyorsunuz. " +
                       $"Bu ay {FormatAmount(insight.NetBalance)} ₺ tasarruf ettiniz. Finansal uzmanlar bunu yeterli buluyor.";

            return $"Harika! Gelirinizin %{insight.SavingsRate}'ini biriktiriyorsunuz. " +
                   $"Bu ay {FormatAmount(insight.NetBalance)} ₺ tasarruf ettiniz. Finansal hedeflerinize hızla yaklaşıyorsunuz!";
        }

        // Mini insight kartlarını da backend'de hazırlıyorum
        private List<InsightCard> GenerateInsightCards(FinancialInsightViewModel insight)
        {
            var cards = new List<InsightCard>();

            // Tasarruf oranı kartı
            cards.Add(new InsightCard
            {
                Icon = insight.SavingsRate >= 20 ? "💰" : insight.SavingsRate >= 0 ? "📊" : "📉",
                Title = "Tasarruf Oranı",
                Value = $"%{insight.SavingsRate}",
                Level = insight.SavingsRate >= 20 ? "success" : insight.SavingsRate >= 10 ? "warning" : "danger"
            });

            // En yüksek harcama kartı
            if (insight.TopExpenseCategoryName != null)
            {
                cards.Add(new InsightCard
                {
                    Icon = "💸",
                    Title = "En Yüksek Gider",
                    Value = $"{insight.TopExpenseCategoryName} ({FormatAmount(insight.TopExpenseCategoryAmount)} ₺)",
                    Level = insight.TopExpenseCategoryPct > 40 ? "warning" : "info"
                });
            }

            // Bütçe durumu kartı
            if (insight.TotalBudgetGoals > 0)
            {
                cards.Add(new InsightCard
                {
                    Icon = insight.ExceededBudgets > 0 ? "🚨" : "🎯",
                    Title = "Bütçe Durumu",
                    Value = insight.ExceededBudgets > 0
                        ? $"{insight.ExceededBudgets} kategori aşıldı"
                        : "Tüm hedefler kontrol altında",
                    Level = insight.ExceededBudgets > 0 ? "danger" : "success"
                });
            }

            // Trend kartı
            if (insight.PreviousMonthExpense > 0)
            {
                cards.Add(new InsightCard
                {
                    Icon = insight.ExpenseTrendUp ? "📈" : "📉",
                    Title = "Gider Trendi",
                    Value = insight.ExpenseTrendUp
                        ? $"Geçen aya göre %{insight.ExpenseTrendPct} arttı"
                        : $"Geçen aya göre %{Math.Abs(insight.ExpenseTrendPct)} azaldı",
                    Level = insight.ExpenseTrendUp ? "warning" : "success"
                });
            }

            return cards;
        }

        // Sayıları Türkçe formatla — sadece görüntüleme için, hesapta kullanmıyorum
        private string FormatAmount(decimal amount)
        {
            return amount.ToString("N2", new System.Globalization.CultureInfo("tr-TR"));
        }
    }
}