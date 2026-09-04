using System.ComponentModel.DataAnnotations;

namespace TheAccountant.Web.Models
{
    public class ImportRow
    {
        public int Id { get; set; }

        public Guid ImportBatchId { get; set; }

        public ImportBatch ImportBatch { get; set; } = null!;

        public int RowNumber { get; set; }

        public DateTime? Date { get; set; }

        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Merchant { get; set; }

        public decimal? Amount { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(64)]
        public string? ImportHash { get; set; }

        public bool IsDuplicate { get; set; }

        public bool IsValid { get; set; } = true;

        [StringLength(500)]
        public string? ValidationError { get; set; }
    }
}