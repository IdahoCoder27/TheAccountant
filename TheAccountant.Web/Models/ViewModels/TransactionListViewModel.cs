using TheAccountant.Web.Models.Enums;
using TheAccountant.Web.Services;

namespace TheAccountant.Web.Models.ViewModels
{
    public class TransactionListViewModel
    {
        public List<Transaction> Transactions { get; set; } = new();

        public List<string> AvailableTags { get; set; } = new();

        public string? Tag { get; set; }

        public decimal PostedExpenses => -Transactions
            .Where(t =>
                !t.IsPending &&
                TransactionClassifier.Classify(t) ==
                    CashFlowKind.Spending)
            .Sum(t => t.Amount);

        public decimal PostedCredits => Transactions
            .Where(t =>
                !t.IsPending &&
                TransactionClassifier.Classify(t) ==
                    CashFlowKind.Income)
            .Sum(t => t.Amount);

        public decimal NetOutflow =>
            PostedExpenses - PostedCredits;

        public decimal PendingExpenses => -Transactions
            .Where(t =>
                t.IsPending &&
                TransactionClassifier.Classify(t) ==
                    CashFlowKind.Spending)
            .Sum(t => t.Amount);
    }
}