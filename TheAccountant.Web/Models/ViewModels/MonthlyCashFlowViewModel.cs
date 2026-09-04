namespace TheAccountant.Web.Models.ViewModels
{
    public class MonthlyCashFlowViewModel
    {
        public string Month { get; set; } = string.Empty;

        public decimal Income { get; set; }

        public decimal Spending { get; set; }
    }
}