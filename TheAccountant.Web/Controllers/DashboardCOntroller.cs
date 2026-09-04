using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheAccountant.Web.Data;
using TheAccountant.Web.Models;
using TheAccountant.Web.Models.Enums;
using TheAccountant.Web.Models.ViewModels;

namespace TheAccountant.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId is null)
            {
                return Challenge();
            }

            var accounts = await _context.Accounts
                .AsNoTracking()
                .Where(a =>
                    a.UserId == userId &&
                    a.IsActive)
                .OrderBy(a => a.InstitutionName)
                .ThenBy(a => a.AccountName)
                .ToListAsync();

            var cashBalance = accounts
                .Where(a =>
                    a.AccountType == AccountType.Checking ||
                    a.AccountType == AccountType.Savings)
                .Sum(a => a.CurrentBalance);

            var creditDebt = accounts
                .Where(a =>
                    a.AccountType == AccountType.CreditCard)
                .Sum(a => Math.Abs(a.CurrentBalance));

            var assetBalance = accounts
                .Where(a =>
                    a.AccountType == AccountType.Checking ||
                    a.AccountType == AccountType.Savings ||
                    a.AccountType == AccountType.Investment)
                .Sum(a => a.CurrentBalance);

            var liabilityBalance = accounts
                .Where(a =>
                    a.AccountType == AccountType.CreditCard ||
                    a.AccountType == AccountType.Loan ||
                    a.AccountType == AccountType.Mortgage)
                .Sum(a => Math.Abs(a.CurrentBalance));

            var model = new DashboardViewModel
            {
                CashBalance = cashBalance,
                CreditDebt = creditDebt,
                NetWorth = assetBalance - liabilityBalance,
                MonthlySpending = 4218.63m, // mock for now

                Accounts = accounts
            };

            return View(model);
        }
    }
}