namespace Baudorf.Web.Services;

/// <summary>
/// Prüft früh im Pipeline, ob der angeforderte Pfad einer aktiven Weiterleitung entspricht,
/// und antwortet dann mit 301/302 auf die neue Adresse. Statische Pfade werden übersprungen.
/// Die 404-Protokollierung passiert separat im FehlerController (StatusCodePages).
/// </summary>
public class SeoRedirectMiddleware(RequestDelegate next, IRedirectService redirects)
{
    private static readonly string[] SkipPrefixe =
        ["/css", "/js", "/lib", "/img", "/uploads", "/favicon", "/Admin", "/Identity", "/Fehler", "/_"];

    public async Task Invoke(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "/";

        if (!SkipPrefixe.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            var query = ctx.Request.QueryString.HasValue ? ctx.Request.QueryString.Value : null;
            var match = await redirects.MatchAsync(path, query);
            if (match is { } r && !ZeigtAufSichSelbst(r.Nach, path))
            {
                _ = redirects.BumpAsync(r.Id); // Zähler nebenläufig
                ctx.Response.StatusCode = r.Code == 302
                    ? StatusCodes.Status302Found
                    : StatusCodes.Status301MovedPermanently;
                ctx.Response.Headers.Location = r.Nach;
                return;
            }
        }

        await next(ctx);
    }

    /// <summary>
    /// Schleifenschutz: verhindert, dass das Ziel (nach Normalisierung) wieder auf denselben
    /// Pfad zeigt — z. B. eine reine Groß-/Kleinschreibungs-Weiterleitung "/immobilien" → "/Immobilien",
    /// die das (case-insensitiv gematchte) Ziel endlos erneut auslösen würde.
    /// </summary>
    private static bool ZeigtAufSichSelbst(string ziel, string aktuellerPfad)
    {
        if (string.IsNullOrWhiteSpace(ziel)) return true;
        if (ziel.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || ziel.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return false; // absolute URL → als anderes Ziel behandeln

        var pfad = ziel;
        var hash = pfad.IndexOf('#'); if (hash >= 0) pfad = pfad[..hash];
        var frage = pfad.IndexOf('?'); if (frage >= 0) pfad = pfad[..frage];
        if (pfad.Length == 0) return false;

        return string.Equals(IRedirectService.Normalize(pfad),
                             IRedirectService.Normalize(aktuellerPfad), StringComparison.Ordinal);
    }
}
