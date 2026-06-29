using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.ViewModels;

public class WiderrufViewModel
{
    [Required(ErrorMessage = "Bitte geben Sie Ihren Vornamen ein.")]
    [Display(Name = "Vorname")]
    public string Vorname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte geben Sie Ihren Nachnamen ein.")]
    [Display(Name = "Nachname")]
    public string Nachname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte geben Sie Ihre E-Mail-Adresse ein.")]
    [EmailAddress(ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse ein.")]
    [Display(Name = "E-Mail-Adresse")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Straße Nr.")]
    public string? Strasse { get; set; }

    [Display(Name = "PLZ Ort")]
    public string? PlzOrt { get; set; }

    [Required(ErrorMessage = "Bitte geben Sie die Vertragsidentifikation an.")]
    [Display(Name = "Vertragsidentifikation (Auftragsnummer)")]
    public string Vertragsidentifikation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte beschreiben Sie den zu widerrufenden Vertrag.")]
    [Display(Name = "Datum / Beschreibung")]
    public string DatumBeschreibung { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Bitte bestätigen Sie Ihren Widerrufswillen.")]
    [Display(Name = "Ich bestätige hiermit meinen eindeutigen Widerrufswillen")]
    public bool Bestaetigt { get; set; }

    /// <summary>Honeypot — muss leer bleiben.</summary>
    public string? Website { get; set; }
}
