using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>Leistung / Dienstleistung mit eigener Detailseite (z. B. Stille Vermarktung).</summary>
public class Leistung
{
    public int Id { get; set; }

    [Required, MaxLength(200)] public string Titel { get; set; } = string.Empty;
    [Required, MaxLength(220)] public string Slug { get; set; } = string.Empty;

    [MaxLength(120)] public string? Overline { get; set; }
    [MaxLength(400)] public string? Teaser { get; set; }   // Kurztext für die Karte
    public string? Body { get; set; }                       // Rich-HTML der Detailseite

    [MaxLength(500)] public string? CoverUrl { get; set; }

    // SEO
    [MaxLength(200)] public string? MetaTitle { get; set; }
    [MaxLength(320)] public string? MetaDescription { get; set; }

    public int Reihenfolge { get; set; }
    public bool IstVeroeffentlicht { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
