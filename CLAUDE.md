# Baudorf Immobilien GmbH — Web Sitesi & Admin Paneli

> Off-market lüks gayrimenkul platformu. Velbert (NRW) merkezli. Kaynak brief:
> `template/uploads/baudorf.md` (tam gereksinim listesi). Tasarım taslağı:
> `template/index.html` + `template/uploads/baudorf-FINAL (9).html`. Mevcut canlı site:
> https://baudorf.de (yalnızca içerik/yapı/hukuki metin kaynağı — tasarımı taklit etme).

## 1. Teknoloji Yığını (kilitli kararlar)

- **Backend:** .NET 10 (SDK 10.0.x), ASP.NET Core MVC (Razor). C#.
- **Yapı:** **Tek Web projesi + Areas** (`Areas/Admin`). Katmanlı çoklu proje DEĞİL.
  Klasör bazlı ayrım: `Models/Entities`, `Data`, `Services`, `Controllers`, `Areas/Admin`.
- **DB:** SQL Server + Entity Framework Core 10. **Code-First** + migration'lar + seed.
- **Auth:** ASP.NET Core Identity (off-market erişim kapısı + `Admin`/`Redakteur`/`Investor` rolleri).
- **Frontend:** Razor + **custom CSS + design token'lar** (`wwwroot/css/site.css`).
  Tailwind YOK. Etkileşim için **Alpine.js (CDN)** + IntersectionObserver. Ağır JS framework yok.
- **Medya depolama:** `IStorageService` soyutlaması arkasında **`wwwroot/uploads`** (yerel disk).
  İleride R2/S3 implementasyonu eklenebilir — interface'i bozmadan.
- **Mail:** `IEmailSender` soyutlaması (SMTP impl). İletişim formu + lead bildirimleri.
- **Deploy hedefi:** Ubuntu 24.04 + Nginx + systemd (Hetzner VPS) + Let's Encrypt TLS.
  README'ye adımlar yazılacak.
- **Dil/i18n:** Birincil **Almanca (de-DE)**. EN için resource altyapısı hazır, içerik DE öncelikli.

## 2. Marka & Tasarım Dili

**Karakter:** Diskret lüks. Sessiz güç. *"Still, wirkungsvoll, mit Stil."* /
*"Diskret. Exklusiv. Direkt."* Asla agresif değil — zarif, bol beyaz alan, premium.
Animasyon **ölçülü** (lüks = sükûnet); abartma.

**Renk paleti (CSS değişkenleri — `:root`):**
```
--gold:#C4A46B  --gold-lt:#D9BF8E  --gold-dk:#96773A
--black:#0C0C0C  --ink:#181614  --graph:#232120  --stone:#5C5550
--parch:#F5F2EC  --linen:#EDE8DF  --white:#FDFCFA
```
Bölüm ritmi: krem ↔ beyaz ↔ koyu mürekkep dönüşümlü. Altın **yalnızca vurgu** (asla baskın).

**Tipografi:** Inter (300/400/500/600 + italic). Başlıklar büyük, negatif `letter-spacing`,
italic altın vurgular (*Exklusiv*). Overline: küçük, geniş harf aralığı, uppercase, önünde altın çizgi.

**Marka varlıkları:** `template/assets/` içinde logolar/amblemler (`baudorf-logo*.png`,
`baudorf-emblem-*.png`). Daire+bina amblemi → bölüm arka planlarında düşük opaklıkta filigran/parallax.
Bu varlıklar `wwwroot/img/brand/` altına kopyalanacak.

## 3. Bilgi Mimarisi (Sitemap)

- **Startseite** (ana sayfa — §4'teki scroll deneyimi)
- **Immobilien**: Off-Market, Kapitalanlagen, Investments, Gewerbe, Wohn, Grundstücke/Projektentwicklungen, Auslandsimmobilien → liste + filtre + detay
- **Über uns**: Karriere, Tippgeber
- **Leistungen**: Stille Vermarktung, Klassische Vermarktung, Immobilienbewertung, Kaufbegleitung, Dienstleisternetzwerk
- **Kontakt**
- **Mein Konto** → Registrieren / Anmelden (off-market kapısı)
- **Hukuki:** Impressum, AGB, Datenschutzerklärung

## 4. Veri Modeli (code-first — ilk taslak)

Çekirdek entity'ler (`Models/Entities`):
- **Property** (Immobilie): slug, Titel, Objektart (enum), Status (Off-Market/Verfügbar/Reserviert/Verkauft),
  Standort/Region, Land, Wohnfläche, Grundstücksfläche, Baujahr, Zustand, Energieklasse, Einheiten,
  Faktor, Rendite, Kaufpreis (nullable → "auf Anfrage"), IstOffMarket, IstFeatured, IsPublished,
  Beschreibung (rich), SEO alanları (MetaTitle/MetaDescription), Lat/Lng (yaklaşık konum), CreatedAt.
- **PropertyMedia**: PropertyId, Typ (Image/Video/VirtualTour), Url, ThumbnailUrl, Reihenfolge, IstCover, Alt.
- **PropertyType / Location** referans verileri (gerekirse enum + lookup).
- **Lead** (Anfrage): Vorname, Nachname, Email, Telefon, InteresseTyp (enum), Nachricht, PropertyId?,
  Status (Neu/In Bearbeitung/Erledigt), Notiz, ZugewiesenAn, DSGVO onay, CreatedAt, IP.
- **BlogPost** (Insights): slug, Titel, Excerpt, Body, CoverUrl, Kategorie, Tags, PublishedAt, IsPublished, SEO.
- **TeamMember**: Name, Rolle, Bio, FotoUrl, Reihenfolge (Andrea + Ayla dahil).
- **TippgeberApplication**, **CareerApplication**: başvuru kayıtları.
- **Favorite** (Merkliste): UserId, PropertyId.
- **SiteSetting**: key/value (iletişim künyesi, sosyal linkler, SMTP, hero medya, hukuki metinler).
- **ApplicationUser : IdentityUser**: investor onay durumu (`IstFreigegeben`), AnzeigeName.

Off-market gating: `Property.IstOffMarket == true` → detaylar yalnızca giriş yapmış **ve**
`IstFreigegeben` (admin onaylı) kullanıcıya açık; aksi halde kilitli + kayıt çağrısı.

## 5. Kod Konvansiyonları

- Public arayüz/içerik **Almanca** (kullanıcıya görünen metin, alan etiketleri). Kod/yorum İngilizce/Türkçe karışık olabilir; entity property adları Almanca domain terimi (Wohnfläche → `Wohnflaeche`) tercih edilir, ASCII-safe.
- Nullable reference types **açık**. `async/await` I/O için zorunlu.
- EF: parametrik sorgular (SQLi yok). Migration adları açıklayıcı: `dotnet ef migrations add <Ad>`.
- Repository/service katmanı: controller'lar ince; iş mantığı `Services/` altında.
- Güvenlik varsayılan: `[ValidateAntiForgeryToken]`, rate limit (iletişim formu), honeypot, güvenli upload (tip/boyut), secure headers, HTTPS redirect.
- DSGVO: cookie consent (varsayılan reddet), açık form onayı, veri minimizasyonu.
- Erişilebilirlik: WCAG AA, `aria-*`, odak halkaları, `prefers-reduced-motion`.
- Performans: lazy-load, responsive `srcset`/WebP, minimum JS, CLS≈0. Lighthouse ≥ 90 hedefi.

## 6. Proje Komutları

```bash
dotnet build                              # derle
dotnet run --project src/Baudorf.Web      # çalıştır (https://localhost:xxxx)
dotnet ef migrations add <Ad> --project src/Baudorf.Web
dotnet ef database update --project src/Baudorf.Web
```
Connection string → `appsettings.Development.json` `ConnectionStrings:DefaultConnection`
(secrets/production'da env var). **Connection string'i repoya commit'leme.**

## 7. Çalışma Sırası (roadmap)

İskelet → ana sayfa → Immobilien liste/detay → iletişim → admin Immobilien CRUD,
sonra: üyelik/off-market gating → Leads admin → Blog → Team → Tippgeber/Karriere →
Ayarlar → hukuki sayfalar → i18n → SEO/Schema.org → deploy. Her büyük adımda kullanıcıya özet ver.

## 8. YAPMA

- WordPress tasarımını kopyalama; AI taslağını birebir tekrarlama — **aş**.
- Off-market veriyi açıkta gösterme (gating zorunlu).
- Animasyonu abartma (sessiz lüks).
- Lorem-ipsum/Unsplash placeholder bırakma — gerçek seed + admin'den medya.
- DSGVO/Impressum/Datenschutz'u atlama (Almanya'da yasal zorunluluk).
- Connection string / SMTP / secret'ları commit'leme.

## 9. Git

Remote: https://github.com/ibrahimgundogdu/baudorf.git
Commit/push yalnızca kullanıcı isteyince. Commit mesajı sonu:
`Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`
