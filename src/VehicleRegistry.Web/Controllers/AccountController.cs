using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VehicleRegistry.Domain.Constants;
using VehicleRegistry.Domain.Entities;
using VehicleRegistry.Web.Models.Account;

namespace VehicleRegistry.Web.Controllers;

public sealed class AccountController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public IActionResult Register() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Vehicles") : View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim()
        };
        var result = await users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        var roleResult = await users.AddToRoleAsync(user, Roles.Customer);
        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(user);
            logger.LogError("Could not assign Customer role during registration");
            ModelState.AddModelError(string.Empty, "Registraci se nepodařilo dokončit.");
            return View(model);
        }
        await signIn.SignInAsync(user, isPersistent: false);
        logger.LogInformation("New customer registered: {UserId}", user.Id);
        return RedirectToAction("Index", "Vehicles");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Vehicles");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        if (user is not null)
        {
            var result = await signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                logger.LogInformation("User signed in: {UserId}", user.Id);
                return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                    ? LocalRedirect(model.ReturnUrl) : RedirectToAction("Index", "Vehicles");
            }
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Účet je dočasně uzamčen, zkuste to později.");
                return View(model);
            }
        }
        ModelState.AddModelError(string.Empty, "Nesprávný e-mail nebo heslo.");
        return View(model);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Denied() => View();
}
