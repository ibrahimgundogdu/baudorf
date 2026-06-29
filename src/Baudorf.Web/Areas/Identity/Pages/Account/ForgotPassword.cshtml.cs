using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Baudorf.Web.Areas.Identity.Pages.Account;

public class ForgotPasswordModel(
    UserManager<ApplicationUser> userManager,
    IEmailService email,
    ILogger<ForgotPasswordModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte geben Sie Ihre E-Mail-Adresse ein.")]
        [EmailAddress(ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse ein.")]
        [Display(Name = "E-Mail")]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);

        // Aus Datenschutzgründen immer dieselbe Bestätigung zeigen — egal ob das Konto existiert.
        if (user is not null)
        {
            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code },
                protocol: Request.Scheme);

            var body = $"""
                <h2>Passwort zurücksetzen</h2>
                <p>Sie haben angefordert, Ihr Passwort für Ihr Baudorf-Konto zurückzusetzen.</p>
                <p><a href="{HtmlEncoder.Default.Encode(callbackUrl ?? "")}">Passwort jetzt zurücksetzen</a></p>
                <p>Falls Sie diese Anfrage nicht gestellt haben, können Sie diese E-Mail ignorieren.</p>
                """;
            try
            {
                await email.SendAsync(Input.Email, "Passwort zurücksetzen — Baudorf Immobilien", body);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Passwort-Reset-Mail an {Email} fehlgeschlagen.", Input.Email);
            }
        }

        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}
