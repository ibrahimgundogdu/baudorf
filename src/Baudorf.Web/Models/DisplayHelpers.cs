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
        PropertyStatus.Vorankuendigung => "Vorankündigung",
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

    /// <summary>Ist die URL ein Video (nach Dateiendung)?</summary>
    public static bool IsVideoUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var q = url.Split('?')[0];
        return q.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
            || q.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
            || q.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
            || q.EndsWith(".ogv", StringComparison.OrdinalIgnoreCase)
            || q.EndsWith(".mov", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Geschätzte Lesezeit in Minuten (~200 Wörter/Minute, HTML wird entfernt).</summary>
    public static int LesezeitMinuten(this BlogPost p)
    {
        if (string.IsNullOrWhiteSpace(p.Body)) return 1;
        var text = System.Text.RegularExpressions.Regex.Replace(p.Body, "<.*?>", " ");
        var woerter = System.Text.RegularExpressions.Regex.Matches(text, @"[\p{L}\p{N}]+").Count;
        return Math.Max(1, (int)Math.Ceiling(woerter / 200.0));
    }

    /// <summary>
    /// Entfernt vertrauliche Off-Market-Daten (Preis, Kennzahlen, Beschreibung, Adresse,
    /// Koordinaten und ALLE Medien) aus dem Objekt, BEVOR es an eine View geht — für nicht
    /// freigegebene Besucher. Sichtbar bleiben nur Teaser-Felder: Titel, Region, Land, Art, Status.
    /// Nur auf AsNoTracking-Instanzen anwenden (verändert keine Datenbankwerte).
    /// </summary>
    public static void RedactOffMarket(this Property p)
    {
        p.Kaufpreis = null;
        p.Faktor = null;
        p.RenditeProzent = null;
        p.Wohnflaeche = null;
        p.Gewerbeflaeche = null;
        p.Grundstuecksflaeche = null;
        p.VideoUrl = null;
        p.Baujahr = null;
        p.Einheiten = null;
        p.Energieklasse = null;
        p.Zustand = null;
        p.Beschreibung = null;
        p.AdresseIntern = null;
        p.Lat = null;
        p.Lng = null;
        p.MetaDescription = null;
        p.Medien = new List<PropertyMedia>();
    }

    /// <summary>
    /// Rendert eine Team-Rolle mit Zeilenumbrüchen: eigene Zeilenumbrüche werden zu &lt;br&gt;,
    /// und ein "Dipl.-Ing. …"-Zusatz kommt automatisch auf eine neue Zeile.
    /// </summary>
    public static Microsoft.AspNetCore.Html.IHtmlContent RoleLines(this string? rolle)
    {
        if (string.IsNullOrWhiteSpace(rolle)) return Microsoft.AspNetCore.Html.HtmlString.Empty;
        var enc = System.Net.WebUtility.HtmlEncode(rolle).Replace("\n", "<br />");
        enc = System.Text.RegularExpressions.Regex.Replace(enc, @"\s+(Dipl\.\-?\s?Ing\.)", "<br />$1");
        return new Microsoft.AspNetCore.Html.HtmlString(enc);
    }

    /// <summary>Cover-URL oder null (View rendert dann einen Marken-Platzhalter).</summary>
    public static string? CoverUrl(this Property p)
    {
        var cover = p.Medien.FirstOrDefault(m => m.IstCover && m.Typ == MediaType.Image)
                    ?? p.Medien.FirstOrDefault(m => m.Typ == MediaType.Image);
        return cover?.Url;
    }
}
