using TheAccountant.Models;

namespace TheAccountant.Web.Models.ViewModels;

public class DashboardViewModel
{
    public List<Account> Accounts { get; set; }
    public List<Transaction> Transactions { get; set; }
    public List<RecurringPayment> RecurringPayments { get; set; }
}
