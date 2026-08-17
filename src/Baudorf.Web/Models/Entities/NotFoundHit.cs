using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>
/// Aggregiertes 404-Protokoll: pro nicht gefundenem Pfad EIN Datensatz mit Trefferzähler.
/// So sieht der Admin, welche alten/kaputten URLs tatsächlich noch aufgerufen werden, und
/// kann gezielt Weiterleitungen anlegen (statt tausende Einzelzeilen zu durchsuchen).
/// </summary>
public class NotFoundHit
{
    public int Id { get; set; }

    /// <summary>Angeforderter Pfad (inkl. Query, falls vorhanden). Normalisiert, eindeutig.</summary>
    [Required, MaxLength(400)]
    public string Pfad { get; set; } = string.Empty;

    public int Anzahl { get; set; } = 1;

    /// <summary>Woher der letzte Aufruf kam (Referrer) — hilft, die Quelle zu erkennen.</summary>
    [MaxLength(600)] public string? LetzterReferrer { get; set; }

    /// <summary>True, sobald dafür eine Weiterleitung angelegt wurde (dann erledigt).</summary>
    public bool Erledigt { get; set; }

    public DateTimeOffset Zuerst { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Zuletzt { get; set; } = DateTimeOffset.UtcNow;
}
