using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VehicleRegistry.Web.Models;

namespace VehicleRegistry.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
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

    /// <summary>Readable page for empty 4xx responses (UseStatusCodePagesWithReExecute keeps the status code).</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int id)
    {
        // Model binding is skipped when the failed request's form is unreadable (e.g. a too large upload),
        // so the original code comes from the re-execute feature first.
        var code = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalStatusCode ?? id;
        return View(code);
    }
}
