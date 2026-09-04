using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using TheAccountant.Web.Interfaces;
using TheAccountant.Web.Models.DTOs;

namespace TheAccountant.Web.Services.Import
{
    public class ChaseCsvTransactionParser
        : ITransactionFileParser
    {
        public string InstitutionName => "Chase";

        public bool CanParse(string fileName)
        {
            return Path.GetExtension(fileName)
                .Equals(".csv", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IReadOnlyList<TransactionImportRowDto>> ParseAsync(
            Stream fileStream)
        {
            var results = new List<TransactionImportRowDto>();

            using var reader = new StreamReader(
                fileStream,
                leaveOpen: true);

            var configuration = new CsvConfiguration(
                CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(
                reader,
                configuration);

            csv.Context.RegisterClassMap<ChaseCsvRecordMap>();

            if (!await csv.ReadAsync())
            {
                return results;
            }

            csv.ReadHeader();

            var rowNumber = 1;

            while (await csv.ReadAsync())
            {
                rowNumber++;

                var source = csv.GetRecord<ChaseCsvRecord>();

                if (source is null)
                {
                    continue;
                }

                var row = new TransactionImportRowDto
                {
                    RowNumber = rowNumber,

                    Description = NormalizeDescription(
                        source.Description),

                    ExternalReference =
                        NullIfEmpty(source.CheckOrSlipNumber),

                    TransactionType =
                        NullIfEmpty(source.Type),

                    IsValid = true
                };

                // Posting date
                if (DateTime.TryParseExact(
                    source.PostingDate,
                    "MM/dd/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
                {
                    row.Date = date;
                }
                else
                {
                    Invalidate(
                        row,
                        $"Invalid posting date '{source.PostingDate}'.");
                }

                // Transaction amount
                if (TryParseMoney(
                    source.Amount,
                    out var amount))
                {
                    row.Amount = amount;
                }
                else
                {
                    Invalidate(
                        row,
                        $"Invalid amount '{source.Amount}'.");
                }

                // Chase leaves Balance blank for pending transactions.
                if (string.IsNullOrWhiteSpace(source.Balance))
                {
                    row.Balance = null;
                    row.IsPending = true;
                }
                else if (TryParseMoney(
                    source.Balance,
                    out var balance))
                {
                    row.Balance = balance;
                    row.IsPending = false;
                }
                else
                {
                    Invalidate(
                        row,
                        $"Invalid balance '{source.Balance}'.");
                }

                // Description is required.
                if (string.IsNullOrWhiteSpace(row.Description))
                {
                    Invalidate(
                        row,
                        "Transaction description is missing.");
                }

                results.Add(row);
            }

            return results;
        }

        private static bool TryParseMoney(
            string? value,
            out decimal result)
        {
            result = 0m;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return decimal.TryParse(
                value.Trim(),
                NumberStyles.Currency |
                NumberStyles.AllowLeadingSign,
                CultureInfo.GetCultureInfo("en-US"),
                out result);
        }

        private static string NormalizeDescription(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return string.Join(
                " ",
                value.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries));
        }

        private static string? NullIfEmpty(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static void Invalidate(
            TransactionImportRowDto row,
            string error)
        {
            row.IsValid = false;

            row.ValidationError =
                string.IsNullOrWhiteSpace(row.ValidationError)
                    ? error
                    : $"{row.ValidationError} {error}";
        }
    }
}