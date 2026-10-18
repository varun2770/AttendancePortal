using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AttendancePortal.Models;

namespace AttendancePortal.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            if (User.IsInRole(Roles.Admin)) return RedirectToAction("Index", "Admin");
            if (User.IsInRole(Roles.Faculty)) return RedirectToAction("Index", "Faculty");
            if (User.IsInRole(Roles.Student)) return RedirectToAction("Index", "Student");
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}