using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KurusMatik.Data;
using KurusMatik.Models;
using KurusMatik.ViewModels;

namespace KurusMatik.Controllers
{
    [Authorize] // Giriş yapmayan kimse işlem ekleyemesin
    public class TransactionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // İşlem listesi - filtreleme destekli
        public async Task<IActionResult> Index(TransactionFilterViewModel? filter)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            // Temel sorguyu kuruyorum, sonra filtreleri uygulayacağım
            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId)
                .AsQueryable();

            // Filtreler varsa uyguluyorum - null check önemli!
            if (filter?.CategoryId.HasValue == true)
                query = query.Where(t => t.CategoryId == filter.CategoryId.Value);

            if (filter?.StartDate.HasValue == true)
                query = query.Where(t => t.Date >= filter.StartDate.Value);

            if (filter?.EndDate.HasValue == true)
                query = query.Where(t => t.Date <= filter.EndDate.Value);

            if (filter?.Type.HasValue == true)
                query = query.Where(t => t.Category!.Type == filter.Type.Value);

            // Metin araması varsa açıklama alanında arıyorum
            if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
                query = query.Where(t => t.Description != null &&
                                         t.Description.Contains(filter.SearchTerm));

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Dropdown için kategorileri de çekiyorum
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var viewModel = filter ?? new TransactionFilterViewModel();
            viewModel.Transactions = transactions;
            viewModel.Categories = categories;
            // Toplam filtreli tutarı da hesapla
            viewModel.FilteredTotal = transactions
                .Where(t => t.Category?.Type == CategoryType.Gider)
                .Sum(t => t.Amount);

            return View(viewModel);
        }

        // Yeni işlem ekleme formu - GET
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new TransactionViewModel
            {
                Date = DateTime.Today,
                AvailableCategories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.Name)
                    .ToListAsync()
            };
            return View(viewModel);
        }

        // Yeni işlem ekleme - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            // Kategori geçerli mi diye kontrol ediyorum
            var category = await _context.Categories.FindAsync(model.CategoryId);
            if (category == null)
            {
                ModelState.AddModelError("CategoryId", "Geçersiz kategori seçildi.");
            }

            if (!ModelState.IsValid)
            {
                // Hata varsa kategorileri tekrar doldurup formu göster
                model.AvailableCategories = await _context.Categories
                    .Where(c => c.IsActive).ToListAsync();
                return View(model);
            }

            var transaction = new Transaction
            {
                Amount = model.Amount,
                Description = model.Description,
                Date = model.Date,
                CategoryId = model.CategoryId,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "İşlem başarıyla eklendi!";
            return RedirectToAction(nameof(Index));
        }

        // İşlem düzenleme - GET
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            // Kayıt bulunamadı veya başka birine aitse 404 döndür
            if (transaction == null) return NotFound();

            var viewModel = new TransactionViewModel
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Description = transaction.Description,
                Date = transaction.Date,
                CategoryId = transaction.CategoryId,
                AvailableCategories = await _context.Categories
                    .Where(c => c.IsActive).ToListAsync()
            };

            return View(viewModel);
        }

        // İşlem düzenleme - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TransactionViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                model.AvailableCategories = await _context.Categories
                    .Where(c => c.IsActive).ToListAsync();
                return View(model);
            }

            // Veritabanından mevcut kaydı çekip güncelliyorum
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null) return NotFound();

            transaction.Amount = model.Amount;
            transaction.Description = model.Description;
            transaction.Date = model.Date;
            transaction.CategoryId = model.CategoryId;
            transaction.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "İşlem başarıyla güncellendi!";
            return RedirectToAction(nameof(Index));
        }

        // İşlem silme - bu AJAX ile de çağrılabilir diye JSON döndürüyorum
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null) return NotFound();

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            // Frontend developer AJAX ile bu endpoint'i çağırabilir diye JSON da döndürüyorum
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = "İşlem silindi." });

            TempData["SuccessMessage"] = "İşlem başarıyla silindi!";
            return RedirectToAction(nameof(Index));
        }

        // API endpoint - AJAX ile güncel bakiye çekmek için
        // Frontend developer Dashboard'daki anlık bakiyeyi bu endpoint ile güncelleyecek
        [HttpGet]
        public async Task<IActionResult> GetCurrentBalance()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var thisMonth = DateTime.Now.Month;
            var thisYear = DateTime.Now.Year;

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId
                         && t.Date.Month == thisMonth
                         && t.Date.Year == thisYear)
                .ToListAsync();

            var income = transactions.Where(t => t.Category?.Type == CategoryType.Gelir).Sum(t => t.Amount);
            var expense = transactions.Where(t => t.Category?.Type == CategoryType.Gider).Sum(t => t.Amount);

            return Json(new
            {
                income = income,
                expense = expense,
                balance = income - expense,
                formattedBalance = (income - expense).ToString("N2") + " ₺"
            });
        }
    }
}
