namespace Baudorf.Web.Models;

/// <summary>
/// Zentrale Site-/SEO-Konfiguration (gebunden an die Sektion "Site" in appsettings).
/// <see cref="BaseUrl"/> bleibt in Entwicklung leer und wird aus dem Request abgeleitet;
/// in Produktion auf "https://baudorf.de" gesetzt (appsettings.Production.json), damit
/// Canonical-/OG-/Sitemap-URLs immer auf die offizielle Domain zeigen.
/// </summary>
public class SiteOptions
{
    public const string SectionName = "Site";

    public string BaseUrl { get; set; } = string.Empty;
    public string Name { get; set; } = "Baudorf Immobilien";
    public string LegalName { get; set; } = "Baudorf Immobilien GmbH";
    public string Slogan { get; set; } = "Still, wirkungsvoll, mit Stil.";
    public string Locale { get; set; } = "de_DE";
    public string TwitterHandle { get; set; } = string.Empty;
    public string DefaultOgImage { get; set; } = "/img/og/baudorf-og.png";
    public string FoundingYear { get; set; } = "1994";

    public ContactOptions Contact { get; set; } = new();
    public List<string> SameAs { get; set; } = [];
}

public class ContactOptions
{
    public string Phone { get; set; } = string.Empty;
    public string PhoneDisplay { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = "DE";
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}
