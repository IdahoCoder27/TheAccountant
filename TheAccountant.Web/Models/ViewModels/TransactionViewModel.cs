using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TheAccountant.Web.Models.Enums;

namespace TheAccountant.Web.Models.ViewModels
{
    public class TransactionViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Type")]
        public TransactionDirection Direction { get; set; } = TransactionDirection.Expense;

        [Required]
        [Display(Name = "Account")]
        public int AccountId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Transaction Date")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Merchant { get; set; }

        [Required]
        [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [Display(Name = "Pending")]
        public bool IsPending { get; set; }

        [Display(Name = "Recurring")]
        public bool IsRecurring { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(1000)]
        [Display(Name = "Tags")]
        public string? TagsText { get; set; }

        public List<SelectListItem> AccountOptions { get; set; } = new();
        public List<string> AvailableTags { get; set; } = new();
    }
}