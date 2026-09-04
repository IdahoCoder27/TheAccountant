namespace TheAccountant.Web.Services.Import
{
    internal class ChaseCsvRecord
    {
        public string Details { get; set; } = string.Empty;

        public string PostingDate { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Amount { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Balance { get; set; } = string.Empty;

        public string CheckOrSlipNumber { get; set; } = string.Empty;
    }
}
