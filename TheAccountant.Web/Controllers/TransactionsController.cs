using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheAccountant.Web.Data;
using TheAccountant.Web.Models;
using TheAccountant.Web.Models.Enums;
using TheAccountant.Web.Models.ViewModels;

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
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            var transactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Account)
                .Where(t => t.Account.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            return View(transactions);
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
                Notes = transaction.Notes
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

            if (!ModelState.IsValid)
            {
                await PopulateAccountOptionsAsync(model);

                return View(model);
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

            transaction.AccountId = model.AccountId;
            transaction.Date = model.Date;
            transaction.Description = model.Description.Trim();
            transaction.Merchant = model.Merchant?.Trim();

            transaction.Amount =
                model.Direction == TransactionDirection.Expense
                    ? -Math.Abs(model.Amount)
                    : Math.Abs(model.Amount);

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

    }
}