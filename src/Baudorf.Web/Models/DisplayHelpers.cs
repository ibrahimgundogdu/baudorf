using System.Globalization;
using Baudorf.Web.Models.Entities;

namespace Baudorf.Web.Models;

/// <summary>Anzeige-Helfer für Enums, Preise und Medien (deutsche Labels, ASCII-safe Code).</summary>
public static class DisplayHelpers
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static string Label(this PropertyKind kind) => kind switch
    {
        PropertyKind.OffMarket => "Off-Market",
        PropertyKind.Kapitalanlage => "Kapitalanlage",
        PropertyKind.Investment => "Investment",
        PropertyKind.Gewerbe => "Gewerbeimmobilie",
        PropertyKind.Wohnimmobilie => "Wohnimmobilie",
        PropertyKind.Grundstueck => "Grundstück",
        PropertyKind.Projektentwicklung => "Projektentwicklung",
        PropertyKind.Auslandsimmobilie => "Auslandsimmobilie",
        _ => kind.ToString()
    };

    public static string Label(this PropertyStatus status) => status switch
    {
        PropertyStatus.OffMarket => "Off-Market",
        PropertyStatus.Verfuegbar => "Verfügbar",
        PropertyStatus.Reserviert => "Reserviert",
        PropertyStatus.Verkauft => "Verkauft",
        _ => status.ToString()
    };

    public static string Label(this InterestType t) => t switch
    {
        InterestType.KaeuferPrivatinvestor => "Käufer – Privatinvestor",
        InterestType.KaeuferFamilyOffice => "Käufer – Family Office",
        InterestType.KaeuferInstitutionell => "Käufer – Institutioneller Investor",
        InterestType.VerkaeuferBestandshalter => "Verkäufer – Bestandshalter",
        InterestType.VerkaeuferProjektentwickler => "Verkäufer – Projektentwickler",
        InterestType.Immobilienbewertung => "Immobilienbewertung",
        InterestType.Kaufbegleitung => "Kaufbegleitung",
        InterestType.Tippgeber => "Tippgeber",
        InterestType.Karriere => "Karriere",
        _ => "Sonstiges"
    };

    /// <summary>Kaufpreis formatiert oder "auf Anfrage", wenn null.</summary>
    public static string PreisText(this Property p) =>
        p.Kaufpreis is { } price ? price.ToString("C0", De) : "auf Anfrage";

    public static string FlaecheText(double? m2) =>
        m2 is { } v ? $"{v.ToString("N0", De)} m²" : "—";

    /// <summary>Cover-URL oder null (View rendert dann einen Marken-Platzhalter).</summary>
    public static string? CoverUrl(this Property p)
    {
        var cover = p.Medien.FirstOrDefault(m => m.IstCover && m.Typ == MediaType.Image)
                    ?? p.Medien.FirstOrDefault(m => m.Typ == MediaType.Image);
        return cover?.Url;
    }
}
