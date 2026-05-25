using OfficeOpenXml;
using OfficeOpenXml.Style;
using KurusMatik.Models;
using System.Drawing;

namespace KurusMatik.Services
{
    // Excel dışa aktarma servisim. EPPlus kütüphanesini kullandım.
    // Kullanıcı kendi harcamalarını indirip inceleyebilsin diye yazdım.
    public class ExcelExportService
    {
        public ExcelExportService()
        {
            // EPPlus'ın ücretsiz lisansını kullanıyorum, bunu belirtmek gerekiyor
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // İşlemleri Excel dosyasına dönüştürüp byte dizisi olarak döndürüyorum
        public byte[] ExportTransactionsToExcel(
            List<Transaction> transactions,
            string userName,
            int month,
            int year)
        {
            using var package = new ExcelPackage();

            // Worksheet oluşturuyorum
            var worksheet = package.Workbook.Worksheets.Add("Harcama Raporu");

            // --- BAŞLIK SATIRI ---
            // Türkçe ay adları için bir dizi hazırladım
            var turkishMonths = new[] { "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
                                         "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

            // Başlık hücresi - birleştirip büyük yazıyorum
            worksheet.Cells["A1:F1"].Merge = true;
            worksheet.Cells["A1"].Value = $"KurusMatik - {turkishMonths[month]} {year} Harcama Raporu";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Kullanıcı adı satırı
            worksheet.Cells["A2:F2"].Merge = true;
            worksheet.Cells["A2"].Value = $"Kullanıcı: {userName}";
            worksheet.Cells["A2"].Style.Font.Italic = true;
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Oluşturulma tarihi
            worksheet.Cells["A3:F3"].Merge = true;
            worksheet.Cells["A3"].Value = $"Oluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}";
            worksheet.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // --- SÜTUN BAŞLIKLARI (5. satırdan başlıyorum) ---
            var headerRow = 5;
            var headers = new[] { "Tarih", "Kategori", "Tür", "Açıklama", "Tutar (₺)", "Durum" };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[headerRow, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                // Yeşilimsi bir başlık rengi
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(39, 174, 96));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // --- VERİ SATIRLARI ---
            int row = headerRow + 1;

            foreach (var transaction in transactions.OrderByDescending(t => t.Date))
            {
                // Her satırı dolduruyorum
                worksheet.Cells[row, 1].Value = transaction.Date.ToString("dd.MM.yyyy");
                worksheet.Cells[row, 2].Value = transaction.Category?.Name ?? "-";
                worksheet.Cells[row, 3].Value = transaction.Category?.Type == CategoryType.Gelir ? "Gelir" : "Gider";
                worksheet.Cells[row, 4].Value = transaction.Description ?? "-";
                worksheet.Cells[row, 5].Value = transaction.Amount;
                worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00 ₺";

                // Gelir/Gider satırlarına farklı renk veriyorum - daha okunaklı görünsün
                var rowColor = transaction.Category?.Type == CategoryType.Gelir
                    ? Color.FromArgb(209, 250, 229) // Açık yeşil - gelir
                    : Color.FromArgb(254, 226, 226); // Açık kırmızı - gider

                for (int col = 1; col <= 6; col++)
                {
                    worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(rowColor);
                    worksheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Hair, Color.Gray);
                }

                // Durum sütununa renkli metin yazmak istiyorum
                worksheet.Cells[row, 6].Value = transaction.Category?.Type == CategoryType.Gelir ? "✓ Gelir" : "✗ Gider";
                worksheet.Cells[row, 6].Style.Font.Bold = true;

                row++;
            }

            // --- ÖZET SATIRI ---
            // En alta toplam gelir, gider ve bakiye yazıyorum
            row += 1; // Bir satır boşluk

            var totalIncome = transactions
                .Where(t => t.Category?.Type == CategoryType.Gelir)
                .Sum(t => t.Amount);

            var totalExpense = transactions
                .Where(t => t.Category?.Type == CategoryType.Gider)
                .Sum(t => t.Amount);

            // Özet başlık
            worksheet.Cells[row, 1, row, 3].Merge = true;
            worksheet.Cells[row, 1].Value = "ÖZET";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 12;
            row++;

            // Toplam Gelir
            worksheet.Cells[row, 4].Value = "Toplam Gelir:";
            worksheet.Cells[row, 4].Style.Font.Bold = true;
            worksheet.Cells[row, 5].Value = totalIncome;
            worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00 ₺";
            worksheet.Cells[row, 5].Style.Font.Color.SetColor(Color.FromArgb(39, 174, 96));
            row++;

            // Toplam Gider
            worksheet.Cells[row, 4].Value = "Toplam Gider:";
            worksheet.Cells[row, 4].Style.Font.Bold = true;
            worksheet.Cells[row, 5].Value = totalExpense;
            worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00 ₺";
            worksheet.Cells[row, 5].Style.Font.Color.SetColor(Color.FromArgb(192, 57, 43));
            row++;

            // Net Bakiye
            worksheet.Cells[row, 4].Value = "Net Bakiye:";
            worksheet.Cells[row, 4].Style.Font.Bold = true;
            worksheet.Cells[row, 5].Value = totalIncome - totalExpense;
            worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00 ₺";
            worksheet.Cells[row, 5].Style.Font.Bold = true;

            // --- SÜTUN GENİŞLİKLERİ ---
            worksheet.Column(1).Width = 15;  // Tarih
            worksheet.Column(2).Width = 25;  // Kategori
            worksheet.Column(3).Width = 10;  // Tür
            worksheet.Column(4).Width = 40;  // Açıklama - bu uzun olabilir
            worksheet.Column(5).Width = 18;  // Tutar
            worksheet.Column(6).Width = 12;  // Durum

            // Tüm hücrelerde orta hizalama
            worksheet.Cells[worksheet.Dimension.Address].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Dosyayı byte array olarak döndürüyorum
            return package.GetAsByteArray();
        }
    }
}
