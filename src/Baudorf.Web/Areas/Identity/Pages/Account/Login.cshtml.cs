using System.ComponentModel.DataAnnotations;
using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Baudorf.Web.Areas.Identity.Pages.Account;

[EnableRateLimiting("login")]
public class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    ITurnstileVerifier turnstile,
    IOptions<TurnstileOptions> turnstileOptions,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    /// <summary>Site-Key fürs Turnstile-Widget; null = CAPTCHA deaktiviert.</summary>
    public string? TurnstileSiteKey => turnstileOptions.Value.Enabled ? turnstileOptions.Value.SiteKey : null;

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte geben Sie Ihre E-Mail-Adresse ein.")]
        [EmailAddress(ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse ein.")]
        [Display(Name = "E-Mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte geben Sie Ihr Passwort ein.")]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Angemeldet bleiben")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        // Vorhandene externe Cookies entfernen, damit ein sauberer Login-Prozess gewährleistet ist.
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // CAPTCHA (nur wenn konfiguriert) — automatisierte Anmeldeversuche aussperren.
        if (turnstile.Enabled)
        {
            var token = Request.Form["cf-turnstile-response"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!await turnstile.VerifyAsync(token, ip))
            {
                ModelState.AddModelError(string.Empty, "Die Sicherheitsprüfung ist fehlgeschlagen. Bitte versuchen Sie es erneut.");
                return Page();
            }
        }

        // lockoutOnFailure: true → nach 5 Fehlversuchen wird das Konto gesperrt.
        var result = await signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("Benutzer hat sich angemeldet.");
            return LocalRedirect(returnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, Input.RememberMe });
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("Benutzerkonto gesperrt (zu viele Fehlversuche).");
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(string.Empty, "Ungültige Anmeldedaten. Bitte überprüfen Sie E-Mail und Passwort.");
        return Page();
    }
}
