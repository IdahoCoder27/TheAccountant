using System.ComponentModel.DataAnnotations;
using TheAccountant.Web.Models.Enums;

namespace TheAccountant.Web.Models.ViewModels
{
    public class AccountViewModel
    {
        public int Id { get; set; }

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

        [StringLength(4, MinimumLength = 4)]
        [RegularExpression(@"^\d{4}$",
            ErrorMessage = "Enter the last four digits.")]
        [Display(Name = "Last 4 Digits")]
        public string? LastFour { get; set; }

        [Display(Name = "Current Balance")]
        [DataType(DataType.Currency)]
        public decimal CurrentBalance { get; set; }

        [Display(Name = "Available Balance")]
        [DataType(DataType.Currency)]
        public decimal? AvailableBalance { get; set; }
    }
}