using Baudorf.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Services;

/// <summary>
/// Zentraler Lesezugriff auf die <c>SiteSettings</c> (Key/Value). Lädt alle Werte einmal
/// pro Request (scoped) und stellt sie Views/Komponenten zur Verfügung — so muss nicht jede
/// Stelle einzeln die DB abfragen. Editierbar im Admin unter Einstellungen.
/// </summary>
public interface ISiteSettings
{
    string Get(string key, string fallback = "");
    bool GetBool(string key, bool fallback = false);

    /// <summary>tel:-tauglicher Link aus der Anzeige-Telefonnummer (Ziffern → +49…).</summary>
    string PhoneLink { get; }
}

public class SiteSettingsService(ApplicationDbContext db) : ISiteSettings
{
    private Dictionary<string, string>? _cache;

    private Dictionary<string, string> Values =>
        _cache ??= db.SiteSettings.AsNoTracking()
            .ToDictionary(s => s.Key, s => s.Value ?? string.Empty);

    public string Get(string key, string fallback = "")
    {
        var v = Values.GetValueOrDefault(key);
        return string.IsNullOrWhiteSpace(v) ? fallback : v;
    }

    public bool GetBool(string key, bool fallback = false)
    {
        var v = Values.GetValueOrDefault(key);
        return string.IsNullOrWhiteSpace(v)
            ? fallback
            : string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || v == "1";
    }

    public string PhoneLink
    {
        get
        {
            var phone = Get("contact.phone");
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits)) return string.Empty;
            // Führende 0 zur internationalen +49-Form (bewusst wie bisher: +49 + lokale Ziffern).
            return "+49" + digits;
        }
    }
}
