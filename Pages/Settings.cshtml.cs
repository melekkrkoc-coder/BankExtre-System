using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BankaExtreAnaliz.Models;

namespace BankaExtreAnaliz.Pages
{
    public class SettingsModel : PageModel
    {
        private readonly AppDbContext _context;

        public SettingsModel(AppDbContext context)
        {
            _context = context;
        }

        public int TotalTransactionsCount { get; set; }
        public int TotalAccountsCount { get; set; }
        public int TotalCategoriesCount { get; set; }
        public int TotalRulesCount { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadCountsAsync();
        }

        private async Task LoadCountsAsync()
        {
            TotalTransactionsCount = await _context.Transactions.CountAsync();
            TotalAccountsCount = await _context.Accounts.CountAsync();
            TotalCategoriesCount = await _context.Categories.CountAsync();
            TotalRulesCount = await _context.CategoryRules.CountAsync();
        }

        public async Task<IActionResult> OnPostClearTransactionsAsync()
        {
            try
            {
                _context.Transactions.RemoveRange(_context.Transactions);
                await _context.SaveChangesAsync();
                SuccessMessage = "Tüm hesap hareketleri başarıyla temizlendi.";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Hata: " + ex.Message;
            }

            await LoadCountsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostClearRulesAsync()
        {
            try
            {
                _context.CategoryRules.RemoveRange(_context.CategoryRules);
                await _context.SaveChangesAsync();
                SuccessMessage = "Kategori eşleme kuralları sıfırlandı.";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Hata: " + ex.Message;
            }

            await LoadCountsAsync();
            return Page();
        }
    }
}