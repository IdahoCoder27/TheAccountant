using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TheAccountant.Models.ViewModels
{
    public class InstitutionViewModel
    {
        [Required]
        [Display(Name = "Institution Name")]
        public string Name { get; set; }

        [Display(Name = "Login Type")]
        public string LoginType { get; set; }  // Optional for now, expand later (OAuth, Manual, etc.)

        [Display(Name = "Account Types")]
        public List<string> AccountTypes { get; set; } = new List<string>();
    }
}
