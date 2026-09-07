namespace TheAccountant.Web.Models.ViewModels
{
    public class ImportPreviewViewModel
    {
        public Guid BatchId { get; set; }

        public string FileName { get; set; }
            = string.Empty;

        public string AccountName { get; set; }
            = string.Empty;

        public int TotalRows { get; set; }

        public int ValidRows { get; set; }

        public int DuplicateRows { get; set; }

        public int InvalidRows { get; set; }

        public List<ImportPreviewRowViewModel> Rows { get; set; }
            = new();
    }

    public class ImportPreviewRowViewModel
    {
        public int RowNumber { get; set; }

        public DateTime? Date { get; set; }

        public string Description { get; set; }
            = string.Empty;

        public decimal? Amount { get; set; }

        public decimal? Balance { get; set; }

        public string? TransactionType { get; set; }

        public string? ExternalReference { get; set; }

        public bool IsValid { get; set; }

        public bool IsDuplicate { get; set; }

        public string? ValidationError { get; set; }

        public bool IsPending { get; set; }
    }
}