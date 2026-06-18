using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>Team-Mitglied (inkl. Bürohund Ayla).</summary>
public class TeamMember
{
    public int Id { get; set; }

    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(160)] public string? Rolle { get; set; }
    public string? Bio { get; set; }
    [MaxLength(500)] public string? FotoUrl { get; set; }

    public int Reihenfolge { get; set; }
    public bool IstSichtbar { get; set; } = true;
}
