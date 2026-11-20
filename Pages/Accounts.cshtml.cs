using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BankaExtreAnaliz.Models;

namespace BankaExtreAnaliz.Pages
{
    public class AccountsModel : PageModel
    {
        private readonly AppDbContext _context;

        public AccountsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Account> Accounts { get; set; } = new();

        [BindProperty]
        public string BankName { get; set; } = string.Empty;

        [BindProperty]
        public string AccountName { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            Accounts = await _context.Accounts.OrderBy(a => a.BankName).ToListAsync();
        }

        public async Task<IActionResult> OnPostAddAccountAsync()
        {
            if (!string.IsNullOrWhiteSpace(BankName) && !string.IsNullOrWhiteSpace(AccountName))
            {
                var newAccount = new Account
                {
                    BankName = BankName.Trim(),
                    AccountName = AccountName.Trim()
                };

                _context.Accounts.Add(newAccount);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAccountAsync(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account != null)
            {
                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}