using System.ComponentModel.DataAnnotations;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Baudorf.Web.Areas.Identity.Pages.Account;

/// <summary>
/// Zweiter Faktor per E-Mail: nach korrektem Passwort wird ein 6-stelliger Code an die
/// hinterlegte E-Mail gesendet. Unabhängig von der Server-/Geräteuhr (kein TOTP-Abgleich).
/// </summary>
public class LoginWithEmailCodeModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IEmailService email,
    ISiteSettings settings,
    ILogger<LoginWithEmailCodeModel> logger) : PageModel
{
    private static readonly string Provider = TokenOptions.DefaultEmailProvider;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }

    /// <summary>Teil-maskierte Zieladresse für die Anzeige (z. B. w••@baudorf.de).</summary>
    public string MaskedEmail { get; set; } = string.Empty;

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte geben Sie den per E-Mail erhaltenen Code ein.")]
        [StringLength(8, MinimumLength = 6, ErrorMessage = "Der Code besteht aus 6 Ziffern.")]
        [DataType(DataType.Text)]
        [Display(Name = "E-Mail-Code")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Diesen Browser merken")]
        public bool RememberMachine { get; set; }
    }

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return email;
        var name = email[..at];
        var shown = name.Length <= 2 ? name[..1] : name[..2];
        return shown + new string('•', Math.Max(2, name.Length - shown.Length)) + email[at..];
    }

    /// <summary>Sendet den Code. Gibt false zurück (statt zu werfen), wenn der Versand scheitert —
    /// so entsteht nie eine 500-Fehlerseite, der Nutzer sieht eine verständliche Meldung.</summary>
    private async Task<bool> SendCodeAsync(ApplicationUser user)
    {
        var to = await userManager.GetEmailAsync(user);
        if (string.IsNullOrWhiteSpace(to)) return false;

        try
        {
            var code = await userManager.GenerateTwoFactorTokenAsync(user, Provider);
            var portal = settings.Get("site.name", "Baudorf Immobilien");
            var body = $"""
                <p>Guten Tag,</p>
                <p>Ihr Anmeldecode für das {portal}-Portal lautet:</p>
                <p style="font-size:28px;font-weight:700;letter-spacing:.3em;margin:18px 0">{code}</p>
                <p>Der Code ist wenige Minuten gültig. Falls Sie sich nicht angemeldet haben,
                können Sie diese E-Mail ignorieren.</p>
                """;
            await email.SendAsync(to, "Ihr Anmeldecode", body);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "2FA-Code-Versand an {Email} fehlgeschlagen.", to);
            return false;
        }
    }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToPage("./Login");

        ReturnUrl = returnUrl;
        RememberMe = rememberMe;
        MaskedEmail = Mask(await userManager.GetEmailAsync(user) ?? string.Empty);

        if (await SendCodeAsync(user))
            logger.LogInformation("2FA-E-Mail-Code an Nutzer {UserId} gesendet.", user.Id);
        else
            ModelState.AddModelError(string.Empty,
                "Der Anmeldecode konnte gerade nicht per E-Mail versendet werden. " +
                "Bitte versuchen Sie es erneut oder wenden Sie sich an den Administrator.");
        return Page();
    }

    public async Task<IActionResult> OnPostResendAsync(bool rememberMe, string? returnUrl = null)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToPage("./Login");

        ReturnUrl = returnUrl;
        RememberMe = rememberMe;
        MaskedEmail = Mask(await userManager.GetEmailAsync(user) ?? string.Empty);

        ModelState.AddModelError(string.Empty, await SendCodeAsync(user)
            ? "Ein neuer Code wurde gesendet."
            : "Der Code konnte nicht versendet werden. Bitte später erneut versuchen.");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool rememberMe, string? returnUrl = null)
    {
        RememberMe = rememberMe;
        ReturnUrl = returnUrl;
        returnUrl ??= Url.Content("~/");

        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToPage("./Login");
        MaskedEmail = Mask(await userManager.GetEmailAsync(user) ?? string.Empty);

        if (!ModelState.IsValid) return Page();

        var code = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await signInManager.TwoFactorSignInAsync(Provider, code, rememberMe, Input.RememberMachine);

        if (result.Succeeded)
        {
            logger.LogInformation("Anmeldung per E-Mail-Code erfolgreich (Nutzer {UserId}).", user.Id);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("Konto gesperrt (2FA-E-Mail).");
            return RedirectToPage("./Lockout");
        }

        logger.LogWarning("Ungültiger E-Mail-Code eingegeben.");
        ModelState.AddModelError(string.Empty, "Ungültiger oder abgelaufener Code. Bitte fordern Sie ggf. einen neuen an.");
        return Page();
    }
}
