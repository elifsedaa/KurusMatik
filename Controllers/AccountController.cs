using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KurusMatik.Models;
using KurusMatik.ViewModels;

namespace KurusMatik.Controllers
{
    // Giriş/Çıkış ve Kayıt işlemlerini bu controller üstleniyor.
    // [AllowAnonymous] koymak zorunda kaldım yoksa giriş yapmamış kullanıcı
    // zaten giriş sayfasına erişemez, mantıksız olur.
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        // Dependency injection ile gerekli servisleri alıyorum
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // Kayıt sayfasını GET ile açıyorum
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            // Zaten giriş yapmış biri tekrar kayıt olmaya çalışırsa dashboard'a yönlendiriyorum
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        // Kayıt formundan POST ile veri geliyor
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken] // CSRF saldırısına karşı koruma
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Validasyon hatası varsa formu tekrar göster
                return View(model);
            }

            // ApplicationUser nesnesi oluşturuyorum, ViewModel'den modele dönüşüm
            var user = new ApplicationUser
            {
                UserName = model.Email, // Identity UserName olarak email kullanıyorum
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                RegisteredAt = DateTime.Now
            };

            // Kullanıcıyı veritabanına kaydet
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Yeni kullanıcı kayıt oldu: {Email}", model.Email);

                // Varsayılan olarak "User" rolü atıyorum
                // Rol yoksa önce oluşturmam gerekiyor
                await EnsureRoleExistsAsync("User");
                await _userManager.AddToRoleAsync(user, "User");

                // Kayıt başarılı, otomatik giriş yap
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Dashboard");
            }

            // Identity'nin döndürdüğü hataları ModelState'e ekliyorum
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Giriş sayfasını GET ile açıyorum
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Giriş formundan POST geliyor
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // Şifre ile giriş deniyorum
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true // 5 başarısız denemede hesabı kilitlesin
            );

            if (result.Succeeded)
            {
                _logger.LogInformation("Kullanıcı giriş yaptı: {Email}", model.Email);

                // Redirect injection saldırısını önlemek için IsLocalUrl kontrolü yapıyorum
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Hesap kilitlendi: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Hesabınız çok fazla başarısız deneme nedeniyle geçici olarak kilitlendi.");
                return View(model);
            }

            // Yanlış şifre veya e-posta - güvenlik için hangisinin yanlış olduğunu söylemiyorum
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        // Çıkış işlemi - POST olması lazım, GET ile yapılırsa CSRF riski oluşur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Kullanıcı çıkış yaptı.");
            return RedirectToAction("Login", "Account");
        }

        // Erişim reddedildi sayfası
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // --- YARDIMCI METOT ---
        // Rol var mı diye kontrol edip yoksa oluşturan özel metodum
        private async Task EnsureRoleExistsAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
                _logger.LogInformation("{RoleName} rolü oluşturuldu.", roleName);
            }
        }
    }
}
