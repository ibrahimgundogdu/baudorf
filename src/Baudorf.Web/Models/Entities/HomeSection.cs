using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>
/// Bearbeitbarer Startseiten-Abschnitt (CMS). Jeder Abschnitt hat einen stabilen
/// <see cref="Key"/> (z. B. "hero", "philosophie") + frei editierbare Inhalte und optionale
/// Listenelemente (<see cref="Items"/>) für Karten/Schritte/Zahlen.
/// </summary>
public class HomeSection
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(120)] public string? Overline { get; set; }
    [MaxLength(300)] public string? Titel { get; set; }      // darf <em>…</em> enthalten
    public string? Text { get; set; }

    [MaxLength(500)] public string? BildUrl { get; set; }

    [MaxLength(120)] public string? CtaText { get; set; }
    [MaxLength(300)] public string? CtaUrl { get; set; }
    [MaxLength(120)] public string? Cta2Text { get; set; }
    [MaxLength(300)] public string? Cta2Url { get; set; }

    public int Reihenfolge { get; set; }
    public bool IstSichtbar { get; set; } = true;

    public ICollection<HomeSectionItem> Items { get; set; } = new List<HomeSectionItem>();
}

/// <summary>Listenelement eines Abschnitts (Leistung, Schritt, Zahl, Klientel-Eintrag).</summary>
public class HomeSectionItem
{
    public int Id { get; set; }

    public int HomeSectionId { get; set; }
    public HomeSection? HomeSection { get; set; }

    [MaxLength(160)] public string? Titel { get; set; }   // z. B. Zahl "1994" oder Kartentitel
    public string? Text { get; set; }
    [MaxLength(500)] public string? BildUrl { get; set; }

    public int Reihenfolge { get; set; }
}
