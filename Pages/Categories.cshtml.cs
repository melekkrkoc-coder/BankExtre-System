using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BankaExtreAnaliz.Models;

namespace BankaExtreAnaliz.Pages
{
    public class CategoriesModel : PageModel
    {
        private readonly AppDbContext _context;

        public CategoriesModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Category> AllCategories { get; set; } = new();
        public List<Category> DefaultCategories { get; set; } = new();
        public List<Category> CustomCategories { get; set; } = new();
        public List<CategoryRule> Rules { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            AllCategories = await _context.Categories
                .Include(c => c.Transactions)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Varsayılan sistem kategorileri (veya IsDefault alanın varsa ona göre, yoksa ilk temel kategoriler)
            var defaultNames = new[] { "Market & Gıda", "Fatura & Abonelik", "Ulaşım & Akaryakıt", "Yeme - İçme", "Maaş / Gelir", "Diğer" };

            DefaultCategories = AllCategories
                .Where(c => defaultNames.Contains(c.Name))
                .ToList();

            CustomCategories = AllCategories
                .Where(c => !defaultNames.Contains(c.Name))
                .ToList();

            Rules = await _context.CategoryRules
                .Include(r => r.Category)
                .OrderBy(r => r.Keyword)
                .ToListAsync();
        }

        // Yeni Kategori Ekleme
        public async Task<IActionResult> OnPostCreateCategoryAsync(string categoryName)
        {
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var trimmed = categoryName.Trim();
                bool exists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == trimmed.ToLower());
                if (!exists)
                {
                    _context.Categories.Add(new Category { Name = trimmed });
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage();
        }

        // Kategori Silme
        public async Task<IActionResult> OnPostDeleteCategoryAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Transactions)
                .Include(c => c.Rules)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category != null)
            {
                // Bağlı işlemleri kategorisiz yap
                foreach (var t in category.Transactions)
                {
                    t.CategoryId = null;
                }

                // Bağlı kuralları sil
                _context.CategoryRules.RemoveRange(category.Rules);
                _context.Categories.Remove(category);

                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        // Yeni Kural Ekleme
        public async Task<IActionResult> OnPostCreateRuleAsync(string keyword, int categoryId)
        {
            if (!string.IsNullOrWhiteSpace(keyword) && categoryId > 0)
            {
                var trimmed = keyword.Trim();
                bool exists = await _context.CategoryRules.AnyAsync(r => r.Keyword.ToLower() == trimmed.ToLower());
                if (!exists)
                {
                    _context.CategoryRules.Add(new CategoryRule
                    {
                        Keyword = trimmed,
                        CategoryId = categoryId
                    });

                    // Geçmişteki benzer işlemleri de otomatik olarak bu kategoriye bağla
                    var similarTransactions = await _context.Transactions
                        .Where(t => t.CategoryId == null && t.Description.ToLower().Contains(trimmed.ToLower()))
                        .ToListAsync();

                    foreach (var t in similarTransactions)
                    {
                        t.CategoryId = categoryId;
                    }

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage();
        }

        // Kural Silme
        public async Task<IActionResult> OnPostDeleteRuleAsync(int id)
        {
            var rule = await _context.CategoryRules.FindAsync(id);
            if (rule != null)
            {
                _context.CategoryRules.Remove(rule);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}