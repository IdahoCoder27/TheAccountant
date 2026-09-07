using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TheAccountant.Web.Data;
using TheAccountant.Web.Interfaces;
using TheAccountant.Web.Models;
using TheAccountant.Web.Models.DTOs;
using TheAccountant.Web.Models.Enums;
using System.Text.RegularExpressions;

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

            var fileAccountLastFour =
    parser.GetAccountLastFour(fileName);

            if (!string.IsNullOrWhiteSpace(fileAccountLastFour) &&
                !string.IsNullOrWhiteSpace(account.LastFour) &&
                !fileAccountLastFour.Equals(
                    account.LastFour,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"This file appears to belong to account " +
                    $"•••• {fileAccountLastFour}, but the selected account is " +
                    $"•••• {account.LastFour}.");
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

        public async Task<int> ConfirmImportAsync(
    string userId,
    Guid batchId)
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

            //
            // Exact duplicate protection.
            //
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

            //
            // Load pending transactions once.
            //
            // Chase may post them several days later, especially across weekends.
            //
            var earliestDate = candidateRows
                .Min(r => r.Date!.Value)
                .AddDays(-7);

            var latestDate = candidateRows
                .Max(r => r.Date!.Value)
                .AddDays(1);

            var pendingTransactions = await _context.Transactions
                .Where(t =>
                    t.AccountId == batch.AccountId &&
                    t.IsPending &&
                    t.Date >= earliestDate &&
                    t.Date <= latestDate)
                .ToListAsync();

            var processedCount = 0;

            foreach (var row in candidateRows)
            {
                //
                // Exact duplicate.
                //
                if (existingHashSet.Contains(row.ImportHash!))
                {
                    row.IsDuplicate = true;
                    continue;
                }

                //
                // A posted transaction may be the final version
                // of something we imported earlier as pending.
                //
                if (!row.IsPending)
                {
                    var pendingMatch = FindPendingMatch(
                        pendingTransactions,
                        row);

                    if (pendingMatch is not null)
                    {
                        pendingMatch.Date =
                            row.Date!.Value;

                        pendingMatch.Description =
                            row.Description;

                        pendingMatch.Merchant =
                            row.Merchant;

                        pendingMatch.Amount =
                            row.Amount!.Value;

                        pendingMatch.Category =
                            !string.IsNullOrWhiteSpace(row.Category)
                                ? row.Category
                                : TransactionCategorizer.Categorize(
                                    row.Description);

                        pendingMatch.IsPending = false;

                        pendingMatch.ImportHash =
                            row.ImportHash;

                        //
                        // The transaction was originally imported
                        // from an earlier batch. The current batch is
                        // the one that established its posted state.
                        //
                        pendingMatch.ImportBatchId =
                            batch.Id;

                        existingHashSet.Add(
                            row.ImportHash!);

                        pendingTransactions.Remove(
                            pendingMatch);

                        processedCount++;

                        continue;
                    }
                }

                //
                // Completely new transaction.
                //
                var transaction = new Transaction
                {
                    AccountId = batch.AccountId,

                    Date = row.Date!.Value,

                    Description =
                        row.Description,

                    Merchant =
                        row.Merchant,

                    Amount =
                        row.Amount!.Value,

                    Category =
                        !string.IsNullOrWhiteSpace(row.Category)
                            ? row.Category
                            : TransactionCategorizer.Categorize(
                                row.Description),

                    IsPending =
                        row.IsPending,

                    IsRecurring = false,

                    Source =
                        TransactionSource.FileImport,

                    ImportHash =
                        row.ImportHash,

                    ImportBatchId =
                        batch.Id,

                    CreatedUtc =
                        DateTime.UtcNow
                };

                _context.Transactions.Add(
                    transaction);

                existingHashSet.Add(
                    row.ImportHash!);

                processedCount++;
            }

            if (processedCount == 0)
            {
                throw new InvalidOperationException(
                    "All transactions in this import already exist.");
            }

            batch.Status =
                ImportBatchStatus.Completed;

            batch.CompletedUtc =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return processedCount;
        }

        private static Transaction? FindPendingMatch(
    IEnumerable<Transaction> pendingTransactions,
    ImportRow row)
        {
            if (!row.Date.HasValue ||
                !row.Amount.HasValue)
            {
                return null;
            }

            var candidates = pendingTransactions
                .Where(t =>
                    t.Amount == row.Amount.Value &&
                    Math.Abs(
                        (t.Date.Date - row.Date.Value.Date)
                        .TotalDays) <= 7)
                .Select(t => new
                {
                    Transaction = t,
                    Score = GetPendingMatchScore(
                        t.Description,
                        row.Description)
                })
                .Where(x => x.Score >= 60)
                .OrderByDescending(x => x.Score)
                .ThenBy(x =>
                    Math.Abs(
                        (x.Transaction.Date.Date -
                         row.Date.Value.Date).TotalDays))
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            //
            // If two candidates score exactly the same,
            // don't guess with financial data.
            //
            if (candidates.Count > 1 &&
                candidates[0].Score == candidates[1].Score)
            {
                return null;
            }

            return candidates[0].Transaction;
        }

        private static string NormalizeForPendingMatch(
            string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return string.Empty;
            }

            var value =
                description.ToUpperInvariant();

            //
            // Chase commonly prefixes pending transactions
            // differently from their final posted descriptions.
            //
            value = Regex.Replace(
                value,
                @"\b(POS|DEBIT|CARD|PURCHASE|PENDING|RECURRING)\b",
                " ");

            //
            // Remove embedded transaction dates such as 09/04.
            //
            value = Regex.Replace(
                value,
                @"\b\d{1,2}/\d{1,2}\b",
                " ");

            //
            // Ignore punctuation differences.
            //
            value = Regex.Replace(
                value,
                @"[^A-Z0-9]+",
                " ");

            //
            // Collapse whitespace.
            //
            value = Regex.Replace(
                value,
                @"\s+",
                " ");

            return value.Trim();
        }

        private static int GetPendingMatchScore(
    string? first,
    string? second)
        {
            var firstIdentifiers =
                ExtractStableIdentifiers(first);

            var secondIdentifiers =
                ExtractStableIdentifiers(second);

            //
            // Strongest evidence:
            // Chase preserved an ACH/reference identifier.
            //
            if (firstIdentifiers
                .Intersect(
                    secondIdentifiers,
                    StringComparer.OrdinalIgnoreCase)
                .Any())
            {
                return 100;
            }

            var firstTokens =
                ExtractMeaningfulTokens(first);

            var secondTokens =
                ExtractMeaningfulTokens(second);

            if (firstTokens.Count == 0 ||
                secondTokens.Count == 0)
            {
                return 0;
            }

            var intersection =
                firstTokens.Intersect(
                    secondTokens,
                    StringComparer.OrdinalIgnoreCase)
                .Count();

            var union =
                firstTokens.Union(
                    secondTokens,
                    StringComparer.OrdinalIgnoreCase)
                .Count();

            if (union == 0)
            {
                return 0;
            }

            var similarity =
                (double)intersection / union;

            return (int)Math.Round(
                similarity * 100);
        }

        private static HashSet<string> ExtractStableIdentifiers(
    string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
            }

            var matches = Regex.Matches(
                description.ToUpperInvariant(),
                @"\b[A-Z]*\d[A-Z0-9]{6,}\b");

            return matches
                .Select(m => m.Value)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<string> ExtractMeaningfulTokens(
    string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
            }

            var ignoredWords = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
    {
        "POS",
        "DEBIT",
        "CREDIT",
        "CARD",
        "PURCHASE",
        "PENDING",
        "RECURRING",
        "ORIG",
        "CO",
        "NAME",
        "ENTRY",
        "DESCR",
        "SEC",
        "WEB",
        "ID",
        "IND",
        "PPD",
        "CTX"
    };

            var normalized = Regex.Replace(
                description.ToUpperInvariant(),
                @"[^A-Z0-9]+",
                " ");

            return normalized
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(token =>
                    token.Length >= 3 &&
                    !ignoredWords.Contains(token) &&
                    !Regex.IsMatch(
                        token,
                        @"^\d{1,2}/\d{1,2}$"))
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);
        }
    }
}