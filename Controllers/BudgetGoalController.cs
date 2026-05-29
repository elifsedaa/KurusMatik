using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KurusMatik.Data;
using KurusMatik.Models;

namespace KurusMatik.Controllers
{
    [Authorize]
    public class BudgetGoalController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BudgetGoalController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Bütçe hedefleri listesi
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var goals = await _context.BudgetGoals
                .Include(b => b.Category)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.IsActive)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(goals);
        }

        // Yeni bütçe hedefi oluşturma - GET
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Sadece gider kategorilerini göster, gelir için bütçe hedefi olmaz
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive && c.Type == CategoryType.Gider)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(new BudgetGoal { StartDate = DateTime.Today });
        }

        // Yeni bütçe hedefi oluşturma - POST

        //versiyon 2

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BudgetGoal model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            // UserId'yi ÖNCE set ediyorum, sonra ModelState kontrolü yapıyorum
            // Çünkü [Required] attribute'u validation sırasında UserId'nin dolu olmasını bekliyor
            model.UserId = userId;

            // UserId artık dolu olduğu için ModelState.IsValid doğru sonuç verecek
            ModelState.Remove("UserId"); // Eğer zaten hata eklenmiş olursa temizle

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories
                    .Where(c => c.IsActive && c.Type == CategoryType.Gider)
                    .ToListAsync();
                return View(model);
            }

            // Aynı kategoride aktif hedef var mı?
            var existingGoal = await _context.BudgetGoals
                .FirstOrDefaultAsync(b => b.UserId == userId
                                       && b.CategoryId == model.CategoryId
                                       && b.IsActive);

            if (existingGoal != null)
            {
                ModelState.AddModelError("CategoryId", "Bu kategori için zaten aktif bir bütçe hedefiniz var.");
                ViewBag.Categories = await _context.Categories
                    .Where(c => c.IsActive && c.Type == CategoryType.Gider).ToListAsync();
                return View(model);
            }

            // UserId zaten set edildi yukarıda, tekrar set etmeye gerek yok
            model.CreatedAt = DateTime.Now;

            _context.BudgetGoals.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bütçe hedefi oluşturuldu!";
            return RedirectToAction(nameof(Index));
        }


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(BudgetGoal model)
        //{
        //    var userId = _userManager.GetUserId(User);
        //    if (userId == null) return Challenge();

        //    if (!ModelState.IsValid)
        //    {
        //        ViewBag.Categories = await _context.Categories
        //            .Where(c => c.IsActive && c.Type == CategoryType.Gider)
        //            .ToListAsync();
        //        return View(model);
        //    }

        //    // Aynı kategoride zaten aktif bir hedef var mı diye kontrol ediyorum
        //    var existingGoal = await _context.BudgetGoals
        //        .FirstOrDefaultAsync(b => b.UserId == userId
        //                               && b.CategoryId == model.CategoryId
        //                               && b.IsActive);

        //    if (existingGoal != null)
        //    {
        //        ModelState.AddModelError("CategoryId", "Bu kategori için zaten aktif bir bütçe hedefiniz var.");
        //        ViewBag.Categories = await _context.Categories
        //            .Where(c => c.IsActive && c.Type == CategoryType.Gider).ToListAsync();
        //        return View(model);
        //    }

        //    model.UserId = userId;
        //    model.CreatedAt = DateTime.Now;

        //    _context.BudgetGoals.Add(model);
        //    await _context.SaveChangesAsync();

        //    TempData["SuccessMessage"] = "Bütçe hedefi oluşturuldu!";
        //    return RedirectToAction(nameof(Index));
        //}

        // Bütçe hedefini aktif/pasif yap
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var goal = await _context.BudgetGoals
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (goal == null) return NotFound();

            goal.IsActive = !goal.IsActive;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Hedef silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var goal = await _context.BudgetGoals
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (goal == null) return NotFound();

            _context.BudgetGoals.Remove(goal);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bütçe hedefi silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
