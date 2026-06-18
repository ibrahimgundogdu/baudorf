using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>Key/Value-Einstellung (Kontaktdaten, Social, SMTP, Hero-Medien, Rechtstexte).</summary>
public class SiteSetting
{
    public int Id { get; set; }

    [Required, MaxLength(120)] public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    [MaxLength(300)] public string? Beschreibung { get; set; }
}
