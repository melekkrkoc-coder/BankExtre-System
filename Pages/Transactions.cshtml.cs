using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BankaExtreAnaliz.Models;

namespace BankaExtreAnaliz.Pages
{
    public class TransactionsModel : PageModel
    {
        private readonly AppDbContext _context;

        public TransactionsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Transaction> Transactions { get; set; } = new();
        public List<Account> Accounts { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedAccountId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedCategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            Accounts = await _context.Accounts.ToListAsync();
            Categories = await _context.Categories.ToListAsync();

            var query = _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .AsQueryable();

            if (SelectedAccountId.HasValue && SelectedAccountId > 0)
            {
                query = query.Where(t => t.AccountId == SelectedAccountId.Value);
            }

            if (SelectedCategoryId.HasValue && SelectedCategoryId > 0)
            {
                query = query.Where(t => t.CategoryId == SelectedCategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(t => t.Description.ToLower().Contains(term));
            }

            Transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .ToListAsync();
        }

        // Kategori ve Kural Güncelleme AJAX Metodu
        public async Task<IActionResult> OnPostUpdateCategoryAsync(int transactionId, int? categoryId, bool createRule, string? customKeyword)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null)
            {
                return NotFound();
            }

            transaction.CategoryId = categoryId;

            if (createRule && categoryId.HasValue)
            {
                string keyword = !string.IsNullOrWhiteSpace(customKeyword)
                    ? customKeyword.Trim()
                    : transaction.Description.Trim().Split(' ')[0];

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    bool exists = await _context.CategoryRules.AnyAsync(r => r.Keyword.ToLower() == keyword.ToLower());
                    if (!exists)
                    {
                        _context.CategoryRules.Add(new CategoryRule
                        {
                            Keyword = keyword,
                            CategoryId = categoryId.Value
                        });

                        // Benzer geçmiş kayıtları da güncelle
                        var similarTransactions = await _context.Transactions
                            .Where(t => t.CategoryId == null && t.Description.ToLower().Contains(keyword.ToLower()))
                            .ToListAsync();

                        foreach (var st in similarTransactions)
                        {
                            st.CategoryId = categoryId.Value;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        // Tekil İşlem Silme Metodu
        public async Task<IActionResult> OnPostDeleteTransactionAsync(int id)
        {
            var item = await _context.Transactions.FindAsync(id);
            if (item != null)
            {
                _context.Transactions.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new
            {
                accountId = SelectedAccountId,
                categoryId = SelectedCategoryId,
                searchTerm = SearchTerm
            });
        }
    }
}