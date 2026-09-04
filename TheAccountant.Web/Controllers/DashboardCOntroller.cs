using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheAccountant.Web.Data;
using TheAccountant.Web.Models;
using TheAccountant.Web.Models.Enums;
using TheAccountant.Web.Models.ViewModels;
using TheAccountant.Web.Services;

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

            //
            // ACCOUNTS
            //
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

            //
            // DATE RANGES
            //
            var monthStart = new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

            var nextMonthStart = monthStart.AddMonths(1);

            var cashFlowStart = monthStart.AddMonths(-5);

            //
            // ANALYTICS TRANSACTIONS
            //
            // Load six months of posted transactions once.
            // Classification happens in memory because the classifier
            // contains business logic that should not be translated to SQL.
            //
            var analyticsTransactions = await _context.Transactions
                .AsNoTracking()
                .Where(t =>
                    t.Account.UserId == userId &&
                    t.Date >= cashFlowStart &&
                    t.Date < nextMonthStart &&
                    !t.IsPending)
                .ToListAsync();

            var classifiedTransactions = analyticsTransactions
                .Select(t => new
                {
                    Transaction = t,
                    Kind = TransactionClassifier.Classify(t)
                })
                .ToList();

            //
            // CASH FLOW HISTORY
            //
            var cashFlowHistory =
                new List<MonthlyCashFlowViewModel>();

            for (var i = 0; i < 6; i++)
            {
                var currentMonth =
                    cashFlowStart.AddMonths(i);

                var followingMonth =
                    currentMonth.AddMonths(1);

                var transactionsForMonth =
                    classifiedTransactions
                        .Where(x =>
                            x.Transaction.Date >= currentMonth &&
                            x.Transaction.Date < followingMonth)
                        .ToList();

                var income = transactionsForMonth
                    .Where(x =>
                        x.Kind == CashFlowKind.Income)
                    .Sum(x => x.Transaction.Amount);

                var spending = transactionsForMonth
                    .Where(x =>
                        x.Kind == CashFlowKind.Spending)
                    .Sum(x =>
                        Math.Abs(x.Transaction.Amount));

                cashFlowHistory.Add(
                    new MonthlyCashFlowViewModel
                    {
                        Month = currentMonth.ToString("MMM"),
                        Income = income,
                        Spending = spending
                    });
            }

            //
            // MONTHLY SPENDING
            //
            // Only transactions classified as actual spending.
            // Transfers, debt payments, investments and financing
            // are excluded.
            //
            var monthlySpending =
                classifiedTransactions
                    .Where(x =>
                        x.Transaction.Date >= monthStart &&
                        x.Transaction.Date < nextMonthStart &&
                        x.Kind == CashFlowKind.Spending)
                    .Sum(x =>
                        Math.Abs(x.Transaction.Amount));

            //
            // RECENT TRANSACTIONS
            //
            // Includes pending transactions because the user should
            // still be able to see recent account activity.
            //
            var recentTransactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Account)
                .Where(t =>
                    t.Account.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .Take(5)
                .ToListAsync();

            //
            // SPENDING BY CATEGORY
            //
            // Only actual spending is included here.
            //
            var spendingByCategory =
                classifiedTransactions
                    .Where(x =>
                        x.Transaction.Date >= monthStart &&
                        x.Transaction.Date < nextMonthStart &&
                        x.Kind == CashFlowKind.Spending)
                    .GroupBy(x =>
                        string.IsNullOrWhiteSpace(
                            x.Transaction.Category)
                            ? "Uncategorized"
                            : x.Transaction.Category!)
                    .Select(g =>
                        new CategorySpendViewModel
                        {
                            Category = g.Key,

                            Amount = g.Sum(x =>
                                Math.Abs(
                                    x.Transaction.Amount))
                        })
                    .OrderByDescending(x => x.Amount)
                    .ToList();

            //
            // DASHBOARD
            //
            var model = new DashboardViewModel
            {
                CashBalance = cashBalance,
                CreditDebt = creditDebt,
                NetWorth =
                    assetBalance - liabilityBalance,

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