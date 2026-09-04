namespace TheAccountant.Web.Models.DTOs
{
    public class TransactionImportRowDto
    {
        public int RowNumber { get; set; }

        public DateTime? Date { get; set; }

        public string Description { get; set; } = string.Empty;

        public string? Merchant { get; set; }

        public decimal? Amount { get; set; }

        public decimal? Balance { get; set; }

        public string? Category { get; set; }

        public string? ExternalReference { get; set; }

        public string? TransactionType { get; set; }

        public bool IsValid { get; set; } = true;

        public string? ValidationError { get; set; }

        public bool IsPending { get; set; }
    }
}