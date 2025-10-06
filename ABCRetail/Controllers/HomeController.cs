using System.Diagnostics;
using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
