using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VehicleRegistry.Application;
using VehicleRegistry.Application.Services;
using VehicleRegistry.Domain.Constants;
using VehicleRegistry.Web.Models.ServiceRecords;

namespace VehicleRegistry.Web.Controllers;

[Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
public sealed class ServiceRecordsController(
    IServiceHistoryService records,
    IVehicleService vehicles,
    ICatalogService catalog,
    ILogger<ServiceRecordsController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await records.AllAsync());

    [HttpGet]
    public async Task<IActionResult> Create(int? vehicleId)
    {
        var model = new ServiceRecordForm { VehicleId = vehicleId ?? 0 };
        if (vehicleId.HasValue && await vehicles.GetVisibleAsync(vehicleId.Value, "", true) is { } vehicle)
            model.MileageAtService = vehicle.Mileage;
        await OptionsAsync();
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceRecordForm model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await records.CreateAsync(model.ToInput(), User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "");
                TempData["Message"] = "Servisní úkon byl uložen.";
                return RedirectToAction("Details", "Vehicles", new { id = model.VehicleId });
            }
            catch (BusinessRuleException ex) { ModelState.AddModelError("", ex.Message); }
        }
        await OptionsAsync();
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var record = await records.GetAsync(id);
        if (record is null) return NotFound();
        await OptionsAsync();
        return View("Form", new ServiceRecordForm
        {
            Id = record.Id, VehicleId = record.VehicleId, ServiceTypeId = record.ServiceTypeId,
            ServiceCenterId = record.ServiceCenterId, ServiceDate = record.ServiceDate,
            MileageAtService = record.MileageAtService, Price = record.Price, Status = record.Status, Note = record.Note
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceRecordForm model)
    {
        if (id != model.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            try
            {
                await records.UpdateAsync(id, model.ToInput());
                TempData["Message"] = "Servisní úkon byl upraven.";
                return RedirectToAction("Details", "Vehicles", new { id = model.VehicleId });
            }
            catch (BusinessRuleException ex) { ModelState.AddModelError("", ex.Message); }
        }
        await OptionsAsync();
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await records.GetAsync(id);
        if (record is null) return NotFound();
        try
        {
            await records.DeleteAsync(id);
            TempData["Message"] = "Servisní záznam byl odstraněn.";
            return RedirectToAction("Details", "Vehicles", new { id = record.VehicleId });
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning(ex, "Could not delete service record {RecordId}", id);
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task OptionsAsync()
    {
        var allVehicles = await vehicles.GetAllAsync();
        ViewBag.Vehicles = new SelectList(allVehicles.Select(v => new
        {
            v.Id,
            Label = $"{v.LicensePlate} — {v.Manufacturer?.Name} {v.Model}"
        }), "Id", "Label");
        ViewBag.Types = new SelectList(await catalog.TypesAsync(), "Id", "Name");
        ViewBag.Centers = new SelectList(await catalog.CentersAsync(), "Id", "Name");
    }
}
