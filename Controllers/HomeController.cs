using System.Diagnostics;
using KABILIN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KABILIN.Controllers;

public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Index() => RedirectToAction("Login", "Account");

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
