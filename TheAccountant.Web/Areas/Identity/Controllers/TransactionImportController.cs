using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheAccountant.Interfaces;

namespace TheAccountant.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Route("Identity/[controller]/[action]")]
    [Authorize]
    public class TransactionImportController : Controller
    {
        private readonly ITransactionImportService _transactionImportService;

        public TransactionImportController(ITransactionImportService transactionImportService)
        {
            _transactionImportService = transactionImportService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a valid file.");
                return View();
            }

            using var stream = file.OpenReadStream();
            var transactions = await _transactionImportService.ImportAsync(stream, Path.GetExtension(file.FileName));

            TempData["Success"] = "Transactions imported successfully!";
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
