using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BankaExtreAnaliz.Models;
using BankaExtreAnaliz.models.ViewModels;

namespace BankaExtreAnaliz.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public DashboardViewModel DashboardData { get; set; } = new();

        public List<SelectListItem> AccountList { get; set; } = new();

        public async Task OnGetAsync(int? accountId, DateTime? startDate, DateTime? endDate)
        {
            var accounts = await _context.Accounts.ToListAsync();
            AccountList = accounts.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.BankName} - {a.AccountName}",
                Selected = accountId.HasValue && accountId.Value == a.Id
            }).ToList();

            var query = _context.Transactions.Include(t => t.Category).AsQueryable();

            if (accountId.HasValue)
            {
                query = query.Where(t => t.AccountId == accountId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate <= endDate.Value);
            }

            var totalIncome = await query
                .Where(t => !t.IsExpense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var totalExpense = await query
                .Where(t => t.IsExpense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var categoryExpenses = await query
                .Where(t => t.IsExpense)
                .GroupBy(t => t.Category != null ? t.Category.Name : "Kategorisiz")
                .Select(g => new CategoryExpenseDto
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync();

            var topPlaces = await query
                .Where(t => t.IsExpense)
                .GroupBy(t => t.Description)
                .Select(g => new TopExpensePlaceDto
                {
                    PlaceName = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .Take(5)
                .ToListAsync();

            // Son 5 işlemi çekme sorgusu
            var recentTransactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Take(5)
                .Select(t => new RecentTransactionItem
                {
                    Date = t.TransactionDate,
                    Description = t.Description,
                    CategoryName = t.Category != null ? t.Category.Name : "Kategorisiz",
                    Amount = t.Amount,
                    IsExpense = t.IsExpense
                })
                .ToListAsync();

            DashboardData = new DashboardViewModel
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                CategoryExpenses = categoryExpenses,
                TopPlaces = topPlaces,
                RecentTransactions = recentTransactions,
                SelectedAccountId = accountId,
                StartDate = startDate,
                EndDate = endDate
            };
        }

        public async Task<JsonResult> OnGetCategoryDetailsAsync(string categoryName, int? accountId, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Where(t => t.IsExpense);

            if (categoryName == "Kategorisiz")
            {
                query = query.Where(t => t.Category == null);
            }
            else
            {
                query = query.Where(t => t.Category != null && t.Category.Name == categoryName);
            }

            if (accountId.HasValue)
            {
                query = query.Where(t => t.AccountId == accountId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate <= endDate.Value);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new
                {
                    date = t.TransactionDate.ToString("dd.MM.yyyy"),
                    account = t.Account != null ? t.Account.AccountName : "-",
                    description = t.Description,
                    amount = t.Amount.ToString("C2")
                })
                .ToListAsync();

            return new JsonResult(transactions);
        }

        public async Task<IActionResult> OnGetExportExpensesAsync(int? accountId, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Where(t => t.IsExpense);

            if (accountId.HasValue)
            {
                query = query.Where(t => t.AccountId == accountId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate <= endDate.Value);
            }

            var expenses = await query
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("Tarih;Banka/Hesap;Kategori;Açıklama;Tutar");

            foreach (var item in expenses)
            {
                var dateStr = item.TransactionDate.ToString("dd.MM.yyyy");
                var accountStr = item.Account != null ? item.Account.AccountName : "-";
                var categoryStr = item.Category != null ? item.Category.Name : "Kategorisiz";
                var descStr = item.Description.Replace(";", " ");
                var amountStr = item.Amount.ToString("F2");

                builder.AppendLine($"{dateStr};{accountStr};{categoryStr};{descStr};{amountStr}");
            }

            var encoding = System.Text.Encoding.UTF8;
            var preamble = encoding.GetPreamble();
            var bytes = encoding.GetBytes(builder.ToString());
            var fileBytes = preamble.Concat(bytes).ToArray();

            var fileName = $"Gider_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(fileBytes, "text/csv; charset=utf-8", fileName);
        }
    }
}