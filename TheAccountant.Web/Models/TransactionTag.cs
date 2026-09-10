using System.ComponentModel.DataAnnotations;

namespace TheAccountant.Web.Models
{
    public class TransactionTag
    {
        public int Id { get; set; }

        public int TransactionId { get; set; }

        public Transaction Transaction { get; set; } = null!;

        [Required]
        [StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string NormalizedName { get; set; } = string.Empty;
    }
}