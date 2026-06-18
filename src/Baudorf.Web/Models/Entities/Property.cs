using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>Immobilie (Objekt) — Kern-Entity des Portfolios.</summary>
public class Property
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Titel { get; set; } = string.Empty;

    /// <summary>URL-Slug, eindeutig.</summary>
    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    public PropertyKind Art { get; set; }
    public PropertyStatus Status { get; set; }

    // Standort (genaue Adresse bei Off-Market verborgen → nur Region/Land öffentlich)
    [MaxLength(160)] public string? Region { get; set; }     // z. B. "Velbert, NRW"
    [MaxLength(80)] public string Land { get; set; } = "Deutschland";
    [MaxLength(260)] public string? AdresseIntern { get; set; } // nur Admin/gating
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    // Kennzahlen
    public double? Wohnflaeche { get; set; }          // m²
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

    // Gating & Sichtbarkeit
    public bool IstOffMarket { get; set; }
    public bool IstFeatured { get; set; }
    public bool IstVeroeffentlicht { get; set; }

    // SEO
    [MaxLength(200)] public string? MetaTitle { get; set; }
    [MaxLength(320)] public string? MetaDescription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<PropertyMedia> Medien { get; set; } = new List<PropertyMedia>();
    public ICollection<Lead> Leads { get; set; } = new List<Lead>();
}
