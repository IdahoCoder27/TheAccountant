using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheAccountant.Web.Data;
using TheAccountant.Web.Interfaces;
using TheAccountant.Web.Models;
using TheAccountant.Web.Models.ViewModels;
using TheAccountant.Web.Interfaces;

namespace TheAccountant.Web.Controllers
{
    [Authorize]
    public class ImportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ITransactionImportService _transactionImportService;

        public ImportController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITransactionImportService transactionImportService)
        {
            _context = context;
            _userManager = userManager;
            _transactionImportService =
                transactionImportService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new ImportViewModel();

            await PopulateAccountsAsync(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            ImportViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            var account = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.Id == model.AccountId &&
                    a.UserId == userId &&
                    a.IsActive);

            if (account is null)
            {
                ModelState.AddModelError(
                    nameof(model.AccountId),
                    "Select a valid account.");
            }

            if (model.File is null ||
                model.File.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.File),
                    "Select a transaction file.");
            }

            var allowedExtensions = new[]
            {
                ".csv",
                ".xlsx",
                ".xls"
            };

            if (model.File is not null)
            {
                var extension =
                    Path.GetExtension(model.File.FileName)
                        .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(model.File),
                        "Select an transaction file.");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateAccountsAsync(model);

                return View(model);
            }

            try
            {
                await using var fileStream =
                    model.File!.OpenReadStream();

                var batchId =
                    await _transactionImportService
                        .CreatePreviewAsync(
                            userId,
                            model.AccountId,
                            model.File.FileName,
                            fileStream);

                return RedirectToAction(
                    nameof(Preview),
                    new { id = batchId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    ex.Message);

                await PopulateAccountsAsync(model);

                return View(model);
            }
        }

        private async Task PopulateAccountsAsync(
            ImportViewModel model)
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
        public async Task<IActionResult> Preview(Guid id)
        {
            var userId =
                _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            var batch = await _context.ImportBatches
                .AsNoTracking()
                .Include(b => b.Account)
                .Include(b => b.Rows)
                .FirstOrDefaultAsync(b =>
                    b.Id == id &&
                    b.UserId == userId);

            if (batch is null)
            {
                return NotFound();
            }

            var rows = batch.Rows
                .OrderBy(r => r.RowNumber)
                .ToList();

            var model =
                new ImportPreviewViewModel
                {
                    BatchId = batch.Id,

                    FileName = batch.FileName,

                    AccountName =
                        $"{batch.Account.InstitutionName} " +
                        $"{batch.Account.AccountName}",

                    TotalRows = rows.Count,

                    ValidRows = rows.Count(r =>
                        r.IsValid &&
                        !r.IsDuplicate),

                    DuplicateRows = rows.Count(r =>
                        r.IsDuplicate),

                    InvalidRows = rows.Count(r =>
                        !r.IsValid),

                    Rows = rows.Select(r =>
                        new ImportPreviewRowViewModel
                        {
                            RowNumber = r.RowNumber,
                            Date = r.Date,
                            Description = r.Description,
                            Amount = r.Amount,
                            Balance = r.Balance,
                            TransactionType =
                                r.TransactionType,
                            ExternalReference =
                                r.ExternalReference,
                            IsValid = r.IsValid,
                            IsDuplicate =
                                r.IsDuplicate,
                            ValidationError =
                                r.ValidationError
                        })
                        .ToList()
                };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(Guid id)
        {
            var userId =
                _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            try
            {
                var importedCount =
                    await _transactionImportService
                        .ConfirmImportAsync(
                            userId,
                            id);

                TempData["Success"] =
                    $"{importedCount} transaction" +
                    $"{(importedCount == 1 ? "" : "s")} imported successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction(
                    nameof(Preview),
                    new { id });
            }
        }

    }
}