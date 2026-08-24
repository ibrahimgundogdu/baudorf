using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Baudorf.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Datenbank (SQL Server, Code-First) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- Identity (ApplicationUser + Rollen) ---
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // TODO: prod'da e-posta doğrulama açılacak
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;

        // Brute-Force-Schutz: Konto nach 5 Fehlversuchen 15 Min. sperren.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<GermanIdentityErrorDescriber>()
    .AddClaimsPrincipalFactory<AdditionalUserClaimsPrincipalFactory>()
    .AddDefaultUI();

// Login-Protokoll: bei jeder Anmeldung einen LoginEvent speichern.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    // Sitzung 14 Tage gültig, gleitend verlängert — kein Rauswurf nach kurzer Inaktivität.
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.Events.OnSignedIn = async ctx =>
    {
        var sp = ctx.HttpContext.RequestServices;
        var dbx = sp.GetRequiredService<ApplicationDbContext>();
        var userId = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        dbx.LoginEvents.Add(new LoginEvent
        {
            UserId = userId,
            Email = ctx.Principal?.Identity?.Name,
            IpAdresse = ctx.HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = ctx.HttpContext.Request.Headers.UserAgent.ToString()
        });
        await dbx.SaveChangesAsync();
    };
});

// SecurityStamp seltener prüfen (Standard 30 Min.) — verhindert schnelle Abmeldungen,
// wenn sich am Konto etwas ändert. 12 Stunden ist für ein kleines Admin-Team ausreichend.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromHours(12));

// --- Site-/SEO-Konfiguration ---
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.SectionName));

// --- Turnstile (CAPTCHA, optional) ---
builder.Services.Configure<TurnstileOptions>(builder.Configuration.GetSection(TurnstileOptions.SectionName));
builder.Services.AddHttpClient<ITurnstileVerifier, TurnstileVerifier>();

// --- Anwendungsdienste ---
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<ISiteSettings, SiteSettingsService>();
builder.Services.AddScoped<IMediaLibrary, MediaLibrary>();

// SEO-Weiterleitungen (alte, indexierte URLs → neue Adressen) + 404-Protokoll.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IRedirectService, RedirectService>();
builder.Services.AddSingleton<ILookupService, LookupService>();

// E-Mail: echter SMTP-Versand, sobald "Email:Host" konfiguriert ist (appsettings.Production.json /
// Umgebungsvariablen). Ohne Host → Log-Only (kein realer Versand). Steuert auch die 2FA-Methode:
// bei konfiguriertem SMTP läuft die Zwei-Faktor-Anmeldung über einen per E-Mail zugestellten Code.
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
var emailConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["Email:Host"]);
if (emailConfigured)
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
else
    builder.Services.AddScoped<IEmailService, LoggingEmailService>();

// --- Autorisierung ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminArea", p => p.RequireRole(Roles.Admin, Roles.Redakteur));
});

// --- Rate Limiting (Kontaktformular gegen Spam) ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("kontakt", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));

    // Login: Brute-Force zusätzlich pro IP drosseln.
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));
});

// Mehrsprachigkeit (Admin-Panel DE/EN): deutsche Texte sind die Schlüssel, nur die
// englische Übersetzung wird in Resources/SharedResource.en.resx gepflegt.
// Kein ResourcesPath: die .resx liegt neben der Klasse SharedResource (gleicher Namespace),
// daher heißt die Ressource "Baudorf.Web.SharedResource(.en)" — genau das sucht der Localizer.
builder.Services.AddLocalization();

// Non-nullable Reference-Types (z. B. "string Slug") NICHT automatisch als Pflichtfeld
// behandeln — sonst erzeugt jedes solche Formularfeld ein client-seitiges "required",
// obwohl der Wert serverseitig erzeugt wird (Slugs). Pflichtfelder nutzen explizit [Required].
builder.Services.AddControllersWithViews(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddViewLocalization();
builder.Services.AddRazorPages();

// Unterstützte Kulturen: Standard Deutsch, optional Englisch (per Cookie umschaltbar).
var supportedCultures = new[] { "de", "en" };
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("de")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    // Nur Cookie berücksichtigen — die Browsersprache soll die Seite NICHT automatisch umstellen
    // (öffentliche Site bleibt Deutsch; Umschalten erfolgt bewusst im Admin).
    options.RequestCultureProviders =
    [
        new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider()
    ];
});

// Antiforgery-Token auch per Header (für den JS-Consent-POST).
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

// Data-Protection-Schlüssel dauerhaft ablegen — sonst werden Auth-Cookies bei jedem
// App-Neustart/AppPool-Recycle ungültig (Nutzer wird ausgeloggt). Standardmäßig AUSSERHALB
// des Site-Ordners (Windows: C:\ProgramData\Baudorf\keys), damit ein Deploy die Schlüssel
// nicht überschreibt. Per Konfiguration überschreibbar: "DataProtection:KeysPath".
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(keysPath))
{
    keysPath = OperatingSystem.IsWindows()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Baudorf", "keys")
        : Path.Combine(builder.Environment.ContentRootPath, "keys");
}
try { Directory.CreateDirectory(keysPath); }
catch { keysPath = Path.Combine(builder.Environment.ContentRootPath, "keys"); Directory.CreateDirectory(keysPath); }

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("Baudorf");

var app = builder.Build();

// Hinter dem Nginx-Reverse-Proxy (Linux/Hetzner) das Original-Schema (https) und die
// echte Client-IP übernehmen — sonst würde UseHttpsRedirection() in eine Schleife laufen
// und Rate-Limit/Logs zeigten nur die Proxy-IP. Auf IIS/Windows unschädlich.
var forwardedOptions = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Zur Laufzeit hochgeladene Dateien (wwwroot/uploads) von der Festplatte ausliefern.
// MapStaticAssets() bedient nur die zur Build-Zeit bekannten Assets — Uploads sind
// dort nicht enthalten und würden sonst 404 liefern.
app.UseStaticFiles();

// Sprache (DE/EN) aus dem Cookie anwenden (Admin-Umschalter).
app.UseRequestLocalization();

// Eigene Fehlerseiten (404 etc.) + Protokollierung kaputter URLs. NACH StaticFiles, damit
// fehlende Assets nicht neu ausgeführt werden; das Ziel /Fehler/{code} rendert die Markenseite.
app.UseStatusCodePagesWithReExecute("/Fehler/{0}");

// Alte, in Google indexierte URLs per 301 auf die neuen Adressen umleiten (vor dem Routing).
app.UseMiddleware<Baudorf.Web.Services.SeoRedirectMiddleware>();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Hinweis: Die frühere "Authenticator-Pflicht" (Weiterleitung zur TOTP-Einrichtung) wurde
// entfernt. Die Zwei-Faktor-Anmeldung läuft jetzt — sofern SMTP konfiguriert ist — direkt beim
// Login über einen per E-Mail zugestellten Code (siehe LoginWithEmailCode). Ohne SMTP-Konfiguration
// gilt reine Passwort-Anmeldung; der DbSeeder gleicht das 2FA-Flag der Konten entsprechend ab.

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// --- Migration + Seed ---
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
