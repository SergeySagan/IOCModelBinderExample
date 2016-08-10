using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IOCModelBinderExample.ViewModels;

namespace IOCModelBinderExample.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(HomeViewModel model)
        {
            return View(model);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
