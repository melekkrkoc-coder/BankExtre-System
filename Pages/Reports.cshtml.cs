using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BankaExtreAnaliz.Models;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BankaExtreAnaliz.Pages
{
    public class ReportsModel : PageModel
    {
        private readonly AppDbContext _context;

        public ReportsModel(AppDbContext context)
        {
            _context = context;
        }

        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance => TotalIncome - TotalExpense;

        public List<CategoryDistributionItem> CategoryExpenses { get; set; } = new();
        public List<TopPlaceReportItem> TopPlaces { get; set; } = new();
        public List<BankDistributionItem> BankDistribution { get; set; } = new();
        public List<MonthlyTrendItem> MonthlyTrends { get; set; } = new();

        public class CategoryDistributionItem
        {
            public string CategoryName { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public double Percentage { get; set; }
        }

        public class TopPlaceReportItem
        {
            public string PlaceName { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
        }

        public class BankDistributionItem
        {
            public string BankName { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public double Percentage { get; set; }
        }

        public class MonthlyTrendItem
        {
            public string MonthName { get; set; } = string.Empty;
            public decimal Income { get; set; }
            public decimal Expense { get; set; }
        }

        public async Task OnGetAsync()
        {
            var query = _context.Transactions.Include(t => t.Category).Include(t => t.Account).AsQueryable();

            TotalIncome = await query
                .Where(t => !t.IsExpense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            TotalExpense = await query
                .Where(t => t.IsExpense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var rawCategoryExpenses = await query
                .Where(t => t.IsExpense)
                .GroupBy(t => t.Category != null ? t.Category.Name : "Kategorisiz")
                .Select(g => new
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync();

            CategoryExpenses = rawCategoryExpenses.Select(x => new CategoryDistributionItem
            {
                CategoryName = x.CategoryName,
                TotalAmount = x.TotalAmount,
                Percentage = TotalExpense > 0 ? (double)(x.TotalAmount / TotalExpense) * 100 : 0
            }).ToList();

            TopPlaces = await query
                .Where(t => t.IsExpense)
                .GroupBy(t => t.Description)
                .Select(g => new TopPlaceReportItem
                {
                    PlaceName = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .Take(5)
                .ToListAsync();

            var rawBankDistribution = await query
                .Where(t => t.IsExpense)
                .GroupBy(t => t.Account != null ? t.Account.BankName : "Belirtilmemiş")
                .Select(g => new
                {
                    BankName = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync();

            BankDistribution = rawBankDistribution.Select(x => new BankDistributionItem
            {
                BankName = x.BankName,
                TotalAmount = x.TotalAmount,
                Percentage = TotalExpense > 0 ? (double)(x.TotalAmount / TotalExpense) * 100 : 0
            }).ToList();

            var allTransactions = await query.ToListAsync();
            var months = allTransactions
                .Select(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                .Distinct()
                .OrderBy(d => d.Year).ThenBy(d => d.Month)
                .TakeLast(6)
                .ToList();

            var trCulture = new CultureInfo("tr-TR");

            if (!months.Any())
            {
                months.Add(new { DateTime.Now.Year, DateTime.Now.Month });
            }

            foreach (var m in months)
            {
                var monthTransactions = allTransactions
                    .Where(t => t.TransactionDate.Year == m.Year && t.TransactionDate.Month == m.Month)
                    .ToList();

                var inc = monthTransactions.Where(t => !t.IsExpense).Sum(t => t.Amount);
                var exp = monthTransactions.Where(t => t.IsExpense).Sum(t => t.Amount);

                DateTime dateObj = new DateTime(m.Year, m.Month, 1);
                MonthlyTrends.Add(new MonthlyTrendItem
                {
                    MonthName = dateObj.ToString("MMMM yyyy", trCulture),
                    Income = inc,
                    Expense = exp
                });
            }
        }

        // PDF ÇIKTISI OLUŞTURMA METODU
        public async Task<IActionResult> OnGetExportPdfAsync()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Account)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var totalIncome = transactions.Where(t => !t.IsExpense).Sum(t => t.Amount);
            var totalExpense = transactions.Where(t => t.IsExpense).Sum(t => t.Amount);
            var netBalance = totalIncome - totalExpense;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                    // Başlık Alanı
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("BankaAnaliz - Finansal Özet Raporu").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text($"Oluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });

                    // Gövde Alanı
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // 3'lü Özet Kartlar Tablosu
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                            {
                                c.Item().Text("Toplam Gelir").FontSize(8).FontColor(Colors.Grey.Medium);
                                c.Item().Text(totalIncome.ToString("C2")).FontSize(12).Bold().FontColor(Colors.Green.Medium);
                            });

                            table.Cell().Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                            {
                                c.Item().Text("Toplam Gider").FontSize(8).FontColor(Colors.Grey.Medium);
                                c.Item().Text(totalExpense.ToString("C2")).FontSize(12).Bold().FontColor(Colors.Red.Medium);
                            });

                            table.Cell().Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                            {
                                c.Item().Text("Net Bakiye").FontSize(8).FontColor(Colors.Grey.Medium);
                                c.Item().Text(netBalance.ToString("C2")).FontSize(12).Bold().FontColor(Colors.Blue.Medium);
                            });
                        });

                        col.Item().PaddingTop(15).Text("Hesap Hareketleri").FontSize(11).Bold();

                        // Tablo
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(65);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Tarih").Bold();
                                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Açıklama").Bold();
                                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Banka / Hesap").Bold();
                                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Kategori").Bold();
                                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Tutar").Bold();
                            });

                            foreach (var item in transactions.Take(100))
                            {
                                var amtColor = item.IsExpense ? Colors.Red.Medium : Colors.Green.Medium;
                                var sign = item.IsExpense ? "-" : "+";

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.TransactionDate.ToString("dd.MM.yyyy"));
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Description);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{item.Account?.BankName} - {item.Account?.AccountName}");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Category?.Name ?? "Kategorisiz");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{sign}{item.Amount:C2}").FontColor(amtColor);
                            }
                        });
                    });

                    // Sayfa Numarası
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("BankaAnaliz Sistemi - Sayfa ");
                        x.CurrentPageNumber();
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Finansal_Rapor_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}