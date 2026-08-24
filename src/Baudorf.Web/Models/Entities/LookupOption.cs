using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>
/// Admin-pflegbare Auswahloption (z. B. Objektart). Ersetzt harte Enums dort, wo Andrea
/// selbst neue Werte anlegen können soll. <see cref="Kategorie"/> gruppiert die Optionen,
/// <see cref="Wert"/> ist der gespeicherte Schlüssel, <see cref="Label"/> die Anzeige.
/// </summary>
public class LookupOption
{
    public int Id { get; set; }

    [Required, MaxLength(40)] public string Kategorie { get; set; } = string.Empty; // z. B. "objektart"
    [Required, MaxLength(60)] public string Wert { get; set; } = string.Empty;      // z. B. "grundstueck"
    [Required, MaxLength(120)] public string Label { get; set; } = string.Empty;    // z. B. "Grundstück"

    public int Reihenfolge { get; set; }
    public bool IstAktiv { get; set; } = true;
}
