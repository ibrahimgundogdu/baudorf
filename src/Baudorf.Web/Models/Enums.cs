namespace Baudorf.Web.Models;

/// <summary>Objektart — Immobilien-Kategorien laut Sitemap.</summary>
public enum PropertyKind
{
    OffMarket = 0,
    Kapitalanlage = 1,
    Investment = 2,
    Gewerbe = 3,
    Wohnimmobilie = 4,
    Grundstueck = 5,
    Projektentwicklung = 6,
    Auslandsimmobilie = 7
}

/// <summary>Vermarktungsstatus eines Objekts.</summary>
public enum PropertyStatus
{
    OffMarket = 0,
    Verfuegbar = 1,
    Reserviert = 2,
    Verkauft = 3
}

/// <summary>Medientyp einer Property.</summary>
public enum MediaType
{
    Image = 0,
    Video = 1,
    VirtualTour = 2
}

/// <summary>Lead-Bearbeitungsstatus im Admin.</summary>
public enum LeadStatus
{
    Neu = 0,
    InBearbeitung = 1,
    Erledigt = 2
}

/// <summary>"Ich interessiere mich als …" — Kontaktformular.</summary>
public enum InterestType
{
    KaeuferPrivatinvestor = 0,
    KaeuferFamilyOffice = 1,
    KaeuferInstitutionell = 2,
    VerkaeuferBestandshalter = 3,
    VerkaeuferProjektentwickler = 4,
    Immobilienbewertung = 5,
    Kaufbegleitung = 6,
    Tippgeber = 7,
    Karriere = 8,
    Sonstiges = 9
}
