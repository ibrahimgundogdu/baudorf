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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
