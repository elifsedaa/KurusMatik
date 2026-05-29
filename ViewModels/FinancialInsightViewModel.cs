namespace KurusMatik.ViewModels
{
	// AI Coach'un ürettiği tüm analiz verilerini taşıyan model.
	// Backend hesaplar, frontend sadece bu modeli render eder.
	public class FinancialInsightViewModel
	{
		// --- Temel Sayılar ---
		public decimal TotalIncome { get; set; }
		public decimal TotalExpense { get; set; }
		public decimal NetBalance => TotalIncome - TotalExpense;

		// Tasarruf oranı: net / gelir * 100
		// Edge case: gelir 0'sa sıfır dön
		public decimal SavingsRate => TotalIncome > 0
			? Math.Round((NetBalance / TotalIncome) * 100, 1)
			: 0;

		// --- Finansal Skor (0-100) ---
		public int FinancialScore { get; set; }
		public string ScoreLevel => FinancialScore >= 70 ? "good"
								  : FinancialScore >= 40 ? "warning"
								  : "danger";
		public string ScoreEmoji => FinancialScore >= 70 ? "🏆"
								  : FinancialScore >= 40 ? "📊"
								  : "⚠️";

		// --- Kategori Analizi ---
		public string? TopExpenseCategoryName { get; set; }
		public decimal TopExpenseCategoryAmount { get; set; }
		public decimal TopExpenseCategoryPct { get; set; }

		// --- Bütçe Durumu ---
		public int TotalBudgetGoals { get; set; }
		public int ExceededBudgets { get; set; }
		public int NearLimitBudgets { get; set; } // %80 üzeri

		// --- Trend ---
		public decimal PreviousMonthExpense { get; set; }
		public decimal ExpenseTrendPct => PreviousMonthExpense > 0
			? Math.Round(((TotalExpense - PreviousMonthExpense) / PreviousMonthExpense) * 100, 1)
			: 0;
		public bool ExpenseTrendUp => TotalExpense > PreviousMonthExpense;

		// --- AI Mesajları (backend'de üretiliyor) ---
		public string MainMessage { get; set; } = string.Empty;
		public List<InsightCard> InsightCards { get; set; } = new();
	}

	// Dashboard'da gösterilecek mini insight kartları
	public class InsightCard
	{
		public string Icon { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public string Value { get; set; } = string.Empty;
		public string Level { get; set; } = "info"; // info, success, warning, danger
	}
}