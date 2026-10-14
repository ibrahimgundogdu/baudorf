using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>
/// Ein Social-Media-Profil, das im Footer (und im JSON-LD "sameAs") erscheint.
/// Frei pflegbar in der Verwaltung — beliebig viele, sortierbar, einzeln deaktivierbar.
/// </summary>
public class SocialLink
{
    public int Id { get; set; }

    /// <summary>Schlüssel aus <see cref="SocialPlatforms"/> (z. B. "tiktok"); bestimmt das Icon.</summary>
    [Required, MaxLength(40)] public string Plattform { get; set; } = "andere";

    [Required, MaxLength(500)] public string Url { get; set; } = string.Empty;

    /// <summary>Optionaler Anzeigename — vor allem für "Andere" (sonst Plattformname).</summary>
    [MaxLength(80)] public string? Bezeichnung { get; set; }

    public int Reihenfolge { get; set; }
    public bool IstAktiv { get; set; } = true;
}
