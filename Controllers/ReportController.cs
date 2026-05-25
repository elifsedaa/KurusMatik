using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KurusMatik.Data;
using KurusMatik.Models;
using KurusMatik.Services;

namespace KurusMatik.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ExcelExportService _excelService;

        public ReportController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ExcelExportService excelService)
        {
            _context = context;
            _userManager = userManager;
            _excelService = excelService;
        }

        // Rapor sayfası - hangi ay için Excel indirilmek istendiğini seçme
        public IActionResult Index()
        {
            return View();
        }

        // Excel indirme endpoint'i
        // Kullanıcı bu URL'e istek atınca Excel dosyası indirilsin
        [HttpGet]
        public async Task<IActionResult> DownloadExcel(int? month, int? year)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Challenge();

            var selectedMonth = month ?? DateTime.Now.Month;
            var selectedYear = year ?? DateTime.Now.Year;

            // Seçili aya ait tüm işlemleri çekiyorum
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId
                         && t.Date.Month == selectedMonth
                         && t.Date.Year == selectedYear)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            if (!transactions.Any())
            {
                TempData["WarningMessage"] = "Seçilen ay için işlem bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // Excel servisini çağırıyorum
            var excelBytes = _excelService.ExportTransactionsToExcel(
                transactions,
                user.FullName,
                selectedMonth,
                selectedYear
            );

            // Türkçe ay adları için basit bir dizi
            var monthNames = new[] { "", "Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran",
                                      "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik" };

            // Dosya adını Türkçe karaktersiz yapıyorum, sorun çıkarmasın
            var fileName = $"KurusMatik_{monthNames[selectedMonth]}_{selectedYear}_Raporu.xlsx";

            // FileResult olarak döndürüyorum - tarayıcı dosyayı indirecek
            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}
