# Claude Code Prompt — Baudorf Immobilien GmbH Web Sitesi & Admin Paneli

> Bu dosyayı olduğu gibi Claude Code'a verebilirsin. Üstteki "Teknoloji Yığını" bölümünü
> beğenmezsen sadece o bölümü değiştirmen yeterli; geri kalan brief stack'ten bağımsızdır.

---

## 0. Rolün ve Görevin

Sen kıdemli bir full-stack mimar + UI/UX tasarımcısın. Almanya'da Velbert (NRW) merkezli,
**off-market lüks gayrimenkul** uzmanı **Baudorf Immobilien GmbH** için sıfırdan modern,
prodüksiyona hazır bir web sitesi ve yönetim paneli kuracaksın.

İki referansın var:

1. **Mevcut WordPress sitesi:** https://baudorf.de/ — içerik, menü yapısı ve hukuki
   metinler için kaynak. Tasarımı eski; bunu **taklit etme**, sadece bilgi/yapı için kullan.
2. **Sahibin yapay zekayla yaptığı tek-sayfa HTML taslağı** (`baudorf-FINAL__9_.html`).
   Tasarım dili gerçekten iyi (altın + koyu mürekkep + krem, Inter font, scroll-reveal).
   **Bunu temel al, ama AŞ.** Daha modern, daha kullanışlı, gerçek bir CMS + üyelik +
   video ağırlıklı portfolio + güçlü iletişim ile bir üst seviyeye taşı.

Hedef: Taslaktan **belirgin biçimde daha iyi** bir ürün. Tek sayfalık statik mockup değil;
admin panelinden yönetilen, canlı, ölçeklenebilir bir platform.

---

## 1. Teknoloji Yığını  *(değiştirmek istersen sadece bu bölümü değiştir)*

- **Backend:** .NET 10, ASP.NET Core MVC (Razor) — temiz katmanlı mimari
  (`Baudorf.Core`, `Baudorf.Infrastructure`, `Baudorf.Web`, `Baudorf.Admin` veya tek Web içinde Area).
- **DB:** SQL Server + Entity Framework Core (Code-First, migration'lar dahil).
- **Üyelik/Auth:** ASP.NET Core Identity (off-market erişim kapısı + admin rolleri).
- **Frontend:** Razor + **Tailwind CSS** + hafif **Alpine.js** (etkileşim/slider için);
  ağır JS framework kullanma. Animasyonlar için CSS + IntersectionObserver.
- **Görsel/video depolama:** Cloudflare R2 veya wwwroot/uploads (soyutla, kolay değişsin).
- **Mail:** SMTP/Resend soyutlaması (iletişim formu + lead bildirimleri).
- **Deploy hedefi:** Ubuntu 24.04 + Nginx + systemd (Hetzner VPS), TLS Let's Encrypt.
  README'ye deploy adımlarını yaz.

> Alternatif istenirse: Next.js 14 (App Router) + Tailwind + Prisma + Postgres, admin için
> aynı şema. Bu durumda tüm brief geçerli kalır, sadece teknoloji değişir.

---

## 2. Marka & Tasarım Dili

**Karakter:** Diskret lüks. Sessiz güç. "Still, wirkungsvoll, mit Stil." (Sessiz, etkili,
stille.) Asla cıvık/agresif değil — zarif, az ama öz, bol beyaz alan, premium his.

**Renk paleti (taslaktan, koru):**
```
--gold:    #C4A46B   (vurgu / aksiyon)
--gold-lt: #D9BF8E
--gold-dk: #96773A
--black:   #0C0C0C
--ink:     #181614   (ana metin/koyu zemin)
--graph:   #232120
--stone:   #5C5550   (ikincil metin)
--parch:   #F5F2EC   (krem zemin)
--linen:   #EDE8DF
--white:   #FDFCFA
```
Bölümler arası ritim: krem ↔ beyaz ↔ koyu mürekkep zeminler dönüşümlü; altın yalnızca
vurgu olarak (asla baskın değil).

**Tipografi:** Inter (300/400/500/600 + italic). Başlıklar büyük, `letter-spacing` negatif,
italic altın vurgular (örn. *Exklusiv*). Overline'lar: küçük, harf aralığı geniş, uppercase,
önünde kısa altın çizgi.

**Logo & marka varlıkları** (kullanıcı sağlayacak — `/uploads` klasörüne koyduracak):
`logo_Facebook.png` (yuvarlak logo), `Baudorf_Facebook_Header.png`, daire+bina amblemi
(`Streifen_Baudorf2_Kopie.jpg` desen olarak filigran), `Singnatur_baudorf_2018.jpg` (imza).
Daire+bina amblemini bölüm arka planlarında çok düşük opaklıkta filigran/parallax motif yap.

**Dil:** Site **Almanca (de)** birincil. Uluslararası objeler için **EN** opsiyonel ikinci dil
altyapısı kur (resource/i18n hazır olsun, ama içerik DE öncelikli).

---

## 3. Bilgi Mimarisi (Sitemap)

Gerçek menüye sadık kal:

- **Startseite** (ana sayfa)
- **Immobilien**
  - Off-Market-Immobilien
  - Kapitalanlagen
  - Investments
  - Gewerbeimmobilien
  - Wohnimmobilien
  - Grundstücke / Projektentwicklungen
  - Auslandsimmobilien (International)
  - *(Liste + filtre + detay sayfaları)*
- **Über uns**
  - Karriere
  - Tippgeber (tavsiye/komisyon programı)
- **Leistungen**
  - Stille Vermarktung
  - Klassische Vermarktung
  - Immobilienbewertung
  - Kaufbegleitung
  - Dienstleisternetzwerk
- **Kontakt**
- **Mein Konto** → Registrieren / Anmelden (off-market erişim kapısı)
- **Hukuki:** Impressum, AGB, Datenschutzerklärung *(içerikleri baudorf.de'den taşı)*

---

## 4. Ana Sayfa — Scroll Deneyimi (sırayla)

Her bölüm scroll-reveal ile yumuşak girer (IntersectionObserver, kademeli gecikme).
Bölümler arası geçişler akıcı; ama lüks markaya yakışır biçimde **ölçülü** animasyon — abartma.

1. **Hero (tam ekran, VİDEO):** Arka planda sessiz, otomatik döngü, kısık bir gayrimenkul/şehir
   videosu (drone/NRW skyline) + koyu gradient overlay. Üstte:
   overline "Off-Market-Immobilien seit 1994", dev başlık **"Diskret. *Exklusiv.* Direkt."**,
   altında kısa alt metin + iki CTA (altın "Objekte anfragen" + outline "Über uns"). Sağda çok
   düşük opaklıkta daire+bina amblemi parallax filigran. Aşağı scroll ipucu.
2. **Philosophie:** "Wir vermitteln nicht. *Wir verbinden.*" + diskresyon/güven anlatısı.
3. **Leistungen:** Kart/grid — Stille Vermarktung, Klassische Vermarktung, Immobilienbewertung,
   Kaufbegleitung, Dienstleisternetzwerk. Her kart hover'da zarif altın detay.
4. **Aktuelle Objekte (ÖNE ÇIKAN — slider/karusel):** Yatay kaydırmalı premium property
   karusel. Her kart: kapak görseli, badge (Off-Market / Verfügbar / Neu), konum, ad,
   specs (Wfl. / GF / BJ), fiyat veya "auf Anfrage", "Faktor"/getiri. Off-market objelerde
   detaylar bulanık/kilitli + "Für vorgemerkte Investoren — jetzt registrieren". CTA: tüm
   objelere git.
5. **Unser Vorgehen (Süreç):** Adımlı timeline — ilk görüşmeden notere kadar (gezilince çizgi
   dolar/sayılar count-up). Adımlar: Immobiliensuche → Objektauswahl → diskretes Vertrautmachen
   → Transaktionsprozess.
6. **Unsere Klientel:** Family Offices, institutionelle Investoren, Wohnungsgesellschaften,
   Bauträger, Projektentwickler — koyu mürekkep zeminde zarif liste/ikonlar.
7. **Über uns / Team:** Andrea Krüger (Geschäftsführerin · Immobilienmaklerin · Dipl.-Ing.
   Architektin) portresi + 1994'ten bugüne hikâye. Ofis köpeği **Ayla** ("Immobilien-Spürnase")
   için sıcak, samimi mini kart (markaya kişilik katıyor — koru).
8. **Trust / Zahlen + Bewertungen:** count-up istatistikler (1994 Gründungsjahr, "Still" Methode,
   "NRW" Fokus, "Off-Market" Kernsegment) + müşteri yorumları karuseli.
9. **Tippgeber:** "Empfehlen Sie uns — und *profitieren Sie*." Tavsiye/komisyon CTA + başvuru formu.
10. **Karriere:** Açık pozisyonlar / genel başvuru CTA.
11. **Markt & Insights (Blog):** "Expertenwissen für *kluge Investoren*." Son 3 makale kartı,
    tümünü gör linki.
12. **Kontakt:** Güçlü iletişim bloğu (bkz. §7).
13. **Footer:** çok sütunlu — iletişim künyesi, Immobilien linkleri, Leistungen, Unternehmen,
    hukuki linkler, telif. Adres: Auf der Egge 68 · 42555 Velbert.

---

## 5. Görsel & Video Ağırlıklı Yaklaşım

Bu sitenin kalbi görseller ve videolardır. Tasarımı buna göre kur:

- **Hero video** + bölüm içi kısa loop'lar (poster fallback, `prefers-reduced-motion` saygısı).
- **Property galerileri:** her obje için lightbox galeri, full-screen mod, klavye/swipe nav.
- **Video tur / drone / sanal tur:** her objede opsiyonel video embed + 360°/sanal tur iframe alanı.
- **Before/After kaydırıcı** (projektentwicklung/renovasyon objeleri için opsiyonel).
- **Lazy-load + responsive `srcset` + WebP/AVIF**; LQIP/blur-up placeholder.
- Tüm görsellere anlamlı `alt`; CLS sıfıra yakın (boyut rezervasyonu).
- Yer tutucu olarak Unsplash kullanma; admin'den gerçek medya yüklenecek şekilde kur.

---

## 6. Immobilien (Portfolio) — Liste, Filtre & Detay

**Liste sayfası:** Grid + sol/üst filtre — Objektart, Standort (NRW/uluslararası), fiyat
aralığı, Wohnfläche, getiri/Faktor, durum (Off-Market / Verfügbar / Reserviert / Verkauft).
Sıralama. Sayfalama veya zarif sonsuz scroll. Off-market objeler listede görünür ama kilitli.

**Detay sayfası (doyurucu & bilgilendirici):**
- Büyük görsel galeri + video + sanal tur sekmeleri.
- Künye tablosu: tip, konum, Wohnfläche, Grundstücksfläche, Baujahr, Zustand, Energieklasse,
  Einheiten, Faktor/Rendite, Kaufpreis veya "auf Anfrage".
- Açıklama (zengin metin), Lage/konum + **harita** (NRW odaklı; tam adres off-market'te gizli,
  yaklaşık bölge gösterilir).
- Yatırım göstergeleri (Faktor, brüt getiri) — yatırımcı odaklı sunum.
- **"Vertraulich anfragen"** CTA → objeye bağlı lead formu.
- Benzer objeler önerisi.
- Off-market objede içerik kayıtlı/onaylı kullanıcıya açılır (gating).

---

## 7. İletişim — Güçlü ve Çok Kanallı

- **Form:** Vorname*, Nachname*, E-Mail*, Telefon, "Ich interessiere mich als" (select:
  Käufer–Privatinvestor / Käufer–Family Office / Käufer–Institutioneller Investor /
  Verkäufer–Bestandshalter / Verkäufer–Projektentwickler / Immobilienbewertung /
  Kaufbegleitung / Tippgeber / Karriere / Sonstiges), Anliegen (textarea),
  DSGVO onay kutusu. Başarı durumunda zarif teşekkür ekranı ("Wir melden uns persönlich
  und diskret — i.d.R. innerhalb von 24 Stunden.").
- **Doğrulama** (client + server), **anti-spam** (honeypot + rate limit), CSRF koruması.
- Gönderimde **admin'e mail + lead kaydı DB'ye**.
- Yan panel künye: Baudorf Immobilien GmbH · Auf der Egge 68 · 42555 Velbert ·
  Mobil **0177 – 838 78 98** · **andrea.krueger@baudorf.de** · Mo–Fr 09:00–18:00 ·
  Diskretion notu.
- **Tıkla-ara**, **WhatsApp** linki, **mailto**, harita.
- Opsiyonel: **randevu talebi** (tarih/saat tercihi) lead'e bağlı.

---

## 8. Admin Paneli (Yönetim)

Korumalı (`/admin`, sadece Admin/Redakteur rolleri). Aynı marka dili, sade ve hızlı.

- **Dashboard:** son lead'ler, yayınlı obje sayıları, son başvurular, basit istatistik kartları.
- **Immobilien CRUD:** ekle/düzenle/sil, çoklu görsel + video yükleme (sürükle-bırak, sıralama,
  kapak seçimi), durum (Off-Market/Verfügbar/Reserviert/Verkauft), öne çıkar, SEO alanları,
  taslak/yayınla, slug, künye alanları, harita konumu.
- **Leads / Anfragen:** gelen kutusu, durum (Neu/In Bearbeitung/Erledigt), not, atama,
  CSV export, mail ile yanıt.
- **Blog / Insights:** makale CRUD, kapak görseli, kategori/etiket, yayın tarihi.
- **Team / Über uns:** üye CRUD (Andrea + Ayla dahil).
- **Tippgeber & Karriere:** başvuru kayıtları yönetimi.
- **Kullanıcılar:** kayıtlı yatırımcıları görüntüle, off-market erişimini onayla/reddet, roller.
- **Ayarlar:** iletişim künyesi, sosyal linkler, SMTP, hero medya, hukuki metinler.

---

## 9. Mein Konto / Üyelik (Off-Market Kapısı)

- **Registrieren / Anmelden** (Identity), e-posta doğrulama, parola sıfırlama.
- Kayıt sonrası **admin onayı** ile off-market objelere tam erişim (yatırımcı doğrulama mantığı).
- Üye paneli: **Merkliste** (kaydedilen objeler), **kendi anfrage geçmişi**, profil.
- Giriş yapmamış kullanıcıya off-market detayları kilitli + kayıt çağrısı.

---

## 10. Teknik Gereksinimler

- **Tam responsive** (mobil-öncelikli); mobilde tam ekran hamburger menü (taslaktaki gibi),
  dokunmatik slider/swipe.
- **Performans:** Lighthouse ≥ 90 (Performance/SEO/Best Practices/Accessibility); lazy-load,
  görsel optimizasyonu, kritik CSS, minimum JS.
- **SEO:** semantik HTML, başlık hiyerarşisi, meta + Open Graph, `sitemap.xml`, `robots.txt`,
  **Schema.org** (RealEstateAgent + Offer/Residence), Almanca anahtar kelimeler
  (Off-Market Immobilien NRW, stille Vermarktung, Kapitalanlage, Family Office Immobilien).
- **Erişilebilirlik:** WCAG AA, klavye navigasyonu, odak halkaları, `aria-*`, kontrast,
  `prefers-reduced-motion`.
- **DSGVO/Hukuk (Almanya — önemli):** cookie consent banner (esas/opsiyonel ayrımı, varsayılan
  reddet), Datenschutzerklärung/Impressum/AGB sayfaları, formlarda açık onay, veri minimizasyonu.
- **Güvenlik:** CSRF, XSS/SQLi koruması (EF parametrik), rate limiting, güvenli upload (tip/boyut
  doğrulama), HTTPS yönlendirme, secure headers.
- **i18n** altyapısı (DE birincil, EN opsiyonel).

---

## 11. Etkileşim & Animasyon Detayları

Tümü **performans dostu** ve **ölçülü** (lüks = sükûnet):

- Scroll-reveal (kademeli fade/translate, IntersectionObserver).
- Sticky, scroll'da incelen nav (taslaktaki `shrink` davranışı).
- Yatay kaydırmalı obje karuseli + yorum karuseli (Alpine.js).
- Hafif parallax (hero amblemi, bölüm filigranları).
- Count-up istatistikler (görünür olunca).
- Hover mikro-etkileşimleri (altın alt çizgi, kart yükselmesi, görsel zoom).
- Smooth-scroll iç bağlantılar, yumuşak `cubic-bezier(0.22,1,0.36,1)` geçişler.
- Görsel galeride lightbox + before/after slider.
- `prefers-reduced-motion: reduce` → tüm hareketleri kapat.

---

## 12. Gerçek İçerik / Seed Verisi

Aşağıdakileri seed data olarak kullan (DB'ye doldur, demo görünsün):

- **Firma:** Baudorf Immobilien GmbH · Velbert (NRW) · 1994 mimarlık bürosu olarak başladı,
  2018'de GmbH kuruldu · uzmanlık: stille/Off-Market Vermarktung.
- **Sahibi:** Andrea Krüger — Geschäftsführerin, Immobilienmaklerin, Dipl.-Ing. Architektin.
- **Ofis köpeği:** Ayla — "Immobilien-Spürnase" (sıcak mini bölüm).
- **İletişim:** Auf der Egge 68, 42555 Velbert · 0177 – 838 78 98 ·
  andrea.krueger@baudorf.de · Mo–Fr 09:00–18:00.
- **Örnek objeler (taslaktan):**
  - *Faktor 20,7 — 40 Wohneinheiten · KfW 40 / QNG*, NRW, Wfl. ~2.809 m², GF ~2.940 m²,
    BJ 2027, 9.750.000 €, Off-Market · Neu.
  - *Neubau-Senioreneinrichtung — 80 stationäre Plätze*, Herne/NRW, GF ~7.565 m², BJ 2023,
    Verfügbar.
  - (Birkaç obje daha üret: Gewerbe, Wohnanlage, Grundstück, Auslandsimmobilie.)
- **Slogan:** "Still, wirkungsvoll, mit Stil." / "Diskret. Exklusiv. Direkt."
- **Hukuki metinler:** Impressum/AGB/Datenschutz içeriklerini baudorf.de'den taşı (placeholder
  bırakma; gerçek metni kullanıcıdan iste veya mevcut siteden al).

---

## 13. Teslimat & Proje Yapısı

- Çalışır .NET 10 çözümü, katmanlı yapı, EF Core migration'lar + seed.
- Tailwind config + tasarım token'ları (renkler/tipografi yukarıdaki paletle).
- `wwwroot/uploads` veya R2 soyutlaması.
- **README.md:** local çalıştırma, migration, seed, ortam değişkenleri (DB, SMTP, R2),
  Hetzner + Nginx + systemd + TLS deploy adımları.
- Temiz, yorumlu, sürdürülebilir kod; admin ve public ayrık ama tutarlı.
- İşe **proje iskeleti + ana sayfa + Immobilien liste/detay + iletişim + admin Immobilien CRUD**
  ile başla; sonra kalan bölümleri ekle. Her büyük adımda bana özet ver.

---

## 14. YAPMA / Dikkat

- Mevcut WordPress tasarımını **kopyalama**; yalnızca içerik/yapı kaynağı.
- AI taslağını birebir tekrarlama — **geçmeyi** hedefle (gerçek CMS, üyelik, video, daha iyi UX).
- Off-market obje verisini herkese **açıkta gösterme** (gating zorunlu).
- Animasyonu abartma; marka **sessiz lüks** — sade, hızlı, zarif kalsın.
- Placeholder lorem-ipsum/Unsplash ile bırakma; gerçek seed + admin'den medya akışı kur.
- DSGVO/Impressum/Datenschutz'u atlama (Almanya'da yasal zorunluluk).

---

**Başla:** Önce kısa bir mimari + sayfa planı + veri modeli taslağı çıkar, sonra iskeleti kur ve
ana sayfayı hayata geçir. Belirsizlik olursa makul varsayım yap, varsayımı belirt, devam et.