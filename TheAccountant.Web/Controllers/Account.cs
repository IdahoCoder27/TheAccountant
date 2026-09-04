using Microsoft.AspNetCore.Mvc;

namespace TheAccountant.Controllers
{
    public class Account : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
