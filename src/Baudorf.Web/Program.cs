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

// --- Site-/SEO-Konfiguration ---
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.SectionName));

// --- Turnstile (CAPTCHA, optional) ---
builder.Services.Configure<TurnstileOptions>(builder.Configuration.GetSection(TurnstileOptions.SectionName));
builder.Services.AddHttpClient<ITurnstileVerifier, TurnstileVerifier>();

// --- Anwendungsdienste ---
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IEmailService, LoggingEmailService>();
builder.Services.AddScoped<ISiteSettings, SiteSettingsService>();
builder.Services.AddScoped<IMediaLibrary, MediaLibrary>();

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

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Antiforgery-Token auch per Header (für den JS-Consent-POST).
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

// Data-Protection-Schlüssel dauerhaft ablegen — sonst werden Auth-Cookies bei
// jedem App-Neustart ungültig (Nutzer wird ausgeloggt). Ordner "keys" ist gitignored.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("Baudorf");

var app = builder.Build();

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

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// 2FA-Pflicht für Staff: angemeldete Admins/Redakteure ohne aktivierte
// Zwei-Faktor-Authentifizierung werden zur Einrichtung geleitet. Nur echte
// Seitenaufrufe (GET/HTML), Manage-/Logout-Pfade bleiben frei (kein Loop).
// In Development deaktiviert: lokale Maschinen haben oft eine falsche Uhr/Zeitzone,
// wodurch TOTP-Codes nie passen. In Produktion (korrekte Zeit) voll aktiv.
if (!app.Environment.IsDevelopment())
{
app.Use(async (context, next) =>
{
    var user = context.User;
    if (user.Identity?.IsAuthenticated == true &&
        (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Redakteur)) &&
        HttpMethods.IsGet(context.Request.Method) &&
        context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var frei = path.StartsWith("/Identity/Account/Manage", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Identity/Account/Logout", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Identity/Account/LoginWith2fa", StringComparison.OrdinalIgnoreCase);
        if (!frei)
        {
            var userMgr = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var appUser = await userMgr.GetUserAsync(user);
            if (appUser is not null && !await userMgr.GetTwoFactorEnabledAsync(appUser))
            {
                context.Response.Redirect("/Identity/Account/Manage/EnableAuthenticator");
                return;
            }
        }
    }
    await next();
});
}

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
