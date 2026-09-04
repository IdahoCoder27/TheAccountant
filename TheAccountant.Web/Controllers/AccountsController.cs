using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheAccountant.Web.Data;
using TheAccountant.Web.Models;
using TheAccountant.Web.Models.ViewModels;

namespace TheAccountant.Web.Controllers
{
    [Authorize]
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountsController(
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

            var accounts = await _context.Accounts
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsActive)
                .ThenBy(a => a.InstitutionName)
                .ThenBy(a => a.AccountName)
                .ToListAsync();

            return View(accounts);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new AccountViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            var account = new Account
            {
                UserId = userId,
                InstitutionName = model.InstitutionName.Trim(),
                AccountName = model.AccountName.Trim(),
                AccountType = model.AccountType,
                LastFour = model.LastFour,
                CurrentBalance = model.CurrentBalance,
                AvailableBalance = model.AvailableBalance,
                IsActive = true,
                LastUpdated = DateTime.UtcNow
            };

            _context.Accounts.Add(account);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            var account = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.UserId == userId);

            if (account is null)
            {
                return NotFound();
            }

            var model = new AccountViewModel
            {
                Id = account.Id,
                InstitutionName = account.InstitutionName,
                AccountName = account.AccountName,
                AccountType = account.AccountType,
                LastFour = account.LastFour,
                CurrentBalance = account.CurrentBalance,
                AvailableBalance = account.AvailableBalance
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            AccountViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.UserId == userId);

            if (account is null)
            {
                return NotFound();
            }

            account.InstitutionName = model.InstitutionName.Trim();
            account.AccountName = model.AccountName.Trim();
            account.AccountType = model.AccountType;
            account.LastFour = model.LastFour;
            account.CurrentBalance = model.CurrentBalance;
            account.AvailableBalance = model.AvailableBalance;
            account.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var userId = _userManager.GetUserId(User);

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.UserId == userId);

            if (account is null)
            {
                return NotFound();
            }

            account.IsActive = false;
            account.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id)
        {
            var userId = _userManager.GetUserId(User);

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.UserId == userId);

            if (account is null)
            {
                return NotFound();
            }

            account.IsActive = true;
            account.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            var account = await _context.Accounts
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.UserId == userId);

            if (account is null)
            {
                return NotFound();
            }

            if (account.Transactions.Count != 0)
            {
                TempData["Error"] =
                    "Accounts with transaction history cannot be deleted. Close the account instead.";

                return RedirectToAction(nameof(Index));
            }

            _context.Accounts.Remove(account);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}