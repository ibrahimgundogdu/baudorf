using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Baudorf.Web.Areas.Identity.Pages.Account.Manage;

public class EnableAuthenticatorModel(
    UserManager<ApplicationUser> userManager,
    ILogger<EnableAuthenticatorModel> logger,
    UrlEncoder urlEncoder) : PageModel
{
    // Aussteller "Baudorf Immobilien" — so erscheint der Eintrag in der Authenticator-App.
    private const string Issuer = "Baudorf Immobilien";
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    public string? SharedKey { get; set; }
    public string? AuthenticatorUri { get; set; }

    [TempData] public string[]? RecoveryCodes { get; set; }
    [TempData] public string? StatusMessage { get; set; }

    [BindProperty] public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte geben Sie den Bestätigungscode ein.")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "Der Code besteht aus 6 Ziffern.")]
        [DataType(DataType.Text)]
        [Display(Name = "Bestätigungscode")]
        public string Code { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return NotFound();
        await LoadSharedKeyAndQrCodeUriAsync(user);
        return Page();
    }

    /// <summary>
    /// Authenticator zurücksetzen: neuen Schlüssel erzeugen und 2FA deaktivieren.
    /// Rettungsanker, falls die App-Kopplung nicht mehr passt (verlorenes Gerät,
    /// Uhr-Drift, falscher Eintrag) — der Nutzer richtet danach frisch ein.
    /// </summary>
    public async Task<IActionResult> OnPostResetKeyAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        logger.LogInformation("Authenticator-Schlüssel wurde zurückgesetzt.");

        StatusMessage = "Der Authenticator wurde zurückgesetzt. Scannen Sie den neuen QR-Code und bestätigen Sie einen frischen Code.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadSharedKeyAndQrCodeUriAsync(user);
            return Page();
        }

        var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var is2faTokenValid = await userManager.VerifyTwoFactorTokenAsync(
            user, userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!is2faTokenValid)
        {
            ModelState.AddModelError("Input.Code", "Der Bestätigungscode ist ungültig. Bitte erneut versuchen.");
            await LoadSharedKeyAndQrCodeUriAsync(user);
            return Page();
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        logger.LogInformation("Zwei-Faktor-Authentifizierung aktiviert.");
        StatusMessage = "Ihre Authenticator-App ist verifiziert — die Zwei-Faktor-Authentifizierung ist jetzt aktiv.";

        if (await userManager.CountRecoveryCodesAsync(user) == 0)
        {
            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            RecoveryCodes = recoveryCodes!.ToArray();
            return RedirectToPage("./ShowRecoveryCodes");
        }

        return RedirectToPage("./TwoFactorAuthentication");
    }

    private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
    {
        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKey = FormatKey(unformattedKey!);
        var email = await userManager.GetEmailAsync(user);
        AuthenticatorUri = GenerateQrCodeUri(email!, unformattedKey!);
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var position = 0;
        while (position + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(position, 4)).Append(' ');
            position += 4;
        }
        if (position < unformattedKey.Length)
            result.Append(unformattedKey.AsSpan(position));
        return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string email, string unformattedKey) =>
        string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            urlEncoder.Encode(Issuer),
            urlEncoder.Encode(email),
            unformattedKey);
}
