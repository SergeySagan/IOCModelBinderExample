using IOCModelBinderExample.ViewModels;
using Microsoft.AspNet.Mvc;

namespace IOCModelBinderExample.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(HomeViewModel model)
        {
            return View(model);
        }

        public IActionResult Test(NonIOCViewModel model)
        {
            return View("Index", model);
        }
    }
}