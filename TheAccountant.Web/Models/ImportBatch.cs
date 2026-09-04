using TheAccountant.Web.Models.Enums;

namespace TheAccountant.Web.Models
{
    public class ImportBatch
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        public int AccountId { get; set; }

        public Account Account { get; set; } = null!;

        public string FileName { get; set; } = string.Empty;

        public ImportBatchStatus Status { get; set; }
            = ImportBatchStatus.Pending;

        public DateTime CreatedUtc { get; set; }
            = DateTime.UtcNow;

        public DateTime? CompletedUtc { get; set; }

        public ICollection<ImportRow> Rows { get; set; }
            = new List<ImportRow>();
    }
}