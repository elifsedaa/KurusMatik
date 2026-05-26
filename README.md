# KurusMatik - Proje Dokümantasyonu ve Kurulum Kılavuzu

KurusMatik, ASP.NET Core MVC mimarisi üzerine kurulu, Entity Framework Core Code-First yaklaşımı ve ASP.NET Core Identity güvenlik altyapısı ile geliştirilmiş bir kişisel finans ve bütçe yönetim sistemidir.

Bu doküman, projenin backend altyapısı ve Windows XP temalı dinamik frontend entegrasyonuna ait teknik detayları, klasör yapısını ve kurulum adımlarını içermektedir.

---

## 1. Proje Yapısı ve Katmanlar

```
KurusMatik/
├── Controllers/
│   ├── AccountController.cs       # Kimlik doğrulama, kayıt ve oturum yönetimi
│   ├── AdminController.cs        # Sistem kategorilerinin yönetimi (Rol korumalı)
│   ├── BudgetGoalController.cs   # Kategori bazlı bütçe limitlerinin yönetimi
│   ├── DashboardController.cs    # Veri analitiği ve LINQ sorgu sonuçlarının işlenmesi
│   ├── ReportController.cs       # Finansal raporlama ve servis tetikleyicileri
│   └── TransactionController.cs  # Gelir ve gider kayıtlarının CRUD operasyonları
├── Data/
│   ├── AppDbContext.cs           # Veritabanı context yapısı ve Fluent API ilişkileri
│   └── DbInitializer.cs          # Rollerin ve varsayılan sistem verilerinin seed edilmesi
├── Models/
│   ├── ApplicationUser.cs        # Genişletilmiş Identity kullanıcı modeli
│   ├── BudgetGoal.cs             # Bütçe limitleri entite modeli
│   ├── Category.cs               # Gelir/Gider kategorileri veri modeli
│   └── Transaction.cs            # Finansal hareketlerin kayıt modeli
├── Services/
│   └── ExcelExportService.cs     # EPPlus kütüphanesi veri dışa aktarım servisi
├── ViewModels/
│   └── AllViewModels.cs          # Arayüz veri transfer nesneleri (DTO / ViewModel)
├── Views/
│   ├── Account/                  # Giriş ve kayıt ekranları (Nostaljik Windows XP arayüzü)
│   ├── Admin/                    # Yönetici yönetim ekranları
│   ├── BudgetGoal/               # Bütçe hedefi izleme ve oluşturma arayüzleri
│   ├── Dashboard/                # Grafik ve veri özet paneli
│   ├── Report/                   # Excel çıktı ve raporlama paneli
│   ├── Shared/                   # Ortak layout ve navigasyon bileşenleri
│   ├── _ViewImports.cshtml       # Ortak kütüphane ve namespace tanımları
│   └── _ViewStart.cshtml         # Varsayılan layout tetikleyicisi
├── wwwroot/
│   ├── css/
│   │   └── xp.css                # Windows XP Luna görsel tema stil dosyası
│   └── js/
│   │   └── xp.js                 # AJAX bakiye güncelleyici ve interaktif arayüz scriptleri
├── appsettings.json              # Veritabanı bağlantı string tanımları
├── Program.cs                    # Bağımlılık enjeksiyonları ve middleware yapılandırması
└── KurusMatik.csproj             # Proje bağımlılıkları ve NuGet paket tanımları

```

---

## 2. Veritabanı ve Mimari Özellikler

### Veri Modeli ve Kısıtlamalar

* **Negatif Değer Engelleme:** `Transaction` modeli üzerinde harcama ve gelir miktarlarının negatif girilmesi veri öznitelikleri (`[Range(0.01, double.MaxValue)]`) ile backend seviyesinde engellenmiştir.
* **İlişkisel Bütünlük:** Kullanıcı silindiğinde ilişkili finansal hareketler otomatik olarak temizlenir (`Cascade Delete`). Kategorilerin sistemden silinmesi ise geçmiş verilerin korunması amacıyla sınırlandırılmıştır (`Restrict`).
* **Kimlik Yönetimi:** Rol tabanlı yetkilendirme altyapısı kurulmuştur. `Admin` rolü sistem genelindeki havuz kategorilerini yönetirken, `User` rolündeki kullanıcılar yalnızca kendi oluşturdukları finansal hareketleri görebilir.

### Analitik Veri Motoru (LINQ)

`DashboardController` bileşeni, arayüzdeki grafiklerin beslenmesi ve finansal durumun özetlenmesi için şu asenkron LINQ sorgularını yürütür:

* Kullanıcıya ait toplam gelir, toplam gider ve net bakiye hesaplamaları.
* `.GroupBy()` ve `.Sum()` operasyonları kullanılarak yapılan kategori bazlı aylık harcama dağılımları.
* Aktif bütçe hedefleri ile mevcut harcamaların karşılaştırmalı analizi ve limit aşım kontrolleri.

---

## 3. Kurulum ve Dağıtım Adımları

### Ön Gereksinimler

* .NET 8 veya .NET 9 SDK
* SQL Server (LocalDB mimarisi)

### Adım 1: Bağımlılıkların Çözümlenmesi

Proje kök dizininde terminali açarak gerekli NuGet paketlerini yerel ortama yükleyin:

```bash
dotnet restore

```

### Adım 2: Veritabanı Şemasının Oluşturulması

Entity Framework Core Code-First yapısını yerel veritabanı sunucusuna uygulamak ve varsayılan sistem kategorileri ile başlangıç rollerini oluşturmak için veritabanını güncelleyin:

```bash
dotnet ef database update

```

### Adım 3: Uygulamanın Başlatılması

Yerel web sunucusunu (Kestrel) aktif hale getirmek için projeyi çalıştırın:

```bash
dotnet run

```

Uygulama çalıştıktan sonra tarayıcı üzerinden terminalde belirtilen adrese (varsayılan olarak `http://localhost:5000` veya `https://localhost:5001`) erişim sağlayın.

### Kimlik Doğrulama Bilgileri

Sisteme yönetici yetkileriyle giriş yapmak ve tüm özellikleri test etmek için aşağıdaki seed verilerini kullanabilirsiniz:

* **Kullanıcı Adı:** `admin@KurusMatik.com`
* **Şifre:** `Admin123!`

---

## 4. Frontend ve Entegrasyon Özellikleri

* **Dinamik Veri Akışı (AJAX):** Sayfa yenileme ihtiyacını ortadan kaldıran Fetch API mimarisi uygulanmıştır. Harcama eklendiğinde veya silindiğinde, genel bakiye verisi `/Transaction/GetCurrentBalance` endpoint'i üzerinden asenkron olarak çekilir ve arayüzdeki görev çubuğuna yansıtılır.
* **Raporlama Entegrasyonu:** `ReportController` ve `ExcelExportService` entegrasyonu sayesinde, seçilen aya ait veriler EPPlus kütüphanesi yardımıyla dinamik olarak renk kodlu Excel (.xlsx) dosyasına dönüştürülerek indirilebilir.
* **Görsel Katman:** Arayüz bileşenleri tamamen Windows XP mimarisinin görsel standartlarına (Luna teması, degradeli başlık çubukları, kabartmalı buton tasarımları ve özel görev çubuğu) sadık kalınarak saf CSS ile inşa edilmiştir. Veri görselleştirmeleri için `Chart.js` kütüphanesi entegre edilmiştir.