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
        await SeedLegalPagesAsync(db);
        await SeedBlogPostsAsync(db);
        await SeedPropertiesAsync(db);
        await SeedHomeSectionsAsync(db);
        await db.SaveChangesAsync();

        // Einmalige Inhalts-Aktualisierung auf den freigegebenen Entwurf (Version 2).
        await RefreshDesignContentAsync(db);
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
        Set("contact.cityShort", "Velbert");
        Set("contact.phone", "0177 – 838 78 98");
        Set("contact.email", "andrea.krueger@baudorf.de");
        Set("contact.hours", "Mo–Fr · 09:00–18:00 Uhr");
        Set("brand.slogan", "Still, wirkungsvoll, mit Stil.");
        Set("brand.claim", "Diskret. Exklusiv. Direkt.");
        Set("footer.intro", "Off-Market-Immobilien aus Velbert. Diskrete Vermarktung anspruchsvoller Wohn-, Gewerbe- und Kapitalanlageobjekte — seit 1994.");
        Set("cookie.title", "Wir schätzen Ihre Privatsphäre");
        Set("cookie.text", "Wir verwenden Cookies, um Ihr Surferlebnis zu verbessern, personalisierte Anzeigen oder Inhalte einzusetzen und unseren Datenverkehr zu analysieren. Wenn Sie auf „Alle akzeptieren\" klicken, stimmen Sie der Anwendung von Cookies zu.");
        Set("whatsapp.number", "4901778387898", "WhatsApp-Nummer im internationalen Format ohne + und Leerzeichen.");
        Set("whatsapp.enabled", "true", "Schwebenden WhatsApp-Button anzeigen (true/false).");
        Set("whatsapp.message", "Hallo Baudorf, ich interessiere mich für Ihre Immobilien.", "Vorausgefüllte WhatsApp-Nachricht.");
    }

    private static async Task SeedLegalPagesAsync(ApplicationDbContext db)
    {
        // Idempotent pro Slug: fehlende Rechtsseiten werden mit dem bisherigen Inhalt angelegt.
        var vorhanden = await db.LegalPages.Select(l => l.Slug).ToListAsync();
        void Add(string slug, string titel, string body)
        {
            if (!vorhanden.Contains(slug))
                db.LegalPages.Add(new LegalPage { Slug = slug, Titel = titel, Overline = "Rechtliches", BodyHtml = body.Trim() });
        }

        Add("impressum", "Impressum", """
        <h2>Angaben gemäß § 5 DDG</h2>
        <p>
            <strong>Baudorf Immobilien GmbH</strong><br />
            Auf der Egge 68<br />
            42555 Velbert<br />
            Deutschland
        </p>
        <h2>Vertreten durch</h2>
        <p>Andrea Krüger (Geschäftsführerin)</p>
        <h2>Kontakt</h2>
        <p>
            Telefon: <a href="tel:+4901778387898">0177 – 838 78 98</a><br />
            E-Mail: <a href="mailto:andrea.krueger@baudorf.de">andrea.krueger@baudorf.de</a>
        </p>
        <h2>Registereintrag</h2>
        <p>
            Eingetragen im Handelsregister.<br />
            Registergericht: Amtsgericht Wuppertal<br />
            Registernummer: HRB &lt;wird ergänzt&gt;
        </p>
        <h2>Umsatzsteuer-ID</h2>
        <p>Umsatzsteuer-Identifikationsnummer gemäß § 27a UStG: &lt;wird ergänzt&gt;</p>
        <h2>Aufsichtsbehörde / Erlaubnis nach § 34c GewO</h2>
        <p>Zuständige Aufsichtsbehörde für die Maklertätigkeit: Stadt Velbert.</p>
        <h2>Verantwortlich für den Inhalt nach § 18 Abs. 2 MStV</h2>
        <p>Andrea Krüger · Auf der Egge 68 · 42555 Velbert</p>
        <h2>Haftung für Inhalte</h2>
        <p>Als Diensteanbieter sind wir gemäß § 7 Abs. 1 DDG für eigene Inhalte auf diesen Seiten nach den allgemeinen Gesetzen verantwortlich. Nach §§ 8 bis 10 DDG sind wir als Diensteanbieter jedoch nicht verpflichtet, übermittelte oder gespeicherte fremde Informationen zu überwachen.</p>
        <h2>Haftung für Links</h2>
        <p>Unser Angebot enthält ggf. Links zu externen Websites Dritter, auf deren Inhalte wir keinen Einfluss haben. Für die Inhalte der verlinkten Seiten ist stets der jeweilige Anbieter oder Betreiber verantwortlich.</p>
        <h2>Urheberrecht</h2>
        <p>Die durch die Seitenbetreiber erstellten Inhalte und Werke auf diesen Seiten unterliegen dem deutschen Urheberrecht.</p>
        <p style="margin-top:2rem;color:#8a847d;font-size:13px">Hinweis: Mit &lt;…&gt; markierte Angaben sind vor Veröffentlichung durch die offiziellen Registerdaten zu ergänzen.</p>
        """);

        Add("datenschutz", "Datenschutzerklärung", """
        <h2>1. Verantwortlicher</h2>
        <p>
            Verantwortlich für die Datenverarbeitung auf dieser Website ist:<br />
            <strong>Baudorf Immobilien GmbH</strong>, Auf der Egge 68, 42555 Velbert.<br />
            E-Mail: <a href="mailto:andrea.krueger@baudorf.de">andrea.krueger@baudorf.de</a>
        </p>
        <h2>2. Allgemeines zur Datenverarbeitung</h2>
        <p>Wir verarbeiten personenbezogene Daten unserer Nutzer grundsätzlich nur, soweit dies zur Bereitstellung einer funktionsfähigen Website sowie unserer Inhalte und Leistungen erforderlich ist. Die Verarbeitung erfolgt auf Grundlage der DSGVO.</p>
        <h2>3. Kontakt- und Anfrageformular</h2>
        <p>Wenn Sie uns über das Kontaktformular Anfragen zukommen lassen, werden Ihre Angaben (Vor- und Nachname, E-Mail, Telefon, Anliegen) zwecks Bearbeitung der Anfrage und für den Fall von Anschlussfragen bei uns gespeichert. Rechtsgrundlage ist Ihre Einwilligung (Art. 6 Abs. 1 lit. a DSGVO) sowie unser berechtigtes Interesse bzw. die Anbahnung eines Vertragsverhältnisses (Art. 6 Abs. 1 lit. b und f DSGVO).</p>
        <h2>4. Benutzerkonto (Off-Market-Zugang)</h2>
        <p>Für den Zugang zu vertraulichen Off-Market-Objekten können Sie ein Benutzerkonto anlegen. Hierbei verarbeiten wir Ihre E-Mail-Adresse und Anmeldedaten. Zur Sicherheit protokollieren wir Anmeldevorgänge (Zeitpunkt, IP-Adresse) sowie aufgerufene Objekte, um Missbrauch vorzubeugen und unser Angebot zu verbessern. Rechtsgrundlage ist Art. 6 Abs. 1 lit. b und f DSGVO.</p>
        <h2>5. Server-Logfiles &amp; Hosting</h2>
        <p>Beim Aufruf der Website werden technisch notwendige Daten (IP-Adresse, Datum/Uhrzeit, User-Agent) verarbeitet. Diese Verarbeitung ist zur sicheren Bereitstellung der Website erforderlich (Art. 6 Abs. 1 lit. f DSGVO).</p>
        <h2>6. Cookies</h2>
        <p>Wir setzen technisch notwendige Cookies zur Sitzungsverwaltung und Sicherheit ein. Nicht notwendige Cookies werden nur mit Ihrer Einwilligung gesetzt.</p>
        <h2>7. Ihre Rechte</h2>
        <p>Sie haben das Recht auf Auskunft, Berichtigung, Löschung, Einschränkung der Verarbeitung, Datenübertragbarkeit sowie Widerspruch. Eine erteilte Einwilligung können Sie jederzeit mit Wirkung für die Zukunft widerrufen. Zudem steht Ihnen ein Beschwerderecht bei einer Aufsichtsbehörde zu.</p>
        <h2>8. Speicherdauer</h2>
        <p>Wir speichern personenbezogene Daten nur so lange, wie es für die genannten Zwecke erforderlich ist oder gesetzliche Aufbewahrungsfristen dies vorschreiben.</p>
        <h2>9. Kontakt zum Datenschutz</h2>
        <p>Bei Fragen zum Datenschutz erreichen Sie uns unter <a href="mailto:andrea.krueger@baudorf.de">andrea.krueger@baudorf.de</a>.</p>
        """);

        Add("agb", "AGB", """
        <h2>§ 1 Geltungsbereich</h2>
        <p>Diese Allgemeinen Geschäftsbedingungen gelten für alle Maklerleistungen der Baudorf Immobilien GmbH (nachfolgend „Makler") gegenüber ihren Auftraggebern und Interessenten.</p>
        <h2>§ 2 Leistungen des Maklers</h2>
        <p>Der Makler erbringt Nachweis- und/oder Vermittlungsleistungen für Immobilien. Ein Anspruch auf einen bestimmten Vermarktungserfolg besteht nicht. Off-Market-Objekte werden ausschließlich vorgemerkten, geprüften Interessenten zugänglich gemacht.</p>
        <h2>§ 3 Provision</h2>
        <p>Mit dem Zustandekommen eines Hauptvertrages (Kauf-, Miet- oder vergleichbarer Vertrag) infolge des Nachweises oder der Vermittlung des Maklers wird die vereinbarte Provision fällig. Die Höhe richtet sich nach der jeweiligen Vereinbarung und den gesetzlichen Vorgaben.</p>
        <h2>§ 4 Vertraulichkeit</h2>
        <p>Sämtliche überlassenen Objektinformationen sind vertraulich zu behandeln und dürfen ohne Zustimmung des Maklers nicht an Dritte weitergegeben werden. Bei Verstoß behält sich der Makler Schadensersatzansprüche vor.</p>
        <h2>§ 5 Haftung</h2>
        <p>Angaben zu Objekten beruhen auf Informationen des Eigentümers und werden ohne Gewähr weitergegeben. Eine Haftung des Maklers für Richtigkeit und Vollständigkeit besteht nur bei Vorsatz und grober Fahrlässigkeit.</p>
        <h2>§ 6 Widerrufsrecht für Verbraucher</h2>
        <p>Verbrauchern steht bei im Fernabsatz geschlossenen Verträgen ein gesetzliches Widerrufsrecht zu. Die Einzelheiten ergeben sich aus der gesondert erteilten Widerrufsbelehrung.</p>
        <h2>§ 7 Schlussbestimmungen</h2>
        <p>Es gilt das Recht der Bundesrepublik Deutschland. Sollten einzelne Bestimmungen unwirksam sein, bleibt die Wirksamkeit der übrigen Bestimmungen unberührt.</p>
        <p style="margin-top:2rem;color:#8a847d;font-size:13px">Hinweis: Diese AGB sind eine allgemeine Vorlage und vor produktivem Einsatz rechtlich zu prüfen.</p>
        """);
    }

    private static async Task SeedBlogPostsAsync(ApplicationDbContext db)
    {
        // Idempotent pro Slug: Inhalte aus dem freigegebenen Entwurf (Markt & Insights).
        var vorhanden = await db.BlogPosts.Select(b => b.Slug).ToListAsync();
        void Add(string slug, string titel, string kategorie, string cover, DateTime datum, string excerpt, string body)
        {
            if (!vorhanden.Contains(slug))
            {
                db.BlogPosts.Add(new BlogPost
                {
                    Slug = slug,
                    Titel = titel,
                    Kategorie = kategorie,
                    CoverUrl = cover,
                    Excerpt = excerpt,
                    Body = body.Trim(),
                    IstVeroeffentlicht = true,
                    PublishedAt = datum,
                    MetaDescription = excerpt
                });
            }
        }

        Add("off-market-2026-warum-die-besten-objekte-nie-inseriert-werden",
            "Off-Market 2026 — warum die besten Objekte nie inseriert werden.",
            "Markt", "/img/design/obj1.jpg", new DateTime(2026, 5, 14),
            "Die attraktivsten Immobilien wechseln den Eigentümer, lange bevor ein Exposé entsteht. Warum Diskretion zum entscheidenden Wettbewerbsvorteil wird.",
            """
            <p>Wer 2026 hochwertige Wohn-, Gewerbe- oder Kapitalanlageobjekte sucht, findet die interessantesten Gelegenheiten nicht auf den großen Portalen. Sie werden gar nicht erst öffentlich angeboten. Off-Market ist längst kein Nischenphänomen mehr, sondern der bevorzugte Weg für Eigentümer, die Wert auf Diskretion, Geschwindigkeit und qualifizierte Käufer legen.</p>
            <h2>Diskretion schützt Wert</h2>
            <p>Ein öffentlich gelistetes Objekt, das über Monate sichtbar bleibt, verliert an Attraktivität — der Markt liest jede Preisanpassung mit. Die stille Vermarktung vermeidet genau das: Sie spricht ausschließlich vorgemerkte, geprüfte Interessenten an und bewahrt so die Verhandlungsposition beider Seiten.</p>
            <h2>Warum Netzwerke entscheiden</h2>
            <p>Off-Market funktioniert über Vertrauen. Eigentümer geben sensible Informationen nur an Partner weiter, die deren Tragweite verstehen und einen belastbaren Käuferkreis mitbringen. Für Investoren bedeutet das: Der Zugang zu den besten Objekten führt nicht über Reichweite, sondern über die richtige Adresse.</p>
            <p>Genau hier setzen wir an — vertraulich, fundiert und auf Augenhöhe mit Family Offices und institutionellen Investoren.</p>
            """);

        Add("faktor-und-rendite-richtig-lesen-leitfaden-fuer-kapitalanleger",
            "Faktor & Rendite richtig lesen — ein Leitfaden für Kapitalanleger.",
            "Investment", "/img/design/obj5.jpg", new DateTime(2026, 4, 28),
            "Der Kaufpreisfaktor ist die meistgenannte und am häufigsten missverstandene Kennzahl am Immobilienmarkt. Was wirklich dahintersteckt.",
            """
            <p>„Das Objekt geht zum 22-Fachen" — kaum eine Angabe fällt bei Kapitalanlagen so oft wie der Faktor. Doch die Zahl allein sagt wenig, solange man nicht weiß, worauf sie sich bezieht und welche Annahmen ihr zugrunde liegen.</p>
            <h2>Faktor ist nicht gleich Rendite</h2>
            <p>Der Kaufpreisfaktor beschreibt das Verhältnis von Kaufpreis zur Jahresnettomiete. Die Bruttorendite ist sein Kehrwert. Entscheidend für die tatsächliche Verzinsung ist jedoch die Nettorendite — nach Bewirtschaftungskosten, Instandhaltung und Mietausfallrisiko.</p>
            <h2>Worauf es wirklich ankommt</h2>
            <p>Lage, Mietermix, Restnutzungsdauer und Entwicklungspotenzial bestimmen, ob ein scheinbar teurer Faktor günstig oder ein günstiger Faktor teuer ist. Ein voll vermietetes Objekt in stabiler Lage rechtfertigt einen höheren Faktor als ein vermeintliches Schnäppchen mit Sanierungsstau.</p>
            <p>Wir ordnen jede Kennzahl in ihren Kontext ein — damit aus einer Zahl eine fundierte Entscheidung wird.</p>
            """);

        Add("kfw-40-qng-was-neubau-portfolios-heute-wirklich-wert-macht",
            "KfW 40 / QNG — was Neubau-Portfolios heute wirklich wert macht.",
            "Neubau", "/img/design/hero2.jpg", new DateTime(2026, 4, 9),
            "Energiestandard und Nachhaltigkeitszertifizierung sind 2026 keine Kür mehr, sondern werttreibende Faktoren. Ein Überblick für Bestandshalter und Entwickler.",
            """
            <p>Die Anforderungen an Neubauten haben sich grundlegend verschoben. Was vor wenigen Jahren als ambitioniert galt, ist heute Marktstandard — und entscheidet zunehmend über Finanzierbarkeit, Mietniveau und Wiederverkaufswert.</p>
            <h2>KfW 40 als Eintrittskarte</h2>
            <p>Der Effizienzhausstandard KfW 40 sichert nicht nur günstigere Finanzierungskonditionen, sondern auch eine bessere Vermietbarkeit. Energieeffiziente Objekte sprechen eine zahlungskräftige, zukunftsorientierte Mieterschaft an und reduzieren das Risiko künftiger Nachrüstpflichten.</p>
            <h2>QNG — Nachhaltigkeit wird messbar</h2>
            <p>Das Qualitätssiegel Nachhaltiges Gebäude (QNG) macht ökologische und soziale Qualität nachweisbar. Für institutionelle Investoren mit ESG-Vorgaben ist es oft die Voraussetzung für einen Ankauf — und damit ein konkreter Werttreiber im Portfolio.</p>
            <p>Wir bewerten Neubau-Portfolios mit Blick auf genau diese Faktoren — vorausschauend statt rückwärtsgewandt.</p>
            """);
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

    private const string DesignVersion = "4";

    /// <summary>
    /// Einmalige Aktualisierung der Startseiten-Inhalte auf den freigegebenen Entwurf.
    /// Setzt Abschnitte + Team-Texte neu (versioniert über SiteSetting), ohne bei jedem Start zu überschreiben.
    /// </summary>
    private static async Task RefreshDesignContentAsync(ApplicationDbContext db)
    {
        var marker = await db.SiteSettings.FirstOrDefaultAsync(s => s.Key == "design.contentVersion");
        if (marker?.Value == DesignVersion) return;

        // Abschnitte neu aufbauen (löscht auch Items via Cascade).
        var alt = await db.HomeSections.ToListAsync();
        db.HomeSections.RemoveRange(alt);
        await db.SaveChangesAsync();

        HomeSection Sec(string key, int ord, string? ov, string? ti, string? tx,
            string? cta = null, string? ctaUrl = null, string? cta2 = null, string? cta2Url = null,
            params (string? t, string? x)[] items)
        {
            var s = new HomeSection { Key = key, Reihenfolge = ord, Overline = ov, Titel = ti, Text = tx,
                CtaText = cta, CtaUrl = ctaUrl, Cta2Text = cta2, Cta2Url = cta2Url, IstSichtbar = true };
            for (var i = 0; i < items.Length; i++)
                s.Items.Add(new HomeSectionItem { Titel = items[i].t, Text = items[i].x, Reihenfolge = i });
            return s;
        }

        var sektionen = new List<HomeSection>
        {
            Sec("hero", 1, "Off-Market-Immobilien · NRW · seit 1994",
                "Diskret. <em>Exklusiv.</em> Direkt.",
                "Wir vermitteln Wohn-, Gewerbe- und Kapitalanlageobjekte abseits des öffentlichen Marktes — vertraulich, fundiert und auf Augenhöhe mit Family Offices und institutionellen Investoren.",
                "Objekte anfragen", "/Kontakt", "Über uns", "/#ueber"),
            Sec("philosophie", 2, "Philosophie",
                "Wir vermitteln nicht.<br><em>Wir verbinden.</em>",
                "Seit 1994 begleiten wir Eigentümer und Investoren bei Transaktionen, die nicht für die Öffentlichkeit bestimmt sind. Was als Architekturbüro begann, ist heute ein spezialisiertes Maklerhaus für die diskrete Vermarktung anspruchsvoller Immobilien in NRW und darüber hinaus.\n\nStille ist kein Mangel an Lautstärke — sie ist eine Haltung. <strong>Still, wirkungsvoll, mit Stil.</strong>"),
            Sec("leistungen", 3, "Leistungen", "Was wir für Sie tun",
                "Fünf Disziplinen, ein Anspruch: die richtige Immobilie für den richtigen Investor — diskret zusammengeführt.",
                null, null, null, null,
                ("Stille Vermarktung", "Diskret, ohne Inserat — die Immobilie wird vertraut, nicht ausgeschrieben."),
                ("Klassische Vermarktung", "Wenn Reichweite zählt: professionell aufbereitet und zielgerichtet platziert."),
                ("Immobilienbewertung", "Fundierte Markt- und Ertragswertanalyse als Basis Ihrer Entscheidung."),
                ("Kaufbegleitung", "Von der Prüfung bis zum Notar — an Ihrer Seite, Schritt für Schritt."),
                ("Dienstleisternetzwerk", "Architekten, Gutachter, Finanzierer — kuratiert und verlässlich.")),
            Sec("statement", 4, "Off-Market · NRW",
                "Die besten Objekte werden nie inseriert.<br><em>Sie werden vertraut.</em>",
                "Über 480 Mio. € begleitetes Transaktionsvolumen — abseits des öffentlichen Marktes, auf Augenhöhe mit professionellen Investoren.",
                "Off-Market-Zugang anfragen", "/Kontakt"),
            Sec("vorgehen", 5, "Unser Vorgehen", "Vom ersten Gespräch <em>bis zum Notar</em>", null,
                null, null, null, null,
                ("Erstgespräch", "Vertraulich und unverbindlich klären wir Ihre Ziele und Möglichkeiten."),
                ("Objektauswahl", "Kuratierte Vorauswahl — nur Relevantes, diskret aufbereitet."),
                ("Prüfung & Begleitung", "Fundierte Bewertung und persönliche Begleitung in Ihrem Tempo."),
                ("Abschluss", "Strukturiert bis zum Notartermin — verbindlich und sicher.")),
            Sec("klientel", 6, "Unsere Klientel", "Mit wem wir <em>arbeiten</em>",
                "Unsere Mandanten erwarten Substanz statt Show. Wir bewegen uns dort, wo Vertraulichkeit zählt und Entscheidungen mit Bedacht getroffen werden.",
                null, null, null, null,
                ("Family Offices", null), ("Institutionelle Investoren", null),
                ("Wohnungsgesellschaften", null), ("Bauträger", null), ("Projektentwickler", null)),
            Sec("ueber-uns", 7, "Über uns", "Andrea Krüger", null),
            Sec("zahlen", 8, null, null, null, null, null, null, null,
                ("30<span class=\"hx-unit\">+</span>", "Jahre Erfahrung — seit 1994 am Markt"),
                ("480<span class=\"hx-unit\"> Mio. €</span>", "Transaktionsvolumen begleitet"),
                ("100<span class=\"hx-unit\"> %</span>", "Diskretion — Off-Market als Kernsegment"),
                ("24<span class=\"hx-unit\"> h</span>", "Reaktionszeit auf Anfragen i. d. R.")),
            Sec("testimonial", 9, null,
                "Family Office · Düsseldorf",
                "Baudorf hat unsere Transaktion mit einer Diskretion und Sachkenntnis begleitet, wie wir sie selten erlebt haben. Verbindlich, ruhig, auf den Punkt."),
            Sec("tippgeber", 10, "Tippgeber", "Empfehlen Sie uns — und <em>profitieren Sie.</em>",
                "Sie kennen einen Eigentümer mit Verkaufsabsicht? Vermitteln Sie den Kontakt — bei erfolgreichem Abschluss honorieren wir Ihren Hinweis.",
                "Tipp einreichen", "/Kontakt"),
            Sec("karriere", 11, "Karriere", "Werden Sie Teil von Baudorf.",
                "Wir suchen Menschen mit Gespür für Immobilien und Diskretion. Initiativbewerbungen sind jederzeit willkommen.",
                "Initiativ bewerben", "/Kontakt"),
            Sec("insights", 12, "Markt & Insights", "Expertenwissen für <em>kluge Investoren</em>", null),
            Sec("kontakt", 13, "Kontakt", "Sprechen wir — <em>diskret.</em>",
                "Ob Verkauf, Investment oder Bewertung — wir melden uns persönlich und vertraulich, in der Regel innerhalb von 24 Stunden.",
                "Anfrage senden", "/Kontakt")
        };

        // Hintergrundbilder aus dem Entwurf.
        void SetBild(string key, string url)
        {
            var s = sektionen.FirstOrDefault(x => x.Key == key);
            if (s is not null) s.BildUrl = url;
        }
        SetBild("philosophie", "/img/design/philo.jpg");
        SetBild("statement", "/img/design/hero3.jpg");
        SetBild("kontakt", "/img/design/kontakt.jpg");

        db.HomeSections.AddRange(sektionen);

        // Objekt-Titelbilder aus dem Entwurf (nur wenn das Objekt noch kein Bild hat).
        var objektBilder = new Dictionary<string, string>
        {
            ["faktor-20-7-40-wohneinheiten-kfw40-qng"] = "/img/design/obj1.jpg",
            ["neubau-senioreneinrichtung-80-plaetze-herne"] = "/img/design/obj2.jpg",
            ["wohn-geschaeftshaus-wuppertal-zentrum"] = "/img/design/obj3.jpg",
            ["exklusive-wohnanlage-24-einheiten-duesseldorf"] = "/img/design/obj4.jpg",
            ["baugrundstueck-projektentwicklung-essen"] = "/img/design/obj5.jpg",
            ["ferienresort-mittelmeer-bestandsobjekt"] = "/img/design/obj6.jpg"
        };
        var objekte = await db.Properties.Include(p => p.Medien)
            .Where(p => objektBilder.Keys.Contains(p.Slug)).ToListAsync();
        foreach (var p in objekte)
        {
            p.IstFeatured = true; // 6er-Mosaik auf der Startseite wie im Entwurf
            if (!p.Medien.Any())
                p.Medien.Add(new PropertyMedia
                {
                    Typ = MediaType.Image, Url = objektBilder[p.Slug], IstCover = true, Reihenfolge = 0, Alt = p.Titel
                });
        }

        // Team-Texte auf Entwurf setzen.
        var team = await db.TeamMembers.OrderBy(t => t.Reihenfolge).ToListAsync();
        var andrea = team.FirstOrDefault(t => t.Name.Contains("Andrea"));
        if (andrea is not null)
        {
            andrea.Rolle = "Geschäftsführerin · Immobilienmaklerin · Dipl.-Ing. Architektin";
            andrea.Bio = "1994 als Architekturbüro gegründet, 2018 in die Baudorf Immobilien GmbH überführt: Aus dem geschulten Blick für Substanz und Gestaltung wurde ein Maklerhaus, das Immobilien nicht nur vermittelt, sondern versteht.\n\nAls Architektin liest Andrea Krüger Gebäude — als Maklerin liest sie Menschen. Diese Verbindung macht den Unterschied, wenn es um diskrete, anspruchsvolle Transaktionen geht.";
            andrea.FotoUrl = "/img/team/andrea.jpg";
        }
        var ayla = team.FirstOrDefault(t => t.Name.Contains("Ayla"));
        if (ayla is not null)
        {
            ayla.Rolle = "Immobilien-Spürnase";
            ayla.Bio = "Unsere Bürohündin begleitet jeden Termin mit untrüglichem Gespür — für gute Lagen, ehrliche Menschen und den passenden Moment für eine Pause. Sie gehört einfach dazu.";
            ayla.FotoUrl = "/img/team/ayla.jpg";
        }

        if (marker is null)
            db.SiteSettings.Add(new SiteSetting { Key = "design.contentVersion", Value = DesignVersion });
        else
            marker.Value = DesignVersion;
    }
}
