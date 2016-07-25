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
    }
}