using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.ViewModels;

/// <summary>Kontaktformular — Eingabe + Anti-Spam (Honeypot) + DSGVO.</summary>
public class KontaktViewModel
{
    [Required(ErrorMessage = "Bitte geben Sie Ihren Vornamen an.")]
    [StringLength(80)]
    [Display(Name = "Vorname")]
    public string Vorname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte geben Sie Ihren Nachnamen an.")]
    [StringLength(80)]
    [Display(Name = "Nachname")]
    public string Nachname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte geben Sie Ihre E-Mail-Adresse an.")]
    [EmailAddress(ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse an.")]
    [StringLength(200)]
    [Display(Name = "E-Mail")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Bitte geben Sie eine gültige Telefonnummer an.")]
    [StringLength(60)]
    [Display(Name = "Telefon")]
    public string? Telefon { get; set; }

    [Required(ErrorMessage = "Bitte wählen Sie Ihr Anliegen.")]
    [Display(Name = "Ich interessiere mich als")]
    public InterestType Interesse { get; set; }

    [Required(ErrorMessage = "Bitte beschreiben Sie Ihr Anliegen.")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Bitte schreiben Sie mindestens 10 Zeichen.")]
    [Display(Name = "Ihr Anliegen")]
    public string Nachricht { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Bitte stimmen Sie der Datenschutzerklärung zu.")]
    [Display(Name = "Datenschutz")]
    public bool DsgvoAkzeptiert { get; set; }

    /// <summary>Optionaler Objektbezug (Slug) — für "Vertraulich anfragen".</summary>
    public string? ObjektSlug { get; set; }
    public string? ObjektTitel { get; set; }

    /// <summary>Honeypot — von echten Nutzern leer, von Bots oft ausgefüllt. Nicht anzeigen.</summary>
    public string? Website { get; set; }
}
