using System.ComponentModel.DataAnnotations;

namespace TheAccountant.Models.ViewModels
{
    public class ImportViewModel
    {
        [Display(Name = "Transaction File")]
        [Required(ErrorMessage = "Please select a file to upload.")]
        public IFormFile ImportFile { get; set; }

        public List<TransactionPreview> PreviewResults { get; set; } = new();

        public bool HasErrors =>
            PreviewResults?.Any(r => !string.IsNullOrEmpty(r.ValidationError)) ?? false;
    }

    public class TransactionPreview
    {
        public string Description { get; set; }

        [DataType(DataType.Currency)]
        public decimal? Amount { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Date { get; set; }

        public string ValidationError { get; set; }
    }
}
