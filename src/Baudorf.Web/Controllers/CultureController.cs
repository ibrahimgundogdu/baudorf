using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Baudorf.Web.Controllers;

/// <summary>Setzt die Sprache (Cookie) und kehrt zur vorherigen Seite zurück (Admin-Umschalter).</summary>
public class CultureController : Controller
{
    [HttpGet]
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (culture is "de" or "en")
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    Path = "/"
                });
        }
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/Admin" : returnUrl);
    }
}
