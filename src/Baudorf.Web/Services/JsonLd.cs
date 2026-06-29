using System.Text.Json;
using System.Text.Json.Serialization;

namespace Baudorf.Web.Services;

/// <summary>
/// Serialisiert schema.org-Objekte (als verschachtelte Dictionaries/Objekte) zu JSON-LD.
/// Null-Werte werden weggelassen; der Standard-Encoder maskiert &lt;, &gt;, &amp; sicher
/// (kein <c>&lt;/script&gt;</c>-Ausbruch). Wird in <c>&lt;script type="application/ld+json"&gt;</c> eingebettet.
/// </summary>
public static class JsonLd
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string Serialize(object graph) => JsonSerializer.Serialize(graph, Options);

    /// <summary>Hilfsfunktion: erstellt ein PostalAddress-Objekt.</summary>
    public static Dictionary<string, object?> PostalAddress(
        string? street, string? postalCode, string? city, string? region, string? country) => new()
    {
        ["@type"] = "PostalAddress",
        ["streetAddress"] = NullIfBlank(street),
        ["postalCode"] = NullIfBlank(postalCode),
        ["addressLocality"] = NullIfBlank(city),
        ["addressRegion"] = NullIfBlank(region),
        ["addressCountry"] = NullIfBlank(country)
    };

    public static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
