using TheAccountant.Web.Models;

namespace TheAccountant.Web.Models.ViewModels
{
    public class DashboardViewModel
    {
        public decimal NetWorth { get; set; }

        public decimal CashBalance { get; set; }

        public decimal CreditDebt { get; set; }

        public decimal MonthlySpending { get; set; }

        public List<Account> Accounts { get; set; } = new();
    }
}