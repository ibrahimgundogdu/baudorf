namespace Baudorf.Web.Models;

/// <summary>Bekannte Plattformen mit Anzeigenamen und SVG-Icon (24er viewBox).</summary>
public static class SocialPlatforms
{
    /// <param name="Key">Interner Schlüssel (in der DB gespeichert).</param>
    /// <param name="Name">Anzeigename / aria-label.</param>
    /// <param name="Svg">Inneres SVG-Markup.</param>
    /// <param name="Filled">true = Glyphe wird gefüllt, false = Kontur (stroke).</param>
    public sealed record Platform(string Key, string Name, string Svg, bool Filled);

    public const string Andere = "andere";

    public static readonly IReadOnlyList<Platform> All =
    [
        new("instagram", "Instagram",
            "<rect x=\"2\" y=\"2\" width=\"20\" height=\"20\" rx=\"5\" ry=\"5\"/><path d=\"M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z\"/><line x1=\"17.5\" y1=\"6.5\" x2=\"17.51\" y2=\"6.5\"/>", false),
        new("facebook", "Facebook",
            "<path d=\"M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z\"/>", false),
        new("linkedin", "LinkedIn",
            "<path d=\"M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z\"/><rect x=\"2\" y=\"9\" width=\"4\" height=\"12\"/><circle cx=\"4\" cy=\"4\" r=\"2\"/>", false),
        new("youtube", "YouTube",
            "<path d=\"M22.54 6.42a2.78 2.78 0 0 0-1.94-2C18.88 4 12 4 12 4s-6.88 0-8.6.46a2.78 2.78 0 0 0-1.94 2A29 29 0 0 0 1 11.75a29 29 0 0 0 .46 5.33A2.78 2.78 0 0 0 3.4 19c1.72.46 8.6.46 8.6.46s6.88 0 8.6-.46a2.78 2.78 0 0 0 1.94-2 29 29 0 0 0 .46-5.25 29 29 0 0 0-.46-5.33z\"/><polygon points=\"9.75 15.02 15.5 11.75 9.75 8.48 9.75 15.02\"/>", false),
        new("tiktok", "TikTok",
            "<path d=\"M12.525.02c1.31-.02 2.61-.01 3.91-.02.08 1.53.63 3.09 1.75 4.17 1.12 1.11 2.7 1.62 4.24 1.79v4.03c-1.44-.05-2.89-.35-4.2-.97-.57-.26-1.1-.59-1.62-.93-.01 2.92.01 5.84-.02 8.75-.08 1.4-.54 2.79-1.35 3.94-1.31 1.92-3.58 3.17-5.91 3.21-1.43.08-2.86-.31-4.08-1.03-2.02-1.19-3.44-3.37-3.65-5.71-.02-.5-.03-1-.01-1.49.18-1.9 1.12-3.72 2.58-4.96 1.66-1.44 3.98-2.13 6.15-1.72.02 1.48-.04 2.96-.04 4.44-.99-.32-2.15-.23-3.02.37-.63.41-1.11 1.04-1.36 1.75-.21.51-.15 1.07-.14 1.61.24 1.64 1.82 3.02 3.5 2.87 1.12-.01 2.19-.66 2.77-1.61.19-.33.4-.67.41-1.06.1-1.79.06-3.57.07-5.36.01-4.03-.01-8.05.02-12.07z\"/>", true),
        new("x", "X (Twitter)",
            "<path d=\"M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z\"/>", true),
        new("pinterest", "Pinterest",
            "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M9.6 21.2l2.1-8.7\"/><path d=\"M10.4 13.6c.5 1 1.4 1.5 2.4 1.5 2.3 0 3.8-2.1 3.8-4.6 0-2.7-2.1-4.5-4.8-4.5-3.1 0-5 2.2-5 4.7 0 1 .4 1.9 1.1 2.3\"/>", false),
        new("xing", "XING",
            "<path d=\"M18.188 0c-.517 0-.741.325-.927.66 0 0-7.455 13.224-7.702 13.657.015.024 4.919 9.023 4.919 9.023.17.308.436.66.967.66h3.454c.211 0 .375-.078.463-.22.089-.151.089-.346-.009-.536l-4.879-8.916c-.004-.006-.004-.016 0-.022L22.139.756c.095-.191.097-.387.006-.535C22.056.078 21.894 0 21.686 0h-3.498zM3.648 4.74c-.211 0-.385.074-.473.216-.09.149-.078.339.02.531l2.34 4.05c.004.01.004.016 0 .021L1.86 16.051c-.099.188-.093.381 0 .529.085.142.239.234.45.234h3.461c.518 0 .766-.348.945-.667l3.734-6.609-2.378-4.155c-.172-.315-.434-.659-.962-.659H3.648z\"/>", true),
        new("threads", "Threads",
            "<circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M16 8v5a3 3 0 0 0 6 0v-1a10 10 0 1 0-3.92 7.94\"/>", false),
        new("whatsapp", "WhatsApp",
            "<path d=\"M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z\"/>", false),
        new(Andere, "Andere / Website",
            "<path d=\"M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71\"/><path d=\"M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71\"/>", false),
    ];

    public static Platform Get(string? key) =>
        All.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))
        ?? All.First(p => p.Key == Andere);

    /// <summary>Vollständiges SVG-Element für ein Icon.</summary>
    public static string IconSvg(string? key, int size = 20)
    {
        var p = Get(key);
        var paint = p.Filled
            ? "fill=\"currentColor\" stroke=\"none\""
            : "fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"";
        // Gefüllte Glyphen (TikTok, X, XING) wirken neben den feinen Kontur-Icons deutlich
        // schwerer. Leicht verkleinert (85 %, zentriert) gleichen sie sich optisch an.
        var body = p.Filled ? $"<g transform=\"translate(1.8 1.8) scale(.85)\">{p.Svg}</g>" : p.Svg;
        return $"<svg width=\"{size}\" height=\"{size}\" viewBox=\"0 0 24 24\" {paint} aria-hidden=\"true\">{body}</svg>";
    }

    /// <summary>
    /// Nur http(s)-Links zulassen. Verhindert z. B. "javascript:"-URLs, die im Footer
    /// als klickbarer Link landen würden (XSS). Fehlt das Schema, wird https ergänzt.
    /// </summary>
    public static string? NormalizeUrl(string? url)
    {
        url = (url ?? string.Empty).Trim();
        if (url.Length == 0) return null;
        if (!url.Contains("://", StringComparison.Ordinal)) url = "https://" + url;
        return Uri.TryCreate(url, UriKind.Absolute, out var u)
               && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp)
               && !string.IsNullOrEmpty(u.Host)
            ? u.ToString()
            : null;
    }
}
