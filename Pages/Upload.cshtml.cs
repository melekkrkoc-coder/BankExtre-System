using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BankaExtreAnaliz.Models;
using BankaExtreAnaliz.Services;
using System.Text.Json;

namespace BankaExtreAnaliz.Pages
{
    public class UploadModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly StatementParserService _parserService;

        public UploadModel(AppDbContext context, StatementParserService parserService)
        {
            _context = context;
            _parserService = parserService;
        }

        public List<Account> Accounts { get; set; } = new();

        [BindProperty]
        public int SelectedAccountId { get; set; }

        [BindProperty]
        public IFormFile? StatementFile { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // Önizleme satırları
        public List<Transaction> PreviewTransactions { get; set; } = new();

        // Önizleme verisini taşıyan JSON string
        [BindProperty]
        public string? SerializedPreviewData { get; set; }

        public async Task OnGetAsync()
        {
            Accounts = await _context.Accounts.ToListAsync();
        }

        // 1. ADIM: Dosyayı İncele (Önizleme Al)
        public async Task<IActionResult> OnPostPreviewAsync()
        {
            Accounts = await _context.Accounts.ToListAsync();

            if (SelectedAccountId == 0 || StatementFile == null || StatementFile.Length == 0)
            {
                ErrorMessage = "Lütfen bir hesap seçin ve geçerli bir ekstre dosyası yükleyin.";
                return Page();
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await StatementFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                PreviewTransactions = await _parserService.ParseFileForPreviewAsync(memoryStream, StatementFile.FileName, SelectedAccountId);

                if (PreviewTransactions.Any())
                {
                    SerializedPreviewData = JsonSerializer.Serialize(PreviewTransactions);
                }
                else
                {
                    ErrorMessage = "Dosya okundu fakat işlem bulunamadı veya format tanınamadı.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Hata oluştu: " + ex.Message;
            }

            return Page();
        }

        // 2. ADIM: Önizlenen Verileri Onayla ve Veritabanına Yaz
        public async Task<IActionResult> OnPostSaveAsync()
        {
            Accounts = await _context.Accounts.ToListAsync();

            if (string.IsNullOrWhiteSpace(SerializedPreviewData))
            {
                ErrorMessage = "Kaydedilecek önizleme verisi bulunamadı.";
                return Page();
            }

            try
            {
                var transactions = JsonSerializer.Deserialize<List<Transaction>>(SerializedPreviewData);
                if (transactions != null && transactions.Any())
                {
                    _context.Transactions.AddRange(transactions);
                    await _context.SaveChangesAsync();

                    // Sadece istenen net onay metni:
                    SuccessMessage = "Başarıyla yüklendi";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Kaydedilirken hata oluştu: " + ex.Message;
            }

            return Page();
        }
    }
}