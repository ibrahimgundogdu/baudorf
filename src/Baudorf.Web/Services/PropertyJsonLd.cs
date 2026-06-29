using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;

namespace Baudorf.Web.Services;

/// <summary>
/// Erzeugt strukturierte Daten (JSON-LD) für eine Objektdetailseite:
/// ein Produkt-/RealEstateListing-Graph plus eine BreadcrumbList.
/// Bei Off-Market/gesperrten Objekten werden Preis/Angebot ausgelassen.
/// </summary>
public static class PropertyJsonLd
{
    public static string Build(
        Property p,
        string canonical,
        string? imageAbsolute,
        string baseUrl,
        string immobilienUrl,
        bool gated)
    {
        var additional = new List<object>();
        void AddProp(string name, object? value, string? unitCode = null)
        {
            if (value is null) return;
            var pv = new Dictionary<string, object?> { ["@type"] = "PropertyValue", ["name"] = name, ["value"] = value };
            if (unitCode is not null) pv["unitCode"] = unitCode;
            additional.Add(pv);
        }

        AddProp("Objektart", p.Art.Label());
        if (p.Wohnflaeche.HasValue) AddProp("Wohnfläche", p.Wohnflaeche.Value, "MTK");
        if (p.Grundstuecksflaeche.HasValue) AddProp("Grundstücksfläche", p.Grundstuecksflaeche.Value, "MTK");
        if (p.Baujahr.HasValue) AddProp("Baujahr", p.Baujahr.Value);
        AddProp("Energieklasse", JsonLd.NullIfBlank(p.Energieklasse));
        AddProp("Zustand", JsonLd.NullIfBlank(p.Zustand));

        var listing = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = new[] { "Product", "RealEstateListing" },
            ["name"] = JsonLd.NullIfBlank(p.MetaTitle) ?? p.Titel,
            ["description"] = JsonLd.NullIfBlank(p.MetaDescription) ?? JsonLd.NullIfBlank(StripDescription(p)),
            ["url"] = canonical,
            ["category"] = p.Art.Label(),
            ["image"] = string.IsNullOrEmpty(imageAbsolute) ? null : new[] { imageAbsolute },
            ["brand"] = new Dictionary<string, object?> { ["@type"] = "Organization", ["@id"] = baseUrl + "/#organization" },
            ["areaServed"] = JsonLd.NullIfBlank(p.Region),
            ["additionalProperty"] = additional.Count > 0 ? additional : null
        };

        // Angebot nur bei öffentlichem (nicht gesperrtem) Objekt mit bekanntem Preis.
        if (!gated && p.Kaufpreis is { } preis)
        {
            listing["offers"] = new Dictionary<string, object?>
            {
                ["@type"] = "Offer",
                ["price"] = preis,
                ["priceCurrency"] = "EUR",
                ["availability"] = Availability(p.Status),
                ["url"] = canonical,
                ["seller"] = new Dictionary<string, object?> { ["@id"] = baseUrl + "/#organization" }
            };
        }

        var breadcrumb = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = new object[]
            {
                Crumb(1, "Startseite", baseUrl + "/"),
                Crumb(2, "Immobilien", immobilienUrl),
                Crumb(3, p.Titel, canonical)
            }
        };

        return JsonLd.Serialize(listing) + "</script>\n<script type=\"application/ld+json\">" + JsonLd.Serialize(breadcrumb);
    }

    private static Dictionary<string, object?> Crumb(int pos, string name, string url) => new()
    {
        ["@type"] = "ListItem",
        ["position"] = pos,
        ["name"] = name,
        ["item"] = url
    };

    private static string Availability(PropertyStatus status) => status switch
    {
        PropertyStatus.Verfuegbar => "https://schema.org/InStock",
        PropertyStatus.Reserviert => "https://schema.org/LimitedAvailability",
        PropertyStatus.Verkauft => "https://schema.org/SoldOut",
        _ => "https://schema.org/InStock"
    };

    private static string? StripDescription(Property p)
    {
        if (string.IsNullOrWhiteSpace(p.Beschreibung)) return null;
        var text = System.Text.RegularExpressions.Regex.Replace(p.Beschreibung, "<.*?>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(text, "\\s+", " ").Trim();
        return text.Length > 300 ? text[..300].TrimEnd() + "…" : text;
    }
}
