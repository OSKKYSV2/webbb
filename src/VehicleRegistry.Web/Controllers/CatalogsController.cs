using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleRegistry.Application;
using VehicleRegistry.Application.Services;
using VehicleRegistry.Domain.Constants;
using VehicleRegistry.Web.Models.Catalogs;

namespace VehicleRegistry.Web.Controllers;

[Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
public sealed class CatalogsController(ICatalogService catalog) : Controller
{
    private bool CanAccess(string kind) => kind switch
    {
        "manufacturers" => User.IsInRole(Roles.Admin),
        "types" or "centers" => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager),
        _ => false
    };

    [HttpGet]
    public async Task<IActionResult> Index(string kind = "types")
    {
        if (!CanAccess(kind)) return Forbid();
        var list = kind switch
        {
            "manufacturers" => new CatalogList(kind, "Výrobci", (await catalog.ManufacturersAsync())
                .Select(m => new CatalogRow(m.Id, m.Name, m.Country ?? "")).ToList()),
            "types" => new CatalogList(kind, "Druhy servisních úkonů", (await catalog.TypesAsync())
                .Select(t => new CatalogRow(t.Id, t.Name, t.Description ?? "")).ToList()),
            _ => new CatalogList(kind, "Servisy", (await catalog.CentersAsync())
                .Select(c => new CatalogRow(c.Id, c.Name, c.Address)).ToList())
        };
        return View(list);
    }

    [HttpGet]
    public IActionResult Create(string kind = "types") => CanAccess(kind)
        ? View("Form", new CatalogForm { Kind = kind }) : Forbid();

    [HttpGet]
    public async Task<IActionResult> Edit(string kind, int id)
    {
        if (!CanAccess(kind)) return Forbid();
        CatalogForm? model = null;
        switch (kind)
        {
            case "manufacturers":
                var m = await catalog.ManufacturerAsync(id);
                if (m is not null) model = new CatalogForm { Kind = kind, Id = m.Id, Name = m.Name, Country = m.Country };
                break;
            case "types":
                var t = await catalog.TypeAsync(id);
                if (t is not null) model = new CatalogForm { Kind = kind, Id = t.Id, Name = t.Name, Description = t.Description,
                    RecommendedIntervalKm = t.RecommendedIntervalKm, RecommendedIntervalMonths = t.RecommendedIntervalMonths };
                break;
            case "centers":
                var c = await catalog.CenterAsync(id);
                if (c is not null) model = new CatalogForm { Kind = kind, Id = c.Id, Name = c.Name, Address = c.Address,
                    Phone = c.Phone, Email = c.Email };
                break;
        }
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(CatalogForm model) => Save(model, false);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, CatalogForm model) =>
        id == model.Id ? Save(model, true) : Task.FromResult<IActionResult>(BadRequest());

    private async Task<IActionResult> Save(CatalogForm model, bool edit)
    {
        if (!CanAccess(model.Kind)) return Forbid();
        if (ModelState.IsValid)
        {
            try
            {
                int? id = edit ? model.Id : null;
                switch (model.Kind)
                {
                    case "manufacturers": await catalog.SaveManufacturerAsync(id, model.Name, model.Country); break;
                    case "types": await catalog.SaveTypeAsync(id, model.Name, model.Description, model.RecommendedIntervalKm, model.RecommendedIntervalMonths); break;
                    case "centers": await catalog.SaveCenterAsync(id, model.Name, model.Address ?? "", model.Phone, model.Email); break;
                }
                TempData["Message"] = "Záznam byl uložen.";
                return RedirectToAction(nameof(Index), new { kind = model.Kind });
            }
            catch (BusinessRuleException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        }
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string kind, int id)
    {
        if (!CanAccess(kind)) return Forbid();
        try
        {
            switch (kind)
            {
                case "manufacturers": await catalog.DeleteManufacturerAsync(id); break;
                case "types": await catalog.DeleteTypeAsync(id); break;
                case "centers": await catalog.DeleteCenterAsync(id); break;
            }
            TempData["Message"] = "Záznam byl smazán.";
        }
        catch (BusinessRuleException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { kind });
    }
}
