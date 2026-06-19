using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Data;

/// <summary>Migration + Seed beim Start (Rollen, Admin, Team, Demo-Objekte, Einstellungen).</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));

        var cfg = sp.GetRequiredService<IConfiguration>();
        var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = sp.GetRequiredService<ILogger<ApplicationDbContext>>();
        var adminEmail = cfg["Seed:AdminEmail"] ?? "andrea.krueger@baudorf.de";
        var adminPwd = cfg["Seed:AdminPassword"];

        // Kein Default-Passwort im Quellcode: fehlt es, wird der Admin nicht angelegt.
        if (string.IsNullOrWhiteSpace(adminPwd))
        {
            logger.LogWarning("Seed:AdminPassword fehlt — Admin-Benutzer wird nicht angelegt. " +
                              "Setze ihn in appsettings.Development.json oder via Umgebungsvariable.");
        }
        else if (await userMgr.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                AnzeigeName = "Andrea Krüger",
                IstFreigegeben = true,
                FreigegebenAm = DateTime.UtcNow
            };
            var res = await userMgr.CreateAsync(admin, adminPwd);
            if (res.Succeeded)
                await userMgr.AddToRoleAsync(admin, Roles.Admin);
        }

        await SeedTeamAsync(db);
        await SeedSettingsAsync(db);
        await SeedPropertiesAsync(db);
        await SeedHomeSectionsAsync(db);

        await db.SaveChangesAsync();
    }

    private static async Task SeedTeamAsync(ApplicationDbContext db)
    {
        if (await db.TeamMembers.AnyAsync()) return;

        db.TeamMembers.AddRange(
            new TeamMember
            {
                Name = "Andrea Krüger",
                Rolle = "Geschäftsführerin · Immobilienmaklerin · Dipl.-Ing. Architektin",
                Bio = "Seit 1994 in der Immobilienwelt zuhause — von der Architektur zur stillen Vermarktung. " +
                      "Diskretion, Verbindlichkeit und ein Gespür für besondere Objekte.",
                FotoUrl = "/img/team/andrea.jpg",
                Reihenfolge = 1
            },
            new TeamMember
            {
                Name = "Ayla",
                Rolle = "Immobilien-Spürnase",
                Bio = "Unsere Bürohündin und gute Seele des Hauses — immer dabei, immer charmant.",
                FotoUrl = "/img/team/ayla.jpg",
                Reihenfolge = 2
            });
    }

    private static async Task SeedSettingsAsync(ApplicationDbContext db)
    {
        // Idempotent pro Key: fehlende Einstellungen werden ergänzt, vorhandene bleiben.
        var vorhanden = await db.SiteSettings.Select(s => s.Key).ToListAsync();
        void Set(string key, string value, string? desc = null)
        {
            if (!vorhanden.Contains(key))
                db.SiteSettings.Add(new SiteSetting { Key = key, Value = value, Beschreibung = desc });
        }

        Set("contact.company", "Baudorf Immobilien GmbH");
        Set("contact.street", "Auf der Egge 68");
        Set("contact.city", "42555 Velbert");
        Set("contact.phone", "0177 – 838 78 98");
        Set("contact.email", "andrea.krueger@baudorf.de");
        Set("contact.hours", "Mo–Fr 09:00–18:00");
        Set("brand.slogan", "Still, wirkungsvoll, mit Stil.");
        Set("brand.claim", "Diskret. Exklusiv. Direkt.");
        Set("whatsapp.number", "4901778387898", "WhatsApp-Nummer im internationalen Format ohne + und Leerzeichen.");
        Set("whatsapp.enabled", "true", "Schwebenden WhatsApp-Button anzeigen (true/false).");
        Set("whatsapp.message", "Hallo Baudorf, ich interessiere mich für Ihre Immobilien.", "Vorausgefüllte WhatsApp-Nachricht.");
    }

    private static async Task SeedPropertiesAsync(ApplicationDbContext db)
    {
        // Idempotent pro Slug: neue Objekte werden bei jedem Start ergänzt, vorhandene bleiben unberührt.
        var saat = new[]
        {
            new Property
            {
                Titel = "Faktor 20,7 — 40 Wohneinheiten · KfW 40 / QNG",
                Slug = "faktor-20-7-40-wohneinheiten-kfw40-qng",
                Art = PropertyKind.Kapitalanlage,
                Status = PropertyStatus.OffMarket,
                Region = "NRW",
                Wohnflaeche = 2809,
                Grundstuecksflaeche = 2940,
                Baujahr = 2027,
                Einheiten = 40,
                Faktor = 20.7m,
                RenditeProzent = 4.8m,
                Kaufpreis = 9_750_000m,
                Zustand = "Neubau",
                Energieklasse = "A+",
                Beschreibung = "Hochwertiges Neubauprojekt mit 40 Wohneinheiten nach KfW 40 / QNG-Standard.\n" +
                               "Nachhaltige Bauweise, attraktiver Faktor, langfristige Wertstabilität.\n" +
                               "Vollständige Unterlagen für vorgemerkte Investoren nach Freigabe.",
                IstOffMarket = true,
                IstFeatured = true,
                IstVeroeffentlicht = true,
                MetaTitle = "Off-Market Kapitalanlage NRW — Faktor 20,7"
            },
            new Property
            {
                Titel = "Neubau-Senioreneinrichtung — 80 stationäre Plätze",
                Slug = "neubau-senioreneinrichtung-80-plaetze-herne",
                Art = PropertyKind.Investment,
                Status = PropertyStatus.Verfuegbar,
                Region = "Herne, NRW",
                Grundstuecksflaeche = 7565,
                Baujahr = 2023,
                Einheiten = 80,
                Faktor = 19.2m,
                Kaufpreis = 18_400_000m,
                Zustand = "Neubau",
                Energieklasse = "A",
                Beschreibung = "Moderne Senioreneinrichtung mit 80 stationären Plätzen.\n" +
                               "Langfristiger Pachtvertrag mit etabliertem Betreiber — institutionelle Kapitalanlage.",
                IstOffMarket = false,
                IstFeatured = true,
                IstVeroeffentlicht = true,
                MetaTitle = "Pflegeimmobilie Herne — 80 Plätze"
            },
            new Property
            {
                Titel = "Wohn- und Geschäftshaus in zentraler Lage",
                Slug = "wohn-geschaeftshaus-wuppertal-zentrum",
                Art = PropertyKind.Gewerbe,
                Status = PropertyStatus.Verfuegbar,
                Region = "Wuppertal, NRW",
                Wohnflaeche = 1240,
                Grundstuecksflaeche = 680,
                Baujahr = 1998,
                Einheiten = 14,
                Faktor = 16.5m,
                RenditeProzent = 5.6m,
                Kaufpreis = 3_950_000m,
                Zustand = "Gepflegt",
                Energieklasse = "C",
                Beschreibung = "Etabliertes Wohn- und Geschäftshaus mit stabilem Mietermix in Innenstadtlage.\n" +
                               "Solide Bestandsimmobilie mit Entwicklungspotenzial.",
                IstOffMarket = false,
                IstFeatured = false,
                IstVeroeffentlicht = true
            },
            new Property
            {
                Titel = "Exklusive Wohnanlage — 24 Einheiten",
                Slug = "exklusive-wohnanlage-24-einheiten-duesseldorf",
                Art = PropertyKind.Wohnimmobilie,
                Status = PropertyStatus.Reserviert,
                Region = "Düsseldorf, NRW",
                Wohnflaeche = 2150,
                Grundstuecksflaeche = 3100,
                Baujahr = 2021,
                Einheiten = 24,
                Faktor = 24.0m,
                Kaufpreis = 14_200_000m,
                Zustand = "Neuwertig",
                Energieklasse = "A+",
                Beschreibung = "Gehobene Wohnanlage in begehrter Lage — hochwertige Ausstattung, voll vermietet.",
                IstOffMarket = true,
                IstFeatured = false,
                IstVeroeffentlicht = true
            },
            new Property
            {
                Titel = "Baugrundstück mit Projektentwicklung",
                Slug = "baugrundstueck-projektentwicklung-essen",
                Art = PropertyKind.Grundstueck,
                Status = PropertyStatus.Verfuegbar,
                Region = "Essen, NRW",
                Grundstuecksflaeche = 4800,
                Beschreibung = "Erschlossenes Baugrundstück mit Baugenehmigung für Wohnbebauung.\n" +
                               "Ideal für Bauträger und Projektentwickler.",
                Kaufpreis = 2_600_000m,
                Zustand = "Erschlossen",
                IstOffMarket = false,
                IstFeatured = false,
                IstVeroeffentlicht = true
            },
            new Property
            {
                Titel = "Ferienresort am Mittelmeer — Bestandsobjekt",
                Slug = "ferienresort-mittelmeer-bestandsobjekt",
                Art = PropertyKind.Auslandsimmobilie,
                Status = PropertyStatus.OffMarket,
                Region = "Costa Blanca",
                Land = "Spanien",
                Wohnflaeche = 5400,
                Grundstuecksflaeche = 12000,
                Baujahr = 2016,
                Einheiten = 60,
                Faktor = 18.0m,
                RenditeProzent = 6.2m,
                Kaufpreis = 22_500_000m,
                Zustand = "Sehr gut",
                Energieklasse = "B",
                Beschreibung = "Etabliertes Ferienresort in erster Linie — stabile Auslastung, internationales Klientel.\n" +
                               "Vertrauliche Vermarktung für vorgemerkte Investoren.",
                IstOffMarket = true,
                IstFeatured = false,
                IstVeroeffentlicht = true,
                MetaTitle = "Off-Market Ferienresort Spanien — Auslandsimmobilie"
            }
        };

        var vorhanden = await db.Properties.Select(p => p.Slug).ToListAsync();
        var neue = saat.Where(s => !vorhanden.Contains(s.Slug)).ToList();
        if (neue.Count > 0)
            db.Properties.AddRange(neue);
    }

    private static async Task SeedHomeSectionsAsync(ApplicationDbContext db)
    {
        // Idempotent pro Key: bestehende (admin-bearbeitete) Abschnitte bleiben unberührt.
        var vorhanden = await db.HomeSections.Select(s => s.Key).ToListAsync();

        HomeSection Sec(string key, int ord, string? overline, string? titel, string? text,
            string? cta = null, string? ctaUrl = null, string? cta2 = null, string? cta2Url = null,
            params (string? t, string? x)[] items)
        {
            var s = new HomeSection
            {
                Key = key, Reihenfolge = ord, Overline = overline, Titel = titel, Text = text,
                CtaText = cta, CtaUrl = ctaUrl, Cta2Text = cta2, Cta2Url = cta2Url, IstSichtbar = true
            };
            for (var i = 0; i < items.Length; i++)
                s.Items.Add(new HomeSectionItem { Titel = items[i].t, Text = items[i].x, Reihenfolge = i });
            return s;
        }

        var alle = new[]
        {
            Sec("hero", 1, "Off-Market-Immobilien · NRW · seit 1994",
                "Diskret. <em>Exklusiv.</em> Direkt.",
                "Die besten Objekte werden nie inseriert — sie werden vertraut. Wir verbinden außergewöhnliche Immobilien mit den richtigen Investoren. Still, wirkungsvoll, mit Stil.",
                "Objekte anfragen", "/Kontakt", "Über uns", "/#ueber-uns"),
            Sec("philosophie", 2, "Philosophie",
                "Wir vermitteln nicht. <em>Wir verbinden.</em>",
                "Stille ist kein Mangel an Lautstärke — sie ist eine Haltung. Diskretion, Verbindlichkeit und ein feines Gespür für besondere Objekte sind die Grundlage jeder Zusammenarbeit. Wir bewegen Werte, ohne Aufsehen zu erregen."),
            Sec("leistungen", 3, "Leistungen", "Was wir für Sie tun",
                "Fünf Disziplinen, ein Anspruch: die richtige Immobilie für den richtigen Investor — diskret zusammengeführt.",
                null, null, null, null,
                ("Stille Vermarktung", "Diskret, ohne Inserat — die Immobilie wird vertraut, nicht ausgeschrieben."),
                ("Klassische Vermarktung", "Wenn Reichweite zählt: professionell aufbereitet und zielgerichtet platziert."),
                ("Immobilienbewertung", "Fundierte Markt- und Ertragswertanalyse als Basis Ihrer Entscheidung."),
                ("Kaufbegleitung", "Von der Prüfung bis zum Notar — an Ihrer Seite, Schritt für Schritt."),
                ("Dienstleisternetzwerk", "Architekten, Gutachter, Finanzierer — kuratiert und verlässlich.")),
            Sec("vorgehen", 5, "Unser Vorgehen", "Vom ersten Gespräch bis zum Notar", null,
                null, null, null, null,
                ("Immobiliensuche", "Wir verstehen Ihr Profil und finden, was zu Ihnen passt."),
                ("Objektauswahl", "Kuratierte Vorauswahl — nur Relevantes, vertraulich aufbereitet."),
                ("Diskretes Vertrautmachen", "Persönliche Begleitung, ohne Öffentlichkeit, in Ihrem Tempo."),
                ("Transaktionsprozess", "Strukturiert bis zum Notartermin — verbindlich und sicher.")),
            Sec("klientel", 6, "Unsere Klientel", "Mit wem wir arbeiten", null,
                null, null, null, null,
                ("Family Offices", null), ("Institutionelle Investoren", null),
                ("Wohnungsgesellschaften", null), ("Bauträger", null), ("Projektentwickler", null)),
            Sec("ueber-uns", 7, "Über uns", "Menschen hinter Baudorf", null),
            Sec("zahlen", 8, null, null, null, null, null, null, null,
                ("1994", "Jahre Erfahrung — seit 1994 am Markt"),
                ("Still", "Unsere Methode der Vermarktung"),
                ("NRW", "Unser Fokusmarkt"),
                ("24 h", "Reaktionszeit auf Anfragen i. d. R.")),
            Sec("tippgeber", 9, "Tippgeber", "Empfehlen Sie uns — und <em>profitieren Sie.</em>",
                "Kennen Sie eine Immobilie oder einen Eigentümer? Reichen Sie Ihren Tipp diskret ein.",
                "Tipp einreichen", "/Kontakt"),
            Sec("kontakt", 10, "Kontakt", "Sprechen wir über Ihr <em>Vorhaben.</em>",
                "Alle Anfragen werden streng vertraulich behandelt. Wir melden uns persönlich und diskret — in der Regel innerhalb von 24 Stunden.",
                "Kontaktformular öffnen", "/Kontakt")
        };

        var neue = alle.Where(s => !vorhanden.Contains(s.Key)).ToList();
        if (neue.Count > 0)
            db.HomeSections.AddRange(neue);
    }
}
