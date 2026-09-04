using Microsoft.AspNetCore.Mvc;

namespace TheAccountant.Controllers
{
    public class Dashboard : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
