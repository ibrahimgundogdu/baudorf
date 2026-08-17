using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>
/// Eine 301/302-Weiterleitung von einer alten (indexierten) URL auf die neue Zieladresse.
/// <see cref="VonPfad"/> wird normalisiert gespeichert (klein, ohne abschließenden Slash).
/// </summary>
public class Redirect
{
    public int Id { get; set; }

    /// <summary>Alter Pfad, z. B. "/immobilien-velbert" oder "/?p=123". Normalisiert, eindeutig.</summary>
    [Required, MaxLength(400)]
    public string VonPfad { get; set; } = string.Empty;

    /// <summary>Ziel: relativer Pfad ("/Immobilien") oder absolute URL.</summary>
    [Required, MaxLength(600)]
    public string NachPfad { get; set; } = string.Empty;

    /// <summary>301 = dauerhaft (Standard, überträgt Ranking), 302 = temporär.</summary>
    public int Code { get; set; } = 301;

    public bool IstAktiv { get; set; } = true;

    /// <summary>Wie oft die Weiterleitung ausgelöst wurde.</summary>
    public int Treffer { get; set; }
    public DateTimeOffset? LetzterTreffer { get; set; }

    [MaxLength(300)] public string? Notiz { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
