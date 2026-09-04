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
                    a.AccountType == AccountType.Brokerage ||
                    a.AccountType == AccountType.Retirement)
                .Sum(a => a.CurrentBalance);

            var liabilityBalance = accounts
                .Where(a =>
                    a.AccountType == AccountType.CreditCard ||
                    a.AccountType == AccountType.Loan ||
                    a.AccountType == AccountType.Mortgage)
                .Sum(a => Math.Abs(a.CurrentBalance));

            var monthStart = new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

            var nextMonthStart = monthStart.AddMonths(1);

            var cashFlowStart = monthStart.AddMonths(-5);

            var cashFlowTransactions = await _context.Transactions
                .AsNoTracking()
                .Where(t =>
                    t.Account.UserId == userId &&
                    t.Date >= cashFlowStart &&
                    t.Date < nextMonthStart &&
                    !t.IsPending)
                .Select(t => new
                {
                    t.Date,
                    t.Amount
                })
                .ToListAsync();

            var cashFlowHistory = new List<MonthlyCashFlowViewModel>();

            for (var i = 0; i < 6; i++)
            {
                var currentMonth = cashFlowStart.AddMonths(i);
                var followingMonth = currentMonth.AddMonths(1);

                var transactionsForMonth = cashFlowTransactions
                    .Where(t =>
                        t.Date >= currentMonth &&
                        t.Date < followingMonth)
                    .ToList();

                var income = transactionsForMonth
                    .Where(t => t.Amount > 0)
                    .Sum(t => t.Amount);

                var spending = transactionsForMonth
                    .Where(t => t.Amount < 0)
                    .Sum(t => Math.Abs(t.Amount));

                cashFlowHistory.Add(new MonthlyCashFlowViewModel
                {
                    Month = currentMonth.ToString("MMM"),
                    Income = income,
                    Spending = spending
                });
            }

            //
            // MONTHLY SPENDING
            //
            // Negative transactions only.
            // Pending transactions are intentionally excluded.
            //
            var monthlySpending = await _context.Transactions
                .AsNoTracking()
                .Where(t =>
                    t.Account.UserId == userId &&
                    t.Date >= monthStart &&
                    t.Date < nextMonthStart &&
                    t.Amount < 0 &&
                    !t.IsPending)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            monthlySpending = Math.Abs(monthlySpending);

            //
            // RECENT TRANSACTIONS
            //
            var recentTransactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Account)
                .Where(t => t.Account.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .Take(5)
                .ToListAsync();

            //
            // SPENDING BY CATEGORY
            //
            var spendingByCategory = await _context.Transactions
                .AsNoTracking()
                .Where(t =>
                    t.Account.UserId == userId &&
                    t.Date >= monthStart &&
                    t.Date < nextMonthStart &&
                    t.Amount < 0 &&
                    !t.IsPending)
                .GroupBy(t =>
                    string.IsNullOrWhiteSpace(t.Category)
                        ? "Uncategorized"
                        : t.Category!)
                .Select(g => new CategorySpendViewModel
                {
                    Category = g.Key,
                    Amount = Math.Abs(g.Sum(t => t.Amount))
                })
                .OrderByDescending(x => x.Amount)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                CashBalance = cashBalance,
                CreditDebt = creditDebt,
                NetWorth = assetBalance - liabilityBalance,
                MonthlySpending = monthlySpending,
                ActiveAccountCount = accounts.Count,
                Accounts = accounts,
                RecentTransactions = recentTransactions,
                SpendingByCategory = spendingByCategory,

                CashFlowHistory = cashFlowHistory
            };

            return View(model);
        }
    }
}