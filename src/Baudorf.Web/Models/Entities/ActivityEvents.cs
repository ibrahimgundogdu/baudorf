using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>Login-Protokoll: jeder erfolgreiche Anmeldevorgang.</summary>
public class LoginEvent
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [MaxLength(200)] public string? Email { get; set; }
    [MaxLength(64)] public string? IpAdresse { get; set; }
    [MaxLength(400)] public string? UserAgent { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Objekt-Aufruf: welcher Nutzer (oder Gast) welche Immobilie angesehen hat.</summary>
public class PropertyView
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [MaxLength(64)] public string? IpAdresse { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Cookie-Einwilligung (Nachweis nach Art. 5 Abs. 2 / Art. 7 DSGVO — Rechenschaftspflicht).
/// Datenminimierung: IP wird anonymisiert gespeichert (letztes Oktett/Suffix genullt).
/// </summary>
public class ConsentLog
{
    public int Id { get; set; }

    /// <summary>Zufalls-Referenz, die auch im Browser-Cookie liegt (Korrelation ohne Personenbezug).</summary>
    public Guid Referenz { get; set; }

    /// <summary>Gewählte Kategorien, z. B. "necessary,statistics,marketing".</summary>
    [MaxLength(200)] public string Kategorien { get; set; } = "necessary";

    /// <summary>Textstand/Version des Banners — bei Änderung wird erneut eingeholt.</summary>
    [MaxLength(40)] public string Version { get; set; } = "";

    /// <summary>accept | reject | custom — wie die Einwilligung erteilt wurde.</summary>
    [MaxLength(20)] public string Aktion { get; set; } = "";

    /// <summary>Anonymisierte IP (z. B. 203.0.113.0).</summary>
    [MaxLength(64)] public string? IpAnonymisiert { get; set; }

    [MaxLength(400)] public string? UserAgent { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>WhatsApp-Klick: Klick auf den Click-to-Chat-Button (Lead-Indikator).</summary>
public class WhatsAppClick
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>Optionaler Objektbezug (Klick auf einer Objektseite).</summary>
    public int? PropertyId { get; set; }
    public Property? Property { get; set; }

    [MaxLength(300)] public string? Quelle { get; set; }   // z. B. URL/Seite des Klicks
    [MaxLength(64)] public string? IpAdresse { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
