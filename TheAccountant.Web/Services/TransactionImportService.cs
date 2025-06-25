// TransactionImportService.cs
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.Data;
using System.Globalization;
using TheAccountant.Interfaces;
using TheAccountant.Models;
using TheAccountant.Web.Data; // Adjust namespace to your actual models

namespace TheAccountant.Services
{
    public class TransactionPreview
    {
        public DateTime? Date { get; set; }
        public string Description { get; set; }
        public decimal? Amount { get; set; }
        public List<string> Errors { get; set; } = new();

        public bool IsValid =>
            Date.HasValue && !string.IsNullOrWhiteSpace(Description) && Amount.HasValue;
    }


    public class TransactionImportService : ITransactionImportService
    {
        public async Task<IEnumerable<TransactionDto>> ImportAsync(Stream fileStream, string fileType)
        {
            if (fileType.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return await ParseExcelAsync(fileStream);
            }
            else if (fileType.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return await ParsePdfAsync(fileStream);
            }

            throw new NotSupportedException("Unsupported file type.");
        }
        }

        private async Task<IEnumerable<TransactionDto>> ParseExcelAsync(Stream stream)
        {
            var results = new List<TransactionDto>();
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
                return results;

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                results.Add(new TransactionDto
                {
                    Date = DateTime.Parse(worksheet.Cells[row, 1].Text),
                    Description = worksheet.Cells[row, 2].Text,
                    Amount = decimal.Parse(worksheet.Cells[row, 3].Text),
                    Category = worksheet.Cells[row, 4].Text
                });
            }

            return results;
        }

        public List<TransactionPreview> PreviewTransactions(IFormFile file)
        {
            var result = new List<TransactionPreview>();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var stream = new MemoryStream();
            file.CopyTo(stream);
            using var package = new ExcelPackage(stream);
            var sheet = package.Workbook.Worksheets[0];

            int rowCount = sheet.Dimension.End.Row;

            for (int row = 2; row <= rowCount; row++)
            {
                var preview = new TransactionPreview
                {
                    Date = DateTime.TryParse(sheet.Cells[row, 1].Text, out var parsedDate) ? parsedDate : null,
                    Description = sheet.Cells[row, 2].Text,
                    Amount = decimal.TryParse(sheet.Cells[row, 3].Text, out var parsedAmount) ? parsedAmount : null
                };

                if (!preview.Date.HasValue) preview.Errors.Add("Invalid or missing date.");
                if (string.IsNullOrWhiteSpace(preview.Description)) preview.Errors.Add("Missing description.");
                if (!preview.Amount.HasValue) preview.Errors.Add("Invalid or missing amount.");

                result.Add(preview);
            }

            return result;
        }

        public async Task<int> ImportValidTransactionsAsync(
                List<TransactionPreview> previewList,
                ApplicationDbContext db)
        {
            var valid = previewList
                .Where(p => p.IsValid)
                .Select(p => new Transaction
                {
                    Date = p.Date.Value,
                    Description = p.Description,
                    Amount = p.Amount.Value
                }).ToList();

            db.Transactions.AddRange(valid);
            return await db.SaveChangesAsync();
        }

        private async Task<IEnumerable<TransactionDto>> ParsePdfAsync(Stream stream)
        {
            var results = new List<TransactionDto>();
            // Use iText7, PdfPig, or another PDF parser here.
            // Placeholder for now.
            return results;
        }
    }

}

