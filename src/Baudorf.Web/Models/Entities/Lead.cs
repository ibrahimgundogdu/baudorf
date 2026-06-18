using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>Anfrage aus dem Kontaktformular oder objektbezogener Lead.</summary>
public class Lead
{
    public int Id { get; set; }

    [Required, MaxLength(80)] public string Vorname { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Nachname { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(200)] public string Email { get; set; } = string.Empty;
    [MaxLength(60)] public string? Telefon { get; set; }

    public InterestType Interesse { get; set; }

    [Required, MaxLength(4000)] public string Nachricht { get; set; } = string.Empty;

    /// <summary>Optionaler Objektbezug (z. B. "Vertraulich anfragen").</summary>
    public int? PropertyId { get; set; }
    public Property? Property { get; set; }

    public LeadStatus Status { get; set; } = LeadStatus.Neu;
    [MaxLength(2000)] public string? Notiz { get; set; }
    [MaxLength(160)] public string? ZugewiesenAn { get; set; }

    public bool DsgvoAkzeptiert { get; set; }
    [MaxLength(64)] public string? IpAdresse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
