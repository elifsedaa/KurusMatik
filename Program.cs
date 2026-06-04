using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KurusMatik.Data;
using KurusMatik.Models;
using KurusMatik.Services;

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";

var builder = WebApplication.CreateBuilder(args);

// --- SERVİSLER ---

// EF Core ile SQL Server bağlantısını kuruyorum
// Connection string'i appsettings.json'dan okuyorum
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity servislerini ekliyorum
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Şifre kuralları - çok katı olmayacak, ödev projesi sonuçta
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false; // Büyük harf zorunlu değil
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Hesap kilitleme ayarları
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Kullanıcı adı yerine email kullanacağım
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>() // Kullanıcıları EF ile saklıyorum
.AddDefaultTokenProviders();

// Giriş/çıkış ayarları
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8); // 8 saat oturum açık kalsın
    options.SlidingExpiration = true; // Her istekte oturum süresi sıfırlanıyor
});

// Excel servisimi DI konteynerine ekliyorum - Scoped veya Singleton olabilir
builder.Services.AddScoped<ExcelExportService>();

builder.Services.AddScoped<FinancialAnalysisService>();

// MVC servisleri
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- MIDDLEWARE ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // wwwroot klasörü için

app.UseRouting();

// Authentication ve Authorization - sıralama önemli!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");
// Ana sayfa direkt dashboard'a gitsin

// --- UYGULAMA BAŞLARKEN SEED DATA ---
// DbInitializer'ı burada çağırıyorum
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Migration'ları otomatik uygula
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        // Admin ve rolleri oluştur
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbInitializer.SeedAsync(userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Seed data yüklenirken hata oluştu.");
    }
}

app.Run($"http://0.0.0.0:{port}");
