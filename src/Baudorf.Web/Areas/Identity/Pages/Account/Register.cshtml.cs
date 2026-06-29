using System.ComponentModel.DataAnnotations;
using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Baudorf.Web.Areas.Identity.Pages.Account;

public class RegisterModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITurnstileVerifier turnstile,
    IOptions<TurnstileOptions> turnstileOptions,
    ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    /// <summary>Site-Key für das Turnstile-Widget; leer = CAPTCHA deaktiviert.</summary>
    public string? TurnstileSiteKey => turnstileOptions.Value.Enabled ? turnstileOptions.Value.SiteKey : null;

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte geben Sie Ihren Vornamen ein.")]
        [Display(Name = "Vorname")]
        [StringLength(80)]
        public string Vorname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte geben Sie Ihren Nachnamen ein.")]
        [Display(Name = "Nachname")]
        [StringLength(80)]
        public string Nachname { get; set; } = string.Empty;

        [Display(Name = "Firma (optional)")]
        [StringLength(160)]
        public string? Firma { get; set; }

        [Required(ErrorMessage = "Bitte geben Sie Ihre E-Mail-Adresse ein.")]
        [EmailAddress(ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse ein.")]
        [Display(Name = "E-Mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte geben Sie Ihre Telefonnummer ein.")]
        [Phone(ErrorMessage = "Bitte geben Sie eine gültige Telefonnummer ein.")]
        [Display(Name = "Telefon")]
        public string Telefon { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte wählen Sie Ihren Investorentyp.")]
        [Display(Name = "Investorentyp")]
        public string Investorentyp { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte geben Sie Ihren Beruf an.")]
        [StringLength(120)]
        [Display(Name = "Beruf")]
        public string Beruf { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte teilen Sie uns kurz mit, warum Sie sich registrieren möchten.")]
        [StringLength(2000)]
        [Display(Name = "Warum möchten Sie sich registrieren?")]
        public string Registrierungsgrund { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte geben Sie ein Passwort ein.")]
        [StringLength(100, ErrorMessage = "Das {0} muss mindestens {2} und höchstens {1} Zeichen lang sein.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Passwort bestätigen")]
        [Compare(nameof(Password), ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Bitte akzeptieren Sie die AGB und Datenschutzerklärung.")]
        [Display(Name = "AGB & Datenschutz akzeptieren")]
        public bool AgbAkzeptiert { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ReturnUrl = returnUrl;

        // CAPTCHA (nur wenn konfiguriert).
        if (turnstile.Enabled)
        {
            var token = Request.Form["cf-turnstile-response"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!await turnstile.VerifyAsync(token, ip))
            {
                ModelState.AddModelError(string.Empty, "Die Sicherheitsprüfung ist fehlgeschlagen. Bitte versuchen Sie es erneut.");
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            PhoneNumber = Input.Telefon.Trim(),
            AnzeigeName = $"{Input.Vorname.Trim()} {Input.Nachname.Trim()}".Trim(),
            Firma = string.IsNullOrWhiteSpace(Input.Firma) ? null : Input.Firma.Trim(),
            Beruf = Input.Beruf.Trim(),
            Investorentyp = Input.Investorentyp.Trim(),
            Registrierungsgrund = Input.Registrierungsgrund.Trim(),
            AgbAkzeptiertAm = DateTime.UtcNow,
            IstFreigegeben = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, Roles.Investor);
            logger.LogInformation("Neuer Investor registriert: {Email} (wartet auf Freigabe).", user.Email);

            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToPage("./RegisterConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}
