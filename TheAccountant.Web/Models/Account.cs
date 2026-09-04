using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TheAccountant.Web.Models.Enums;
using TheAccountant.Web.Models.Enums;

namespace TheAccountant.Web.Models
{
    public class Account
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Institution")]
        public string InstitutionName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Account Name")]
        public string AccountName { get; set; } = string.Empty;

        [Display(Name = "Account Type")]
        public AccountType AccountType { get; set; }

        [Display(Name = "Retirement Account Type")]
        public RetirementAccountType? RetirementAccountType { get; set; }

        [StringLength(4)]
        [Display(Name = "Last 4 Digits")]
        public string? LastFour { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Current Balance")]
        public decimal CurrentBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Available Balance")]
        public decimal? AvailableBalance { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; } = null!;

        public ICollection<Transaction> Transactions { get; set; }
            = new List<Transaction>();
    }
}