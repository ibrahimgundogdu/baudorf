using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>
/// Ein in der Mediathek verwaltetes Medienobjekt (Bild). Wird beim Upload angelegt und
/// kann in Inhalten (WYSIWYG, Cover, Objekt-Bilder) wiederverwendet werden.
/// </summary>
public class MediaAsset
{
    public int Id { get; set; }

    /// <summary>Öffentliche URL (z. B. /uploads/abc.jpg).</summary>
    [Required, MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(260)]
    public string? FileName { get; set; }

    [MaxLength(120)]
    public string? ContentType { get; set; }

    public long SizeBytes { get; set; }

    [MaxLength(200)]
    public string? Alt { get; set; }

    [MaxLength(200)]
    public string? Titel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
