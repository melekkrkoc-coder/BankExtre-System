using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using BankaExtreAnaliz.Models;
using ExcelDataReader;
using UglyToad.PdfPig;

namespace BankaExtreAnaliz.Services
{
    public class StatementParserService
    {
        private readonly AppDbContext _context;

        public StatementParserService(AppDbContext context)
        {
            _context = context;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        // --- ÖNİZLEME İÇİN DOSYA AYRIŞTIRICI (Veritabanına kaydetmez) ---
        public async Task<List<Transaction>> ParseFileForPreviewAsync(Stream fileStream, string fileName, int accountId)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            var rules = await _context.CategoryRules.ToListAsync();

            if (ext == ".csv")
                return await ParseCsvInternalAsync(fileStream, accountId, rules);
            if (ext == ".xlsx" || ext == ".xls")
                return ParseExcelInternal(fileStream, accountId, rules);
            if (ext == ".pdf")
                return ParsePdfInternal(fileStream, accountId, rules);

            return new List<Transaction>();
        }

        // --- 1. CSV İÇSEL AYRIŞTIRMA ---
        private async Task<List<Transaction>> ParseCsvInternalAsync(Stream fileStream, int accountId, List<CategoryRule> rules)
        {
            var transactions = new List<Transaction>();
            if (fileStream.CanSeek) fileStream.Position = 0;

            using var reader = new StreamReader(fileStream, System.Text.Encoding.Default);
            var content = await reader.ReadToEndAsync();
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length <= 1) return transactions;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                char separator = line.Contains(';') ? ';' : ',';
                var parts = line.Split(separator);
                if (parts.Length < 3) continue;

                DateTime date = ParseDate(parts[0].Trim());
                string rawDesc = parts[1].Trim();
                decimal amount;
                bool isExpense;

                if (TryExtractSpecialTransaction(rawDesc, out decimal specialAmount, out bool specialIsExpense, out string cleanDescSpecial))
                {
                    amount = specialAmount;
                    isExpense = specialIsExpense;
                    rawDesc = cleanDescSpecial;
                }
                else
                {
                    if (!TryParseAmount(parts[2].Trim(), out amount)) continue;
                    isExpense = amount < 0;
                    amount = Math.Abs(amount);

                    if (parts.Length > 3)
                    {
                        string typePart = parts[3].Trim().ToLower(new CultureInfo("tr-TR"));
                        if (typePart.Contains("gelir") || typePart.Contains("alacak"))
                        {
                            isExpense = false;
                        }
                    }
                }

                string cleanedDesc = CleanDescription(rawDesc);
                transactions.Add(new Transaction
                {
                    AccountId = accountId,
                    TransactionDate = date,
                    Description = cleanedDesc,
                    Amount = amount,
                    IsExpense = isExpense,
                    CategoryId = MatchCategory(cleanedDesc, rules)
                });
            }

            return transactions;
        }

        // --- 2. EXCEL İÇSEL AYRIŞTIRMA ---
        private List<Transaction> ParseExcelInternal(Stream fileStream, int accountId, List<CategoryRule> rules)
        {
            var transactions = new List<Transaction>();
            if (fileStream.CanSeek) fileStream.Position = 0;

            using var reader = ExcelReaderFactory.CreateReader(fileStream);
            bool isHeader = true;

            while (reader.Read())
            {
                if (isHeader)
                {
                    isHeader = false;
                    continue;
                }

                if (reader.FieldCount < 3) continue;

                var rawDate = reader.GetValue(0)?.ToString();
                var rawDesc = reader.GetValue(1)?.ToString() ?? "İşlem";
                var rawAmount = reader.GetValue(2)?.ToString();

                if (string.IsNullOrWhiteSpace(rawDate)) continue;

                DateTime date = ParseDate(rawDate);
                decimal amount;
                bool isExpense;

                if (TryExtractSpecialTransaction(rawDesc, out decimal specialAmount, out bool specialIsExpense, out string cleanDescSpecial))
                {
                    amount = specialAmount;
                    isExpense = specialIsExpense;
                    rawDesc = cleanDescSpecial;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(rawAmount) || !TryParseAmount(rawAmount, out amount)) continue;
                    isExpense = amount < 0;
                    amount = Math.Abs(amount);

                    if (reader.FieldCount > 3)
                    {
                        string rawType = reader.GetValue(3)?.ToString()?.ToLower(new CultureInfo("tr-TR")) ?? "";
                        if (rawType.Contains("gelir") || rawType.Contains("alacak"))
                        {
                            isExpense = false;
                        }
                    }
                }

                string cleanedDesc = CleanDescription(rawDesc);
                transactions.Add(new Transaction
                {
                    AccountId = accountId,
                    TransactionDate = date,
                    Description = cleanedDesc,
                    Amount = amount,
                    IsExpense = isExpense,
                    CategoryId = MatchCategory(cleanedDesc, rules)
                });
            }

            return transactions;
        }

        // --- 3. PDF İÇSEL AYRIŞTIRMA ---
        private List<Transaction> ParsePdfInternal(Stream fileStream, int accountId, List<CategoryRule> rules)
        {
            var transactions = new List<Transaction>();
            if (fileStream.CanSeek) fileStream.Position = 0;

            using var document = PdfDocument.Open(fileStream);

            foreach (var page in document.GetPages())
            {
                string text = page.Text;

                if (text.Contains("FAST TUTARI") || text.Contains("EFT") || text.Contains("HAVALE"))
                {
                    var dateMatch = Regex.Match(text, @"(?:BELGE TARİHİ|TARİH)\s*[:]\s*(\d{2}[\./\-]\d{2}[\./\-]\d{4})", RegexOptions.IgnoreCase);
                    DateTime date = dateMatch.Success ? ParseDate(dateMatch.Groups[1].Value) : DateTime.Now;

                    if (TryExtractSpecialTransaction(text, out decimal amount, out bool isExpense, out string cleanDesc))
                    {
                        transactions.Add(new Transaction
                        {
                            AccountId = accountId,
                            TransactionDate = date,
                            Description = cleanDesc,
                            Amount = amount,
                            IsExpense = isExpense,
                            CategoryId = MatchCategory(cleanDesc, rules)
                        });
                        continue;
                    }
                }

                var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var rowRegex = new Regex(@"^(\d{2}[\./\-]\d{2}[\./\-]\d{4})\s+(.+?)\s+(-?[\d\.,]+(?:\s*TL|\s*₺)?)$");

                foreach (var line in lines)
                {
                    var m = rowRegex.Match(line.Trim());
                    if (!m.Success) continue;

                    DateTime date = ParseDate(m.Groups[1].Value.Trim());
                    string desc = m.Groups[2].Value.Trim();
                    if (!TryParseAmount(m.Groups[3].Value.Trim(), out decimal amount)) continue;

                    bool isExpense = amount < 0;
                    amount = Math.Abs(amount);

                    transactions.Add(new Transaction
                    {
                        AccountId = accountId,
                        TransactionDate = date,
                        Description = CleanDescription(desc),
                        Amount = amount,
                        IsExpense = isExpense,
                        CategoryId = MatchCategory(desc, rules)
                    });
                }
            }

            return transactions;
        }

        // --- ESKİ DOĞRUDAN KAYIT METOTLARI (Geriye uyumluluk için) ---
        public async Task<int> ParseAndSaveCsvAsync(Stream fileStream, int accountId)
        {
            var rules = await _context.CategoryRules.ToListAsync();
            var transactions = await ParseCsvInternalAsync(fileStream, accountId, rules);
            return await SaveTransactionsAsync(transactions);
        }

        public async Task<int> ParseAndSaveExcelAsync(Stream fileStream, int accountId)
        {
            var rules = await _context.CategoryRules.ToListAsync();
            var transactions = ParseExcelInternal(fileStream, accountId, rules);
            return await SaveTransactionsAsync(transactions);
        }

        public async Task<int> ParseAndSavePdfAsync(Stream fileStream, int accountId)
        {
            var rules = await _context.CategoryRules.ToListAsync();
            var transactions = ParsePdfInternal(fileStream, accountId, rules);
            return await SaveTransactionsAsync(transactions);
        }

        // --- DENİZBANK VE ÖZEL DEKONT DESENLERİ ---
        private bool TryExtractSpecialTransaction(string text, out decimal amount, out bool isExpense, out string cleanDesc)
        {
            amount = 0;
            isExpense = true;
            cleanDesc = "Para Transferi";

            var fastMatch = Regex.Match(text, @"(?:GİDEN|GELEN)?\s*(?:FAST|EFT|HAVALE|İŞLEM)\s*TUTARI\s*[:]\s*(-?[\d\.,]+)", RegexOptions.IgnoreCase);
            if (fastMatch.Success)
            {
                if (TryParseAmount(fastMatch.Groups[1].Value, out amount))
                {
                    isExpense = text.Contains("GİDEN", StringComparison.OrdinalIgnoreCase) || amount < 0;
                    amount = Math.Abs(amount);

                    var receiverMatch = Regex.Match(text, @"(?:ALICI|GÖNDEREN)\s*(?:HESAP NO|ADI SOYADI|AD SOYAD)?\s*[:]\s*([^\s:/]+(?:\s+[^\s:/]+)?)", RegexOptions.IgnoreCase);
                    if (receiverMatch.Success)
                    {
                        cleanDesc = $"FAST Transferi - {receiverMatch.Groups[1].Value.Trim()}";
                    }
                    else
                    {
                        cleanDesc = isExpense ? "Giden FAST Transferi" : "Gelen FAST Transferi";
                    }
                    return true;
                }
            }

            return false;
        }

        private DateTime ParseDate(string dateStr)
        {
            if (DateTime.TryParse(dateStr, new CultureInfo("tr-TR"), DateTimeStyles.None, out DateTime date))
                return date;
            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return date;
            return DateTime.Now;
        }

        private bool TryParseAmount(string amountStr, out decimal amount)
        {
            amount = 0;
            if (string.IsNullOrWhiteSpace(amountStr)) return false;

            amountStr = amountStr.Replace("TL", "", StringComparison.OrdinalIgnoreCase)
                                 .Replace("₺", "")
                                 .Trim();

            if (amountStr.Contains(','))
            {
                amountStr = amountStr.Replace(".", "").Replace(',', '.');
            }

            return decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
        }

        private int? MatchCategory(string desc, List<CategoryRule> rules)
        {
            string upperDesc = desc.ToUpper(new CultureInfo("tr-TR"));
            foreach (var rule in rules)
            {
                if (upperDesc.Contains(rule.Keyword.ToUpper(new CultureInfo("tr-TR"))))
                {
                    return rule.CategoryId;
                }
            }
            return null;
        }

        private string CleanDescription(string desc)
        {
            desc = Regex.Replace(desc, @"(FIS\s*NO:\s*\d+)|(REF:\s*\d+)|(TERM:\s*\w+)", "", RegexOptions.IgnoreCase);
            desc = Regex.Replace(desc, @"\s+", " ").Trim();
            return string.IsNullOrEmpty(desc) ? "İşlem" : desc;
        }

        private async Task<int> SaveTransactionsAsync(List<Transaction> transactions)
        {
            if (transactions.Any())
            {
                _context.Transactions.AddRange(transactions);
                await _context.SaveChangesAsync();
            }
            return transactions.Count;
        }
    }
}