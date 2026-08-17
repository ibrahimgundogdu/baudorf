using Baudorf.Web.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Baudorf.Web.Controllers;

/// <summary>
/// Ziel von UseStatusCodePagesWithReExecute("/Fehler/{0}"). Zeigt eine markenkonforme
/// Fehlerseite und protokolliert 404er (aggregiert je Pfad), damit der Admin kaputte/alte
/// URLs erkennt und gezielt Weiterleitungen anlegen kann.
/// </summary>
public class FehlerController(IRedirectService redirects) : Controller
{
    // Statische Assets nicht als "kaputte Seite" protokollieren (nur echte Seitenaufrufe zählen).
    private static readonly string[] AssetEndungen =
        [".css", ".js", ".map", ".png", ".jpg", ".jpeg", ".webp", ".avif", ".gif", ".svg",
         ".ico", ".woff", ".woff2", ".ttf", ".eot", ".mp4", ".webm", ".pdf", ".xml", ".txt"];

    [Route("/Fehler/{code:int}")]
    public async Task<IActionResult> Index(int code)
    {
        Response.StatusCode = code;

        var feature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        var originalPath = feature?.OriginalPath ?? Request.Path.Value ?? "/";
        var originalQuery = feature?.OriginalQueryString ?? string.Empty;

        if (code == 404
            && HttpMethods.IsGet(Request.Method)
            && !AssetEndungen.Any(e => originalPath.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
        {
            var referrer = Request.Headers.Referer.ToString();
            await redirects.LogNotFoundAsync(originalPath + originalQuery,
                string.IsNullOrWhiteSpace(referrer) ? null : referrer);
        }

        ViewData["Code"] = code;
        ViewData["Path"] = originalPath;
        return View(code == 404 ? "NotFound" : "Fehler");
    }
}
