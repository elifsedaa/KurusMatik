# 💰 KurusMatik – Kişisel Finans & Bütçe Takip Sistemi

> ASP.NET Core MVC (.NET 8), Entity Framework Core Code-First ve ASP.NET Core Identity tabanlı web uygulaması.
> Web Programlama dersi dönem projesi – Geliştirici 1 (Backend) kısmı.

---

## 📁 Proje Dosya Yapısı

```
KurusMatik/
├── Controllers/
│   ├── AccountController.cs      → Kayıt/Giriş/Çıkış
│   ├── AdminController.cs        → Kategori yönetimi (sadece Admin)
│   ├── BudgetGoalController.cs   → Bütçe hedefleri
│   ├── DashboardController.cs    → Ana panel + LINQ sorgular
│   ├── ReportController.cs       → Excel rapor indirme
│   └── TransactionController.cs  → Gelir/Gider CRUD
├── Data/
│   ├── AppDbContext.cs           → EF Core DbContext
│   └── DbInitializer.cs         → Admin seed data
├── Models/
│   ├── ApplicationUser.cs        → Identity kullanıcı modeli
│   ├── BudgetGoal.cs             → Bütçe hedefi modeli
│   ├── Category.cs               → Kategori modeli
│   └── Transaction.cs            → İşlem (gelir/gider) modeli
├── Services/
│   └── ExcelExportService.cs     → EPPlus ile Excel export
├── ViewModels/
│   ├── AccountViewModels.cs      → Login/Register VM'leri
│   └── DashboardViewModels.cs    → Dashboard + Transaction VM'leri
├── appsettings.json
├── Program.cs
└── KurusMatik.csproj
```

---

## 🚀 Sıfırdan Kurulum (Adım Adım)

### Gereksinimler
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) veya [VS Code](https://code.visualstudio.com/)
- SQL Server (LocalDB yeterli – VS ile birlikte gelir) ya da SQLite

---

### Adım 1 – Projeyi İndir / Klonla

Eğer GitHub'dan klonluyorsan:
```bash
git clone https://github.com/KULLANICI_ADIN/KurusMatik.git
cd KurusMatik
```

Yoksa projeyi bir klasöre kopyalayıp o klasöre gir.

---

### Adım 2 – NuGet Paketlerini Yükle

Terminal veya Package Manager Console'da proje klasöründeyken:
```bash
dotnet restore
```

Bu komut `KurusMatik.csproj` içindeki tüm paketleri indirir:
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `EPPlus` (Excel export için)

---

### Adım 3 – Veritabanı Bağlantı Ayarı

`appsettings.json` dosyasını aç ve `DefaultConnection` satırını kontrol et:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KurusMatikDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

**LocalDB kullanıyorsan** (varsayılan, Visual Studio ile gelir) – değiştirmen gerekmez.

**SQL Server Express kullanıyorsan** şu şekilde değiştir:
```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=KurusMatikDb;Trusted_Connection=True;"
```

**SQLite kullanmak istiyorsan** (daha basit):
1. `KurusMatik.csproj` içinde `SqlServer` paketini `Sqlite` ile değiştir:
   ```xml
   <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
   ```
2. `Program.cs` içinde `UseSqlServer` yerine `UseSqlite` yaz:
   ```csharp
   options.UseSqlite("Data Source=KurusMatik.db")
   ```

---

### Adım 4 – Migration Oluştur ve Veritabanını Kur

**Visual Studio kullanıyorsan** → Tools → NuGet Package Manager → Package Manager Console:
```powershell
Add-Migration InitialCreate
Update-Database
```

**Terminal (dotnet CLI) kullanıyorsan**:
```bash
# Önce EF Core araçlarını global olarak yükle (bir kere yapman yeterli)
dotnet tool install --global dotnet-ef

# Migration oluştur
dotnet ef migrations add InitialCreate

# Veritabanını oluştur ve migration'ı uygula
dotnet ef database update
```

Bu komutlar çalıştıktan sonra:
- `KurusMatikDb` adında veritabanı oluşur
- Tüm tablolar oluşur (Users, Categories, Transactions, BudgetGoals)
- Hazır kategoriler (Market, Ulaşım, Maaş vb.) otomatik eklenir

---

### Adım 5 – Projeyi Çalıştır

```bash
dotnet run
```

Veya Visual Studio'da `F5` / `Ctrl+F5`.

Tarayıcıda şu adresi aç: `https://localhost:5001` veya `http://localhost:5000`

Program.cs içindeki seed kodu **ilk çalıştırmada otomatik olarak** admin kullanıcısı oluşturur:
- 📧 **E-posta:** `admin@KurusMatik.com`
- 🔑 **Şifre:** `Admin123!`

> ⚠️ Güvenlik notu: Gerçek bir projede bu şifreyi environment variable'dan okuman gerekir.

---

### Adım 6 – Frontend Geliştiricisi İçin Controller Endpoint'leri

Frontend geliştirici bu endpoint'leri kullanacak:

| Endpoint | Metot | Açıklama |
|----------|-------|----------|
| `/Dashboard/Index?month=3&year=2025` | GET | Dashboard verisi |
| `/Transaction/GetCurrentBalance` | GET (AJAX) | Anlık bakiye JSON |
| `/Transaction/Delete/{id}` | POST (AJAX) | İşlem silme |
| `/Report/DownloadExcel?month=3&year=2025` | GET | Excel indirme |

---

## 🔧 Migrations Hakkında Ekstra Notlar

Migration sil ve yeniden oluştur (tablolarda değişiklik yaptıysan):
```bash
# Tüm migration'ları geri al
dotnet ef database update 0

# Migration klasörünü sil
rm -rf Migrations/

# Yeniden oluştur
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Mevcut veritabanını sil, temiz başla:
```bash
dotnet ef database drop --force
dotnet ef database update
```

---

## 📤 GitHub'a Yükleme (Git Rehberi)

### İlk Yükleme (Yeni Repo)

**1. GitHub'da yeni repo oluştur**
- github.com'a gir → New repository
- İsim: `KurusMatik`
- README ekleme (zaten var), .gitignore ekleme (zaten var)
- Create repository'ye tıkla

**2. Terminalde proje klasörüne git**
```bash
cd /proje/klasörü/KurusMatik
```

**3. Git başlat ve dosyaları ekle**
```bash
# Git repository oluştur
git init

# Tüm dosyaları takibe al
git add .

# İlk commit'i yap - commit mesajları Türkçe yazabilirsin
git commit -m "İlk commit: Backend temel altyapı kuruldu"
```

**4. GitHub reposunu bağla ve yükle**
```bash
# GitHub repo adresini bağla (kendi kullanıcı adınla değiştir)
git remote add origin https://github.com/KULLANICI_ADIN/KurusMatik.git

# Ana branch'i main olarak ayarla
git branch -M main

# GitHub'a gönder
git push -u origin main
```

---

### Sonraki Commits (Çalışmaya Devam Ederken)

```bash
# Değişen dosyaları ekle
git add .

# Commit mesajı yaz (anlamlı olsun, hoca okuyacak)
git commit -m "DashboardController: kategori bazlı LINQ sorguları eklendi"

# GitHub'a gönder
git push
```

---

### İki Kişi Çalışırken (Geliştirici 2 ile)

**Geliştirici 2 projeyi ilk klonlarken:**
```bash
git clone https://github.com/KULLANICI_ADIN/KurusMatik.git
cd KurusMatik
dotnet restore
dotnet ef database update
```

**Her gün çalışmadan önce güncel kodu çek:**
```bash
git pull origin main
```

**Kendi değişikliklerini göndermeden önce:**
```bash
git pull origin main   # önce çek, çakışma olmasın
git add .
git commit -m "Transaction view'ları ve AJAX endpoint'leri eklendi"
git push origin main
```

---

### Önerilen Commit Mesajı Formatı (Hoca için düzgün görünsün)

```
feat: ExcelExportService eklendi
fix: BudgetGoal cascade delete hatası düzeltildi
refactor: DashboardController LINQ sorguları optimize edildi
docs: README kurulum adımları güncellendi
```

---

## 🔑 Rol Sistemi Özeti

| Rol | Yapabilecekleri |
|-----|----------------|
| **Admin** | Kategorileri ekle/düzenle/pasife çek, admin paneline gir |
| **User** | Kendi işlemlerini ekle/düzenle/sil, bütçe hedefi kur, Excel indir |

Yeni bir kullanıcıyı Admin yapmak için (migration sonrası):
```sql
-- SQL Server Management Studio'da çalıştır
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u, AspNetRoles r
WHERE u.Email = 'kullanici@email.com' AND r.Name = 'Admin'
```

Veya `DbInitializer.cs` içine ikinci admin eklenebilir.

---

## ⚠️ Sık Karşılaşılan Hatalar

**"Build hatası: EPPlus lisans hatası"**
```csharp
// Program.cs veya ExcelExportService constructor'ına ekle:
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
```

**"Migration: Veritabanı zaten var"**
```bash
dotnet ef database drop --force
dotnet ef database update
```

**"dotnet-ef komutu bulunamadı"**
```bash
dotnet tool install --global dotnet-ef
# PATH'e eklenmiş mi diye kontrol et:
dotnet ef --version
```

---

*KurusMatik – Web Programlama Dersi Dönem Projesi*
