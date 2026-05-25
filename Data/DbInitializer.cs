using Microsoft.AspNetCore.Identity;
using KurusMatik.Models;

namespace KurusMatik.Data
{
    // Uygulama ilk çalıştığında admin kullanıcısını ve rolleri oluşturacak sınıf.
    // Bu olmadan admin paneline hiçbir zaman giremezdim çünkü kayıt ekranı sadece User rolü veriyor.
    public static class DbInitializer
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Önce rolleri oluşturuyorum
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    Console.WriteLine($"[Seed] '{role}' rolü oluşturuldu.");
                }
            }

            // Admin kullanıcısı yoksa oluştur
            // Şifreyi appsettings'den almak daha güvenli olurdu ama ödev için bu yeterli
            const string adminEmail = "admin@KurusMatik.com";
            const string adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Sistem",
                    LastName = "Yöneticisi",
                    EmailConfirmed = true, // Onay gerekmesin diye true yaptım
                    RegisteredAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"[Seed] Admin kullanıcısı oluşturuldu: {adminEmail}");
                }
                else
                {
                    // Hata varsa konsola yazdır - log tutulsun
                    foreach (var error in result.Errors)
                        Console.WriteLine($"[Seed Hata] {error.Description}");
                }
            }
        }
    }
}
