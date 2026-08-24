using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>Immobilie (Objekt) — Kern-Entity des Portfolios.</summary>
public class Property
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Titel { get; set; } = string.Empty;

    /// <summary>URL-Slug, eindeutig. Wird bei leerem Feld automatisch aus dem Titel erzeugt
    /// (siehe PropertiesController.AssignUniqueSlugAsync) — daher kein [Required].</summary>
    [MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Alte Enum-Spalte — bleibt als Backup erhalten; führend ist jetzt <see cref="ArtKey"/>.</summary>
    public PropertyKind Art { get; set; }

    /// <summary>Objektart als admin-pflegbarer Lookup-Schlüssel (LookupOption Kategorie "objektart").</summary>
    [MaxLength(60)] public string ArtKey { get; set; } = string.Empty;

    public PropertyStatus Status { get; set; }

    // Standort (genaue Adresse bei Off-Market verborgen → nur Region/Land öffentlich)
    [MaxLength(160)] public string? Region { get; set; }     // z. B. "Velbert, NRW"
    [MaxLength(80)] public string Land { get; set; } = "Deutschland";
    [MaxLength(260)] public string? AdresseIntern { get; set; } // nur Admin/gating
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    // Kennzahlen
    public double? Wohnflaeche { get; set; }          // m²
    public double? Gewerbeflaeche { get; set; }       // m² (Gewerbefläche / commercial space)
    public double? Grundstuecksflaeche { get; set; }  // m²
    public int? Baujahr { get; set; }
    [MaxLength(80)] public string? Zustand { get; set; }
    [MaxLength(40)] public string? Energieklasse { get; set; }
    public int? Einheiten { get; set; }
    public decimal? Faktor { get; set; }
    public decimal? RenditeProzent { get; set; }

    /// <summary>Kaufpreis in EUR; null bedeutet "auf Anfrage".</summary>
    public decimal? Kaufpreis { get; set; }

    public string? Beschreibung { get; set; }  // rich text / html

    /// <summary>Optionaler Video-Link (Instagram-Reel, YouTube, Vimeo oder hochgeladene Datei
    /// aus der Mediathek). Wird auf der Detailseite eingebettet bzw. als Button angezeigt.</summary>
    [MaxLength(500)] public string? VideoUrl { get; set; }

    // Gating & Sichtbarkeit
    public bool IstOffMarket { get; set; }
    public bool IstFeatured { get; set; }
    public bool IstVeroeffentlicht { get; set; }

    // SEO
    [MaxLength(200)] public string? MetaTitle { get; set; }
    [MaxLength(320)] public string? MetaDescription { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Soft-Delete: im Papierkorb, überall ausgeblendet (globaler Query-Filter),
    /// im Admin wiederherstellbar. Erst „endgültig löschen" entfernt Datensatz + Dateien.</summary>
    public bool IstGeloescht { get; set; }
    public DateTimeOffset? GeloeschtAm { get; set; }

    public ICollection<PropertyMedia> Medien { get; set; } = new List<PropertyMedia>();
    public ICollection<Lead> Leads { get; set; } = new List<Lead>();
}
