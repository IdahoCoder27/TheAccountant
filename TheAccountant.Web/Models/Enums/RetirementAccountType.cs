using System.ComponentModel.DataAnnotations;

namespace TheAccountant.Web.Models.Enums
{
    public enum RetirementAccountType
    {
        [Display(Name = "Traditional 401(k)")]
        Traditional401k,

        [Display(Name = "Roth 401(k)")]
        Roth401k,

        [Display(Name = "Traditional IRA")]
        TraditionalIRA,

        [Display(Name = "Roth IRA")]
        RothIRA,

        [Display(Name = "SEP IRA")]
        SEPIRA,

        [Display(Name = "SIMPLE IRA")]
        SIMPLEIRA,

        [Display(Name = "Pension")]
        Pension,

        Other
    }
}