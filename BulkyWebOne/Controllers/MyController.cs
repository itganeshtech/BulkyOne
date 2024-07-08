using Microsoft.AspNetCore.Mvc;

namespace BulkyWebOne.Controllers
{
    public class MyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult BootPractice()
        {
            return View();
        }
    }
}
