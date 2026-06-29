using System.Security.Claims;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Baudorf.Web.Services;

/// <summary>
/// Ergänzt das Login-Cookie um den Anzeigenamen (und Freigabe-Status), damit Views diese
/// ohne zusätzliche DB-Abfrage anzeigen können. Erbt von der rollenfähigen Factory, damit
/// die Rollen-Claims (Admin/Redakteur/Investor) erhalten bleiben.
/// </summary>
public class AdditionalUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        if (principal.Identity is ClaimsIdentity identity)
        {
            if (!string.IsNullOrWhiteSpace(user.AnzeigeName))
                identity.AddClaim(new Claim("AnzeigeName", user.AnzeigeName));
            if (user.IstFreigegeben)
                identity.AddClaim(new Claim("IstFreigegeben", "true"));
        }
        return principal;
    }
}
