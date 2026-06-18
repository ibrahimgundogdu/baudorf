using Microsoft.AspNetCore.Identity;

namespace Baudorf.Web.Models.Entities;

/// <summary>
/// Erweiterter Identity-Benutzer. Off-Market-Zugang wird erst nach
/// Admin-Freigabe (<see cref="IstFreigegeben"/>) gewährt.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? AnzeigeName { get; set; }

    /// <summary>Vom Admin freigegebener Investor → darf Off-Market-Details sehen.</summary>
    public bool IstFreigegeben { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FreigegebenAm { get; set; }

    public ICollection<Favorite> Favoriten { get; set; } = new List<Favorite>();
}
