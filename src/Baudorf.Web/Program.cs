using System.Threading.RateLimiting;
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
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<GermanIdentityErrorDescriber>()
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
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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
app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

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
