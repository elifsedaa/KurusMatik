using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KurusMatik.Data;
using KurusMatik.Models;

namespace KurusMatik.Controllers
{
    // Bu controller sadece Admin rolüne sahip kullanıcılar görebilsin.
    // [Authorize(Roles = "Admin")] ile bunu garantiye aldım.
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Kategori listesi
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // Yeni kategori ekleme - GET
        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View(new Category());
        }

        // Yeni kategori ekleme - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.Categories.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{model.Name}' kategorisi başarıyla eklendi.";
            return RedirectToAction(nameof(Categories));
        }

        // Kategori düzenleme - GET
        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // Kategori düzenleme - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, Category model)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // Sadece güvenli alanları güncelliyorum
            category.Name = model.Name;
            category.Description = model.Description;
            category.ColorHex = model.ColorHex;
            category.IconClass = model.IconClass;
            category.IsActive = model.IsActive;
            // Type'ı değiştirmeye izin vermiyorum - işlemler karışır

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Kategori güncellendi.";
            return RedirectToAction(nameof(Categories));
        }

        // Kategori silmek yerine pasife çekiyorum, veri kaybı olmasın
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();

            var status = category.IsActive ? "aktif" : "pasif";
            TempData["SuccessMessage"] = $"'{category.Name}' kategorisi {status} yapıldı.";
            return RedirectToAction(nameof(Categories));
        }

        // Admin paneli - basit bir istatistik sayfası
        public async Task<IActionResult> Index()
        {
            // Toplam kullanıcı sayısını ve işlem istatistiklerini gösteriyorum
            var stats = new
            {
                TotalCategories = await _context.Categories.CountAsync(),
                ActiveCategories = await _context.Categories.CountAsync(c => c.IsActive),
                TotalTransactions = await _context.Transactions.CountAsync(),
                TotalBudgetGoals = await _context.BudgetGoals.CountAsync(b => b.IsActive)
            };

            ViewBag.Stats = stats;
            return View();
        }
    }
}
