using Microsoft.AspNetCore.Identity;

namespace VehicleRegistry.Infrastructure.Identity;

/// <summary>
/// Czech texts for ASP.NET Core Identity errors shown to users.
/// Error codes stay the same as in the default describer.
/// </summary>
/// <remarks>
/// The application uses the e-mail as the user name, so a duplicate account
/// produces the same text for both checks and the UI shows it only once.
/// </remarks>
public sealed class CzechIdentityErrorDescriber : IdentityErrorDescriber
{
    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };

    public override IdentityError DefaultError() =>
        Error(nameof(DefaultError), "Došlo k neznámé chybě.");

    public override IdentityError ConcurrencyFailure() =>
        Error(nameof(ConcurrencyFailure), "Záznam mezitím změnil někdo jiný. Načtěte stránku znovu.");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "Nesprávné heslo.");

    public override IdentityError InvalidToken() =>
        Error(nameof(InvalidToken), "Neplatný nebo prošlý token.");

    public override IdentityError RecoveryCodeRedemptionFailed() =>
        Error(nameof(RecoveryCodeRedemptionFailed), "Obnovovací kód se nepodařilo použít.");

    public override IdentityError LoginAlreadyAssociated() =>
        Error(nameof(LoginAlreadyAssociated), "Toto přihlášení už je spojené s jiným účtem.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), $"Uživatelské jméno '{userName}' je neplatné.");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), $"E-mail '{email}' je neplatný.");

    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), $"Účet s e-mailem '{userName}' už existuje.");

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), $"Účet s e-mailem '{email}' už existuje.");

    public override IdentityError InvalidRoleName(string? role) =>
        Error(nameof(InvalidRoleName), $"Název role '{role}' je neplatný.");

    public override IdentityError DuplicateRoleName(string role) =>
        Error(nameof(DuplicateRoleName), $"Role '{role}' už existuje.");

    public override IdentityError UserAlreadyHasPassword() =>
        Error(nameof(UserAlreadyHasPassword), "Uživatel už má nastavené heslo.");

    public override IdentityError UserLockoutNotEnabled() =>
        Error(nameof(UserLockoutNotEnabled), "Pro tohoto uživatele není povolené zamykání účtu.");

    public override IdentityError UserAlreadyInRole(string role) =>
        Error(nameof(UserAlreadyInRole), $"Uživatel už má roli '{role}'.");

    public override IdentityError UserNotInRole(string role) =>
        Error(nameof(UserNotInRole), $"Uživatel nemá roli '{role}'.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"Heslo musí mít alespoň {length} znaků.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars), $"Heslo musí obsahovat alespoň {uniqueChars} různých znaků.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "Heslo musí obsahovat alespoň jeden speciální znak.");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "Heslo musí obsahovat alespoň jednu číslici (0–9).");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "Heslo musí obsahovat alespoň jedno malé písmeno (a–z).");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "Heslo musí obsahovat alespoň jedno velké písmeno (A–Z).");
}
