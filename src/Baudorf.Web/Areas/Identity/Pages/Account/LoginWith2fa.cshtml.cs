using System.ComponentModel.DataAnnotations;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Baudorf.Web.Areas.Identity.Pages.Account;

public class LoginWith2faModel(
    SignInManager<ApplicationUser> signInManager,
    ILogger<LoginWith2faModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte geben Sie den Authenticator-Code ein.")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "Der Code besteht aus 6 Ziffern.")]
        [DataType(DataType.Text)]
        [Display(Name = "Authenticator-Code")]
        public string TwoFactorCode { get; set; } = string.Empty;

        [Display(Name = "Diesen Browser merken")]
        public bool RememberMachine { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        // Kein ausstehender 2FA-Login (Direktaufruf)? → sauber zur Anmeldung, kein 500.
        if (await signInManager.GetTwoFactorAuthenticationUserAsync() is null)
            return RedirectToPage("./Login");
        ReturnUrl = returnUrl;
        RememberMe = rememberMe;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool rememberMe, string? returnUrl = null)
    {
        RememberMe = rememberMe;
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        returnUrl ??= Url.Content("~/");

        if (await signInManager.GetTwoFactorAuthenticationUserAsync() is null)
            return RedirectToPage("./Login");

        var code = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(code, rememberMe, Input.RememberMachine);

        if (result.Succeeded)
        {
            logger.LogInformation("Benutzer hat sich per Zwei-Faktor-Authentifizierung angemeldet.");
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("Benutzerkonto gesperrt (2FA).");
            return RedirectToPage("./Lockout");
        }

        logger.LogWarning("Ungültiger Authenticator-Code eingegeben.");
        ModelState.AddModelError(string.Empty, "Ungültiger Authenticator-Code.");
        return Page();
    }
}
