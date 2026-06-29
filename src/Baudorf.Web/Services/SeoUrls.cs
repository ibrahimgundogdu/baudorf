using Baudorf.Web.Models;

namespace Baudorf.Web.Services;

/// <summary>
/// Baut absolute URLs für Canonical-, Open-Graph- und Sitemap-Zwecke.
/// Bevorzugt <see cref="SiteOptions.BaseUrl"/> (Produktion → offizielle Domain),
/// fällt sonst auf Schema + Host des aktuellen Requests zurück.
/// </summary>
public static class SeoUrls
{
    /// <summary>Basis-URL ohne abschließenden Schrägstrich, z. B. "https://baudorf.de".</summary>
    public static string Base(HttpRequest request, SiteOptions site)
    {
        if (!string.IsNullOrWhiteSpace(site.BaseUrl))
        {
            return site.BaseUrl.TrimEnd('/');
        }

        return $"{request.Scheme}://{request.Host.Value}";
    }

    /// <summary>
    /// Macht einen relativen Pfad absolut. Bereits absolute http(s)-URLs werden unverändert
    /// zurückgegeben; null/leer ergibt einen leeren String.
    /// </summary>
    public static string Absolute(HttpRequest request, SiteOptions site, string? pathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl))
        {
            return string.Empty;
        }

        if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return pathOrUrl;
        }

        var path = pathOrUrl.StartsWith('/') ? pathOrUrl : "/" + pathOrUrl;
        return Base(request, site) + path;
    }

    /// <summary>
    /// Canonical-URL der aktuellen Seite (Query-String wird bewusst ausgeschlossen).
    /// Mit <paramref name="overridePath"/> lässt sich ein abweichender Pfad erzwingen.
    /// </summary>
    public static string Canonical(HttpRequest request, SiteOptions site, string? overridePath = null)
    {
        var path = overridePath ?? request.Path.Value ?? "/";
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        return Base(request, site) + path;
    }
}
