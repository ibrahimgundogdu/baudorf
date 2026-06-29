using Microsoft.AspNetCore.Identity;

namespace Baudorf.Web.Services;

/// <summary>
/// Deutsche Übersetzung der Identity-Fehlermeldungen (Passwort, doppelte E-Mail usw.).
/// Registriert via <c>.AddErrorDescriber&lt;GermanIdentityErrorDescriber&gt;()</c>.
/// </summary>
public class GermanIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = "Ein unbekannter Fehler ist aufgetreten." };

    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = $"Die E-Mail-Adresse „{email}“ ist bereits vergeben." };

    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = $"Der Benutzername „{userName}“ ist bereits vergeben." };

    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = $"Die E-Mail-Adresse „{email}“ ist ungültig." };

    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = $"Der Benutzername „{userName}“ ist ungültig." };

    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "Falsches Passwort." };

    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Das Passwort muss mindestens eine Ziffer (0–9) enthalten." };

    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "Das Passwort muss mindestens einen Kleinbuchstaben (a–z) enthalten." };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Das Passwort muss mindestens ein Sonderzeichen enthalten." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Das Passwort muss mindestens {uniqueChars} unterschiedliche Zeichen enthalten." };

    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "Das Passwort muss mindestens einen Großbuchstaben (A–Z) enthalten." };

    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"Das Passwort muss mindestens {length} Zeichen lang sein." };

    public override IdentityError UserAlreadyHasPassword() => new() { Code = nameof(UserAlreadyHasPassword), Description = "Der Benutzer hat bereits ein Passwort." };

    public override IdentityError UserNotInRole(string role) => new() { Code = nameof(UserNotInRole), Description = $"Der Benutzer ist nicht in der Rolle „{role}“." };

    public override IdentityError UserAlreadyInRole(string role) => new() { Code = nameof(UserAlreadyInRole), Description = $"Der Benutzer ist bereits in der Rolle „{role}“." };

    public override IdentityError InvalidToken() => new() { Code = nameof(InvalidToken), Description = "Ungültiger Token." };

    public override IdentityError ConcurrencyFailure() => new() { Code = nameof(ConcurrencyFailure), Description = "Optimistischer Nebenläufigkeitsfehler, das Objekt wurde geändert." };
}
