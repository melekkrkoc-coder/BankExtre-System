namespace BankaExtreAnaliz.models.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalIncome { get; set; }   // Toplam Gelir
        public decimal TotalExpense { get; set; }  // Toplam Gider
        public decimal NetBalance => TotalIncome - TotalExpense; // Net Kalan

        public List<CategoryExpenseDto> CategoryExpenses { get; set; } = new();
        public List<TopExpensePlaceDto> TopPlaces { get; set; } = new();

        // Eklenen özellik:
        public List<RecentTransactionItem> RecentTransactions { get; set; } = new();

        public int? SelectedAccountId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CategoryExpenseDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public class TopExpensePlaceDto
    {
        public string PlaceName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public class RecentTransactionItem
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsExpense { get; set; }
    }
}