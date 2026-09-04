using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TheAccountant.Web.Models.ViewModels
{
    public class ImportViewModel
    {
        [Required]
        [Display(Name = "Account")]
        public int AccountId { get; set; }

        [Required]
        [Display(Name = "Transaction File")]
        public IFormFile? File { get; set; }

        public List<SelectListItem> AccountOptions { get; set; } = new();
    }
}