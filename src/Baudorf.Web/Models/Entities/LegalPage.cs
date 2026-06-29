using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>
/// Rechtliche Seite (Impressum / Datenschutz / AGB) — Inhalt im Admin editierbar (Rich-Text/HTML).
/// </summary>
public class LegalPage
{
    public int Id { get; set; }

    /// <summary>URL-/Routing-Schlüssel: impressum, datenschutz, agb.</summary>
    [Required, MaxLength(60)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Titel { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Overline { get; set; } = "Rechtliches";

    /// <summary>Inhalt als HTML (im Admin via WYSIWYG-Editor gepflegt).</summary>
    public string BodyHtml { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
