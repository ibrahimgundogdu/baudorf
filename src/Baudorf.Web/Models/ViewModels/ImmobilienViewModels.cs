using Baudorf.Web.Models.Entities;

namespace Baudorf.Web.Models.ViewModels;

/// <summary>Filter-/Sortier-Parameter der Immobilien-Liste (aus Query-String).</summary>
public class ImmobilienFilter
{
    public PropertyKind? Art { get; set; }
    public PropertyStatus? Status { get; set; }
    public string? Q { get; set; }            // Freitext (Titel/Region)
    public decimal? PreisMax { get; set; }
    public string Sort { get; set; } = "neu"; // neu | preis-auf | preis-ab | flaeche
    public int Page { get; set; } = 1;
}

public class ImmobilienListViewModel
{
    public IReadOnlyList<Property> Objekte { get; set; } = [];
    public ImmobilienFilter Filter { get; set; } = new();

    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }

    public bool HatVorige => Page > 1;
    public bool HatNaechste => Page < TotalPages;
}

public class PropertyDetailViewModel
{
    public Property Objekt { get; set; } = null!;
    public IReadOnlyList<Property> AehnlicheObjekte { get; set; } = [];

    /// <summary>Off-Market-Objekt UND Nutzer ist nicht freigegeben → Inhalte gesperrt.</summary>
    public bool IstGesperrt { get; set; }

    /// <summary>Nutzer ist angemeldet (aber evtl. noch nicht freigegeben).</summary>
    public bool IstAngemeldet { get; set; }
}
