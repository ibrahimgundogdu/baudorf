using System.ComponentModel.DataAnnotations;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Baudorf.Web.Areas.Identity.Pages.Account;

public class LoginWithRecoveryCodeModel(
    SignInManager<ApplicationUser> signInManager,
    ILogger<LoginWithRecoveryCodeModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte geben Sie einen Wiederherstellungscode ein.")]
        [DataType(DataType.Text)]
        [Display(Name = "Wiederherstellungscode")]
        public string RecoveryCode { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        if (await signInManager.GetTwoFactorAuthenticationUserAsync() is null)
            return RedirectToPage("./Login");
        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (await signInManager.GetTwoFactorAuthenticationUserAsync() is null)
            return RedirectToPage("./Login");

        var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);
        var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        returnUrl ??= Url.Content("~/");

        if (result.Succeeded)
        {
            logger.LogInformation("Anmeldung per Wiederherstellungscode erfolgreich.");
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("Konto gesperrt (Wiederherstellungscode).");
            return RedirectToPage("./Lockout");
        }

        logger.LogWarning("Ungültiger Wiederherstellungscode eingegeben.");
        ModelState.AddModelError(string.Empty, "Ungültiger Wiederherstellungscode.");
        return Page();
    }
}
