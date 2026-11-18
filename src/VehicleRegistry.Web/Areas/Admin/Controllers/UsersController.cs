using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleRegistry.Application.Services;
using VehicleRegistry.Domain.Constants;
using VehicleRegistry.Domain.Entities;
using VehicleRegistry.Web.Areas.Admin.Models;

namespace VehicleRegistry.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class UsersController(
    UserManager<ApplicationUser> users, IVehicleService vehicles) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var accounts = await users.Users.OrderBy(x => x.Email).ToListAsync();
        var list = new List<UserListRow>();
        foreach (var u in accounts)
        {
            var roles = await users.GetRolesAsync(u);
            list.Add(new UserListRow(u.Id, u.Email ?? "", $"{u.FirstName} {u.LastName}", string.Join(", ", roles)));
        }
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new UserEditForm());

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        var currentRoles = await users.GetRolesAsync(user);
        return View("Form", new UserEditForm
        {
            Id = user.Id, Email = user.Email ?? "", FirstName = user.FirstName,
            LastName = user.LastName, Role = currentRoles.FirstOrDefault() ?? Roles.Customer
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserEditForm model)
    {
        if (!Roles.All.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Neplatná role.");
        if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 8)
            ModelState.AddModelError(nameof(model.Password), "Heslo musí mít alespoň 8 znaků.");
        if (!ModelState.IsValid) return View("Form", model);

        var user = new ApplicationUser
        {
            FirstName = model.FirstName.Trim(), LastName = model.LastName.Trim(),
            Email = model.Email.Trim(), UserName = model.Email.Trim()
        };
        var result = await users.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View("Form", model);
        }
        var roleResult = await users.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(user);
            AddErrors(roleResult);
            return View("Form", model);
        }
        TempData["Message"] = "Účet byl vytvořen.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditForm model)
    {
        if (id != model.Id) return BadRequest();
        if (!Roles.All.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Neplatná role.");
        if (!ModelState.IsValid) return View("Form", model);
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        var oldRoles = await users.GetRolesAsync(user);
        if (oldRoles.Contains(Roles.Admin) && model.Role != Roles.Admin &&
            (id == users.GetUserId(User) || (await users.GetUsersInRoleAsync(Roles.Admin)).Count <= 1))
        {
            ModelState.AddModelError(nameof(model.Role), "Nelze odebrat roli aktuálnímu nebo poslednímu administrátorovi.");
            return View("Form", model);
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        if (!string.Equals(user.Email, model.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var emailChange = await users.SetEmailAsync(user, model.Email.Trim());
            if (!emailChange.Succeeded) { AddErrors(emailChange); return View("Form", model); }
            var nameChange = await users.SetUserNameAsync(user, model.Email.Trim());
            if (!nameChange.Succeeded) { AddErrors(nameChange); return View("Form", model); }
        }
        var update = await users.UpdateAsync(user);
        if (!update.Succeeded) { AddErrors(update); return View("Form", model); }
        var remove = await users.RemoveFromRolesAsync(user, oldRoles.Where(r => r != model.Role));
        if (!remove.Succeeded) { AddErrors(remove); return View("Form", model); }
        if (!await users.IsInRoleAsync(user, model.Role))
        {
            var add = await users.AddToRoleAsync(user, model.Role);
            if (!add.Succeeded) { AddErrors(add); return View("Form", model); }
        }
        TempData["Message"] = "Uživatel byl upraven.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == users.GetUserId(User)) return BadRequest("Nelze odstranit vlastní účet.");
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        if ((await vehicles.GetMineAsync(id)).Count > 0)
        {
            TempData["Error"] = "Uživatel vlastní vozidla; nejprve je převeďte nebo smažte.";
            return RedirectToAction(nameof(Index));
        }
        if (await users.IsInRoleAsync(user, Roles.Admin) && (await users.GetUsersInRoleAsync(Roles.Admin)).Count <= 1)
            return BadRequest("Posledního administrátora nelze smazat.");
        var result = await users.DeleteAsync(user);
        TempData[result.Succeeded ? "Message" : "Error"] = result.Succeeded
            ? "Uživatel byl smazán." : "Uživatele se nepodařilo smazat.";
        return RedirectToAction(nameof(Index));
    }

    private void AddErrors(IdentityResult result)
    {
        // E-mail is also the user name, so a duplicate account reports the same text twice.
        foreach (var message in result.Errors.Select(e => e.Description).Distinct())
            ModelState.AddModelError(string.Empty, message);
    }
}
