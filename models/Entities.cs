using System.ComponentModel.DataAnnotations;

namespace BankaExtreAnaliz.Models
{
    // 1. Banka / Hesap Tablosu
    public class Account
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Hesap adı zorunludur.")]
        [Display(Name = "Hesap Adı")]
        public string AccountName { get; set; } = string.Empty;

        [Display(Name = "Banka Adı")]
        public string BankName { get; set; } = string.Empty;

        public List<Transaction> Transactions { get; set; } = new();
    }

    // 2. Kategori Tablosu
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [Display(Name = "Kategori Adı")]
        public string Name { get; set; } = string.Empty;

        public List<CategoryRule> Rules { get; set; } = new();
        public List<Transaction> Transactions { get; set; } = new();
    }

    // 3. Otomatik Kategori Eşleme Kuralı
    public class CategoryRule
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Anahtar Kelime")]
        public string Keyword { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }

    // 4. Hesap Hareketleri (İşlemler)
    public class Transaction
    {
        public int Id { get; set; }

        [Display(Name = "İşlem Tarihi")]
        public DateTime TransactionDate { get; set; }

        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Tutar")]
        public decimal Amount { get; set; }

        [Display(Name = "Harcama mı?")]
        public bool IsExpense { get; set; } // true ise Harcama, false ise Gelir

        public int AccountId { get; set; }
        public Account? Account { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}