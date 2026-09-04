using TheAccountant.Web.Models;
using TheAccountant.Web.Models.Enums;

namespace TheAccountant.Web.Services
{
    public static class TransactionClassifier
    {
        public static CashFlowKind Classify(Transaction transaction)
        {
            var description = Normalize(transaction.Description);

            // Money entering the account because of financing is not income.
            if (IsFinancing(description))
            {
                return CashFlowKind.Financing;
            }

            // Money merely moving between accounts is neither income nor spending.
            if (IsTransfer(description))
            {
                return CashFlowKind.Transfer;
            }

            // Contributions to investment accounts are not ordinary spending.
            if (IsInvestment(description))
            {
                return CashFlowKind.Investment;
            }

            // Paying debt should not be counted again as spending.
            if (IsDebtPayment(description))
            {
                return CashFlowKind.DebtPayment;
            }

            return transaction.Amount >= 0
                ? CashFlowKind.Income
                : CashFlowKind.Spending;
        }

        private static bool IsTransfer(string description)
        {
            return
                description.Contains("TRANSFER TO SAV") ||
                description.Contains("TRANSFER FROM SAV") ||
                description.Contains("TRANSFER TO CHECK") ||
                description.Contains("TRANSFER FROM CHECK") ||
                description.Contains("ONLINE TRANSFER") ||
                description.Contains("ODP TRANSFER") ||
                description.Contains("EXT TRANS");
        }

        private static bool IsInvestment(string description)
        {
            return
                description.Contains("EDWARD JONES") ||
                description.Contains("INVESTMENT");
        }

        private static bool IsDebtPayment(string description)
        {
            return
                description.Contains("CHASE CREDIT CRD AUTOPAY") ||
                description.Contains("PAYMENT TO CHASE CARD") ||
                description.Contains("AMERICAN EXPRESS ACH PMT") ||
                description.Contains("CAPITAL ONE MOBILE PMT") ||
                description.Contains("ROCKET MORTGAGE") ||
                description.Contains("MORTGAGE LOAN");
        }

        private static bool IsFinancing(string description)
        {
            return
                description.Contains("ROCKET CLOSE") ||
                description.Contains("LOAN PROCEEDS") ||
                description.Contains("CLOSING PROCEEDS");
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return string.Join(
                    " ",
                    value.Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
        }
    }
}