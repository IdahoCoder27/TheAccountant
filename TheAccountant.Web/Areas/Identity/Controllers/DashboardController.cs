using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheAccountant.Interfaces;
using TheAccountant.Web.Data;
using TheAccountant.Web.Models.ViewModels;

namespace TheAccountant.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Route("Identity/[controller]/[action]")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var transactions = await _context.Transactions
                .Include(t => t.Account)
                .Where(t => t.Account.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Take(5)
                .ToListAsync();

            var recurringPayments = await _context.RecurringPayments
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.NextDue)
                .ToListAsync();

            var vm = new DashboardViewModel
            {
                Accounts = accounts,
                Transactions = transactions,
                RecurringPayments = recurringPayments
            };

            return View(vm);
        }

        [HttpPost("import-transactions")]
        public async Task<IActionResult> ImportTransactions(IFormFile file, [FromServices] ITransactionImportService importService)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file.");

            var transactions = await importService.ImportAsync(file.OpenReadStream(), Path.GetExtension(file.FileName));

            // TODO: Save transactions to DB or return for preview
            return Ok(transactions);
        }
    }
}
