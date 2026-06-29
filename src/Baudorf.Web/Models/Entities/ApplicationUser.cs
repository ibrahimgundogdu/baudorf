using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Baudorf.Web.Models.Entities;

/// <summary>
/// Erweiterter Identity-Benutzer. Off-Market-Zugang wird erst nach
/// Admin-Freigabe (<see cref="IstFreigegeben"/>) gewährt.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? AnzeigeName { get; set; }

    /// <summary>Firma/Unternehmen (optional, falls vorhanden).</summary>
    [MaxLength(160)] public string? Firma { get; set; }

    /// <summary>Beruf des Interessenten.</summary>
    [MaxLength(120)] public string? Beruf { get; set; }

    /// <summary>Begründung: Warum möchte die Person Zugang? (Hilft bei der Freigabe-Entscheidung.)</summary>
    [MaxLength(2000)] public string? Registrierungsgrund { get; set; }

    /// <summary>Zeitpunkt der AGB-/Vertragszustimmung bei der Registrierung.</summary>
    public DateTime? AgbAkzeptiertAm { get; set; }

    /// <summary>Vom Admin freigegebener Investor → darf Off-Market-Details sehen.</summary>
    public bool IstFreigegeben { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FreigegebenAm { get; set; }

    public ICollection<Favorite> Favoriten { get; set; } = new List<Favorite>();
}
