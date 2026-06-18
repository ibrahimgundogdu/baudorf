using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>Tippgeber-Empfehlung (Tavsiye/komisyon programı).</summary>
public class TippgeberApplication
{
    public int Id { get; set; }

    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(200)] public string Email { get; set; } = string.Empty;
    [MaxLength(60)] public string? Telefon { get; set; }

    /// <summary>Beschreibung des empfohlenen Objekts / Kontakts.</summary>
    [MaxLength(4000)] public string? Empfehlung { get; set; }

    public LeadStatus Status { get; set; } = LeadStatus.Neu;
    public bool DsgvoAkzeptiert { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Karriere-Bewerbung (offene Stelle oder Initiativbewerbung).</summary>
public class CareerApplication
{
    public int Id { get; set; }

    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(200)] public string Email { get; set; } = string.Empty;
    [MaxLength(60)] public string? Telefon { get; set; }

    [MaxLength(160)] public string? Position { get; set; }
    [MaxLength(4000)] public string? Nachricht { get; set; }
    [MaxLength(500)] public string? LebenslaufUrl { get; set; }

    public LeadStatus Status { get; set; } = LeadStatus.Neu;
    public bool DsgvoAkzeptiert { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
