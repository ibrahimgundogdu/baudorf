using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>
/// Widerruf eines Maklervertrags (gesetzliche Pflicht „Vertrag widerrufen").
/// Wird über das öffentliche Formular eingereicht und im Admin verwaltet.
/// </summary>
public class WiderrufAntrag
{
    public int Id { get; set; }

    [Required, MaxLength(80)] public string Vorname { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Nachname { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string Email { get; set; } = string.Empty;

    [MaxLength(200)] public string? Strasse { get; set; }
    [MaxLength(120)] public string? PlzOrt { get; set; }

    /// <summary>Vertragsidentifikation / Auftragsnummer.</summary>
    [Required, MaxLength(160)] public string Vertragsidentifikation { get; set; } = string.Empty;

    /// <summary>Datum / Beschreibung des Vertrags (Freitext).</summary>
    [Required] public string DatumBeschreibung { get; set; } = string.Empty;

    public bool Bestaetigt { get; set; }

    [MaxLength(60)] public string? IpAdresse { get; set; }
    public bool Erledigt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
