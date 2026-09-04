using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TheAccountant.Web.Data;
using TheAccountant.Web.Interfaces;
using TheAccountant.Web.Models;
using TheAccountant.Web.Models.DTOs;
using TheAccountant.Web.Models.Enums;

namespace TheAccountant.Web.Services.Import
{
    public class TransactionImportService
        : ITransactionImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEnumerable<ITransactionFileParser> _parsers;

        public TransactionImportService(
            ApplicationDbContext context,
            IEnumerable<ITransactionFileParser> parsers)
        {
            _context = context;
            _parsers = parsers;
        }

        public async Task<Guid> CreatePreviewAsync(
            string userId,
            int accountId,
            string fileName,
            Stream fileStream)
        {
            var account = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.Id == accountId &&
                    a.UserId == userId &&
                    a.IsActive);

            if (account is null)
            {
                throw new InvalidOperationException(
                    "The selected account was not found.");
            }

            var parser = _parsers.FirstOrDefault(p =>
                p.InstitutionName.Equals(
                    account.InstitutionName,
                    StringComparison.OrdinalIgnoreCase)
                && p.CanParse(fileName));

            if (parser is null)
            {
                throw new InvalidOperationException(
                    $"No import parser is available for " +
                    $"{account.InstitutionName} files of this type.");
            }

            var parsedRows =
                await parser.ParseAsync(fileStream);

            if (parsedRows.Count == 0)
            {
                throw new InvalidOperationException(
                    "No transactions were found in the uploaded file.");
            }

            var existingHashes = (
                await _context.Transactions
                    .AsNoTracking()
                    .Where(t =>
                        t.AccountId == accountId &&
                        t.ImportHash != null)
                    .Select(t => t.ImportHash!)
                    .ToListAsync())
                .ToHashSet(StringComparer.Ordinal);

            var seenHashes = new HashSet<string>(
                StringComparer.Ordinal);

            var batch = new ImportBatch
            {
                UserId = userId,
                AccountId = accountId,
                FileName = Path.GetFileName(fileName),
                Status = ImportBatchStatus.Pending,
                CreatedUtc = DateTime.UtcNow
            };

            foreach (var parsedRow in parsedRows)
            {
                string? importHash = null;
                var isDuplicate = false;

                if (parsedRow.IsValid &&
                    parsedRow.Date.HasValue &&
                    parsedRow.Amount.HasValue)
                {
                    importHash = ComputeImportHash(
                        accountId,
                        parsedRow);

                    var alreadyInDatabase =
                        existingHashes.Contains(importHash);

                    var alreadyInFile =
                        !seenHashes.Add(importHash);

                    isDuplicate =
                        alreadyInDatabase ||
                        alreadyInFile;
                }

                batch.Rows.Add(new ImportRow
                {
                    RowNumber = parsedRow.RowNumber,
                    Date = parsedRow.Date,
                    Description = parsedRow.Description,
                    Merchant = parsedRow.Merchant,
                    Amount = parsedRow.Amount,
                    Balance = parsedRow.Balance,
                    IsPending = parsedRow.IsPending,
                    Category = parsedRow.Category,
                    ExternalReference = parsedRow.ExternalReference,
                    TransactionType = parsedRow.TransactionType,
                    ImportHash = importHash,
                    IsDuplicate = isDuplicate,
                    IsValid = parsedRow.IsValid,
                    ValidationError = parsedRow.ValidationError
                });
            }

            _context.ImportBatches.Add(batch);

            await _context.SaveChangesAsync();

            return batch.Id;
        }

        private static string ComputeImportHash(
            int accountId,
            TransactionImportRowDto row)
        {
            var description =
                NormalizeForHash(row.Description);

            var reference =
                NormalizeForHash(row.ExternalReference);

            var balance = row.Balance.HasValue
                ? row.Balance.Value.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture)
                : string.Empty;

            var canonical = string.Join(
                "|",
                accountId.ToString(
                    CultureInfo.InvariantCulture),

                row.Date!.Value.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture),

                row.Amount!.Value.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture),

                balance,

                description,

                reference);

            var bytes =
                Encoding.UTF8.GetBytes(canonical);

            var hash =
                SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }

        private static string NormalizeForHash(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return string.Join(
                    " ",
                    value.Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries))
                .Trim()
                .ToUpperInvariant();
        }

        public async Task<int> ConfirmImportAsync(string userId, Guid batchId)
        {
            var batch = await _context.ImportBatches
                .Include(b => b.Rows)
                .FirstOrDefaultAsync(b =>
                    b.Id == batchId &&
                    b.UserId == userId);

            if (batch is null)
            {
                throw new InvalidOperationException(
                    "The import batch was not found.");
            }

            if (batch.Status != ImportBatchStatus.Pending)
            {
                throw new InvalidOperationException(
                    "This import batch has already been processed.");
            }

            var candidateRows = batch.Rows
                .Where(r =>
                    r.IsValid &&
                    !r.IsDuplicate &&
                    r.Date.HasValue &&
                    r.Amount.HasValue &&
                    !string.IsNullOrWhiteSpace(r.ImportHash))
                .OrderBy(r => r.RowNumber)
                .ToList();

            if (candidateRows.Count == 0)
            {
                throw new InvalidOperationException(
                    "There are no new transactions to import.");
            }

            // Re-check against the database at confirmation time.
            // A second import could have completed after this preview was created.
            var candidateHashes = candidateRows
                .Select(r => r.ImportHash!)
                .ToList();

            var existingHashes = await _context.Transactions
                .AsNoTracking()
                .Where(t =>
                    t.AccountId == batch.AccountId &&
                    t.ImportHash != null &&
                    candidateHashes.Contains(t.ImportHash))
                .Select(t => t.ImportHash!)
                .ToListAsync();

            var existingHashSet = existingHashes
                .ToHashSet(StringComparer.Ordinal);

            var importedCount = 0;

            foreach (var row in candidateRows)
            {
                if (existingHashSet.Contains(row.ImportHash!))
                {
                    row.IsDuplicate = true;
                    continue;
                }

                var transaction = new Transaction
                {
                    AccountId = batch.AccountId,
                    Date = row.Date!.Value,
                    Description = row.Description,
                    Merchant = row.Merchant,
                    Amount = row.Amount!.Value,
                    Category = !string.IsNullOrWhiteSpace(row.Category)
                                ? row.Category
                                : TransactionCategorizer.Categorize(row.Description),
                    IsPending = row.IsPending,
                    IsRecurring = false,
                    Source = TransactionSource.FileImport,
                    ImportHash = row.ImportHash,
                    ImportBatchId = batch.Id,
                    CreatedUtc = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);

                existingHashSet.Add(row.ImportHash!);

                importedCount++;
            }

            if (importedCount == 0)
            {
                throw new InvalidOperationException(
                    "All transactions in this import already exist.");
            }

            batch.Status = ImportBatchStatus.Completed;
            batch.CompletedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return importedCount;
        }

    }
}