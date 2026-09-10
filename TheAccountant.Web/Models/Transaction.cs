using System.ComponentModel.DataAnnotations;
using TheAccountant.Web.Models.Enums;

namespace TheAccountant.Web.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }

        public Account Account { get; set; } = null!;

        [Required]
        [Display(Name = "Transaction Date")]
        public DateTime Date { get; set; }

        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Merchant { get; set; }

        public decimal Amount { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public bool IsPending { get; set; }

        public bool IsRecurring { get; set; }

        public TransactionSource Source { get; set; }
            = TransactionSource.Manual;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedUtc { get; set; }
            = DateTime.UtcNow;

        public string? ImportHash { get; set; }

        public Guid? ImportBatchId { get; set; }

        public ImportBatch? ImportBatch { get; set; }

        public List<TransactionTag> Tags { get; set; } = new();
    }
}