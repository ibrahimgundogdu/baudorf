namespace Baudorf.Web.Models;

/// <summary>
/// Cloudflare Turnstile (CAPTCHA). Schlüssel kommen aus appsettings (Prod: Production.json).
/// Ohne konfigurierte Schlüssel ist die Prüfung deaktiviert — so funktioniert die lokale
/// Entwicklung ohne Keys, und Turnstile aktiviert sich automatisch, sobald die Keys gesetzt sind.
/// </summary>
public class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    public string SiteKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    public bool Enabled => !string.IsNullOrWhiteSpace(SiteKey) && !string.IsNullOrWhiteSpace(SecretKey);
}
