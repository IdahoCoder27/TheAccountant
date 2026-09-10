using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheAccountant.Web.Data;
using TheAccountant.Web.Models;
using TheAccountant.Web.Models.Enums;
using TheAccountant.Web.Models.ViewModels;
using TheAccountant.Web.Services;

namespace TheAccountant.Web.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? tag)
        {
            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            var query = _context.Transactions
                .AsNoTracking()
                .Where(t => t.Account.UserId == userId);

            var availableTags = await query
                .SelectMany(t => t.Tags)
                .Select(t => t.Name)
                .Distinct()
                .ToListAsync();

            tag = string.IsNullOrWhiteSpace(tag)
                ? null
                : TransactionTagNames.NormalizeWhitespace(tag);

            if (tag is not null)
            {
                if (tag.Length > 80)
                {
                    return BadRequest();
                }

                var normalizedTag =
                    TransactionTagNames.Normalize(tag);

                query = query.Where(t =>
                    t.Tags.Any(transactionTag =>
                        transactionTag.NormalizedName == normalizedTag));
            }

            var transactions = await query
                .Include(t => t.Account)
                .Include(t => t.Tags)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            var model = new TransactionListViewModel
            {
                Transactions = transactions,

                AvailableTags = availableTags
                    .DistinctBy(TransactionTagNames.Normalize)
                    .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                    .ToList(),

                Tag = tag
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new TransactionViewModel
            {
                Date = DateTime.Today
            };

            await PopulateAccountOptionsAsync(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            TransactionViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            var accountExists = await _context.Accounts
                .AnyAsync(a =>
                    a.Id == model.AccountId &&
                    a.UserId == userId &&
                    a.IsActive);

            if (!accountExists)
            {
                ModelState.AddModelError(
                    nameof(model.AccountId),
                    "Select a valid account.");
            }

            if (!TransactionTagNames.TryParse(
                model.TagsText,
                out var tagNames,
                out var tagError))
            {
                ModelState.AddModelError(
                    nameof(model.TagsText),
                    tagError!);
            }

            if (!ModelState.IsValid)
            {
                await PopulateAccountOptionsAsync(model);

                return View(model);
            }

            var transaction = new Transaction
            {
                AccountId = model.AccountId,
                Date = model.Date,
                Description = model.Description.Trim(),
                Merchant = model.Merchant?.Trim(),
                Amount = model.Direction == TransactionDirection.Expense
                            ? -Math.Abs(model.Amount)
                            : Math.Abs(model.Amount),
                Category = model.Category?.Trim(),
                IsPending = model.IsPending,
                IsRecurring = model.IsRecurring,
                Notes = model.Notes?.Trim(),
                Source = TransactionSource.Manual,
                CreatedUtc = DateTime.UtcNow
            };

            TransactionTagNames.Apply(transaction, tagNames);

            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateAccountOptionsAsync(
            TransactionViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return;
            }

            model.AccountOptions = await _context.Accounts
                .AsNoTracking()
                .Where(a =>
                    a.UserId == userId &&
                    a.IsActive)
                .OrderBy(a => a.InstitutionName)
                .ThenBy(a => a.AccountName)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),

                    Text =
                        $"{a.InstitutionName} {a.AccountName}" +
                        (a.LastFour != null
                            ? $" •••• {a.LastFour}"
                            : "")
                })
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            var transaction = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Account)
                .Include(t => t.Tags)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.Account.UserId == userId);

            if (transaction is null)
            {
                return NotFound();
            }

            var model = new TransactionViewModel
            {
                Id = transaction.Id,
                AccountId = transaction.AccountId,
                Date = transaction.Date,
                Description = transaction.Description,
                Merchant = transaction.Merchant,

                // UI always shows the magnitude.
                Amount = Math.Abs(transaction.Amount),

                // Sign determines the UI type.
                Direction = transaction.Amount < 0
                    ? TransactionDirection.Expense
                    : TransactionDirection.Income,

                Category = transaction.Category,
                IsPending = transaction.IsPending,
                IsRecurring = transaction.IsRecurring,
                Notes = transaction.Notes,
                TagsText = string.Join(
                    ", ",
                    transaction.Tags
                        .OrderBy(t => t.Name)
                        .Select(t => t.Name)),
            };

            await PopulateAccountOptionsAsync(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
                int id,
                TransactionViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            var accountExists = await _context.Accounts
                .AnyAsync(a =>
                    a.Id == model.AccountId &&
                    a.UserId == userId);

            if (!accountExists)
            {
                ModelState.AddModelError(
                    nameof(model.AccountId),
                    "Select a valid account.");
            }

            if (!TransactionTagNames.TryParse(
                model.TagsText,
                out var tagNames,
                out var tagError))
            {
                ModelState.AddModelError(
                    nameof(model.TagsText),
                    tagError!);
            }

            if (!ModelState.IsValid)
            {
                await PopulateAccountOptionsAsync(model);

                return View(model);
            }

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Tags)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.Account.UserId == userId);

            if (transaction is null)
            {
                return NotFound();
            }

            transaction.AccountId = model.AccountId;
            transaction.Date = model.Date;
            transaction.Description = model.Description.Trim();
            transaction.Merchant = model.Merchant?.Trim();

            transaction.Amount =
                model.Direction == TransactionDirection.Expense
                    ? -Math.Abs(model.Amount)
                    : Math.Abs(model.Amount);
            TransactionTagNames.Apply(transaction, tagNames);
            transaction.Category = model.Category?.Trim();
            transaction.IsPending = model.IsPending;
            transaction.IsRecurring = model.IsRecurring;
            transaction.Notes = model.Notes?.Trim();

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.Account.UserId == userId);

            if (transaction is null)
            {
                return NotFound();
            }

            _context.Transactions.Remove(transaction);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(
    int id,
    string category)
        {
            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(category) ||
                !TransactionCategories.All.Contains(
                    category,
                    StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Select a valid category.";

                return RedirectToAction(nameof(Index));
            }

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.Account.UserId == userId);

            if (transaction is null)
            {
                return NotFound();
            }

            transaction.Category = category;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{transaction.Description} categorized as {category}.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTags(
    int id,
    string? tagsText,
    string? tag)
        {
            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            var transaction = await _context.Transactions
                .Include(t => t.Tags)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.Account.UserId == userId);

            if (transaction is null)
            {
                return NotFound();
            }

            if (!TransactionTagNames.TryParse(
                tagsText,
                out var names,
                out var error))
            {
                TempData["Error"] = error;

                return RedirectToAction(nameof(Index), new { tag });
            }

            TransactionTagNames.Apply(transaction, names);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Transaction tags saved.";

            return RedirectToAction(nameof(Index), new { tag });
        }
    }
}