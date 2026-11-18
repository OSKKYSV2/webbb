using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VehicleRegistry.Application;
using VehicleRegistry.Application.Services;
using VehicleRegistry.Domain.Constants;
using VehicleRegistry.Domain.Entities;
using VehicleRegistry.Web.Models.Vehicles;

namespace VehicleRegistry.Web.Controllers;

[Authorize]
public sealed class VehiclesController(
    IVehicleService vehicles,
    ICatalogService catalog,
    IRecommendationService recommendations,
    UserManager<ApplicationUser> users,
    IWebHostEnvironment hosting,
    ILogger<VehiclesController> logger) : Controller
{
    private string MyId => users.GetUserId(User) ?? "";
    private bool IsStaff => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager);
    private bool IsAdmin => User.IsInRole(Roles.Admin);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = IsStaff ? await vehicles.GetAllAsync() : await vehicles.GetMineAsync(MyId);
        return View(items);
    }

    [Authorize(Roles = Roles.Admin + "," + Roles.Manager), HttpGet]
    public async Task<IActionResult> Search(string? q)
    {
        ViewBag.Query = q;
        return View("Index", string.IsNullOrWhiteSpace(q) ? await vehicles.GetAllAsync() : await vehicles.SearchAsync(q));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vehicle = await vehicles.GetVisibleAsync(id, MyId, IsStaff);
        if (vehicle is null) return NotFound();
        return View(new VehicleDetail(vehicle, await recommendations.ForVehicleAsync(id),
            IsAdmin || vehicle.OwnerId == MyId && !User.IsInRole(Roles.Manager), IsStaff));
    }

    [Authorize(Roles = Roles.Admin + "," + Roles.Customer), HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadOptionsAsync();
        return View("Form", new VehicleForm());
    }

    [Authorize(Roles = Roles.Admin + "," + Roles.Customer), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleForm model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var ownerId = IsAdmin && !string.IsNullOrEmpty(model.OwnerId) ? model.OwnerId : MyId;
                if (await users.FindByIdAsync(ownerId) is null)
                    throw new BusinessRuleException("Zvolený vlastník neexistuje.");
                var vehicle = await vehicles.CreateAsync(model.ToInput(), ownerId);
                TempData["Message"] = "Vozidlo bylo přidáno.";
                return RedirectToAction(nameof(Details), new { id = vehicle.Id });
            }
            catch (BusinessRuleException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        }
        await LoadOptionsAsync();
        return View("Form", model);
    }

    [Authorize(Roles = Roles.Admin + "," + Roles.Customer), HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vehicle = await vehicles.GetVisibleAsync(id, MyId, IsAdmin);
        if (vehicle is null) return NotFound();
        await LoadOptionsAsync();
        return View("Form", new VehicleForm
        {
            Id = vehicle.Id, Vin = vehicle.Vin, LicensePlate = vehicle.LicensePlate,
            VehicleModel = vehicle.Model, ManufacturerId = vehicle.ManufacturerId,
            YearOfManufacture = vehicle.YearOfManufacture, FuelType = vehicle.FuelType,
            Mileage = vehicle.Mileage, Color = vehicle.Color, OwnerId = vehicle.OwnerId
        });
    }

    [Authorize(Roles = Roles.Admin + "," + Roles.Customer), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VehicleForm model)
    {
        if (id != model.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            try
            {
                await vehicles.UpdateAsync(id, model.ToInput(), MyId, IsAdmin);
                TempData["Message"] = "Změny vozidla byly uloženy.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (BusinessRuleException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        }
        await LoadOptionsAsync();
        return View("Form", model);
    }

    [Authorize(Roles = Roles.Admin + "," + Roles.Customer), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var old = await vehicles.GetVisibleAsync(id, MyId, IsAdmin);
            if (old is null) return NotFound();
            await vehicles.DeleteAsync(id, MyId, IsAdmin);
            DeleteLocalPhoto(old.ImagePath);
            TempData["Message"] = "Vozidlo bylo odstraněno.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleException) { return NotFound(); }
    }

    [Authorize(Roles = Roles.Admin + "," + Roles.Customer)]
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile? photo)
    {
        var vehicle = await vehicles.GetVisibleAsync(id, MyId, IsAdmin);
        if (vehicle is null) return NotFound();
        if (photo is null || photo.Length is <= 0 or > 2 * 1024 * 1024)
        {
            TempData["Error"] = "Obrázek musí mít maximálně 2 MB.";
            return RedirectToAction(nameof(Details), new { id });
        }
        // Check the actual file signature, not just user-controlled ContentType or extension.
        var magic = new byte[8];
        using (var stream = photo.OpenReadStream())
        {
            if (await stream.ReadAsync(magic) < 8)
            {
                TempData["Error"] = "Neplatný soubor.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        var extension = magic[0] == 0xFF && magic[1] == 0xD8 && magic[2] == 0xFF ? ".jpg"
            : magic.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png" : null;
        if (extension is null)
        {
            TempData["Error"] = "Povoleny jsou pouze JPEG a PNG fotografie.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var folder = Path.Combine(hosting.WebRootPath, "uploads", "vehicles");
        Directory.CreateDirectory(folder);
        var filename = $"{Guid.NewGuid():N}{extension}";
        var physical = Path.Combine(folder, filename);
        try
        {
            await using (var dest = System.IO.File.Create(physical)) await photo.CopyToAsync(dest);
            var oldPath = vehicle.ImagePath;
            await vehicles.UpdatePhotoAsync(id, $"/uploads/vehicles/{filename}", MyId, IsAdmin);
            DeleteLocalPhoto(oldPath);
            TempData["Message"] = "Fotografie byla nahrána.";
        }
        catch (Exception ex)
        {
            System.IO.File.Delete(physical);
            logger.LogError(ex, "Vehicle photo upload failed for {VehicleId}", id);
            TempData["Error"] = "Nahrání fotografie se nepodařilo.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private void DeleteLocalPhoto(string? path)
    {
        if (path is null || !path.StartsWith("/uploads/vehicles/", StringComparison.Ordinal)) return;
        var filename = Path.GetFileName(path);
        var physical = Path.Combine(hosting.WebRootPath, "uploads", "vehicles", filename);
        if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
    }

    private async Task LoadOptionsAsync()
    {
        ViewBag.Manufacturers = new SelectList(await catalog.ManufacturersAsync(), "Id", "Name");
        if (IsAdmin)
            ViewBag.Owners = new SelectList(users.Users.OrderBy(u => u.Email).ToList(), "Id", "Email");
    }
}
