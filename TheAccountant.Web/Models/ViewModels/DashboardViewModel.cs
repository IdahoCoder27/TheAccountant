using TheAccountant.Web.Models;

namespace TheAccountant.Web.Models.ViewModels
{
    public class DashboardViewModel
    {
        public decimal NetWorth { get; set; }

        public decimal CashBalance { get; set; }

        public decimal CreditDebt { get; set; }

        public decimal MonthlySpending { get; set; }

        public int ActiveAccountCount { get; set; }

        public List<Account> Accounts { get; set; } = new();

        public List<Transaction> RecentTransactions { get; set; } = new();

        public List<CategorySpendViewModel> SpendingByCategory { get; set; } = new();

        public List<MonthlyCashFlowViewModel> CashFlowHistory { get; set; }
    = new();
    }
}