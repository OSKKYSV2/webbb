using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleRegistry.Domain.Constants;

namespace VehicleRegistry.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
