using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class UsersController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userMgr) : Controller
{
    public async Task<IActionResult> Index()
    {
        var users = await db.Users.AsNoTracking().OrderByDescending(u => u.CreatedAt).ToListAsync();
        var vms = new List<UserRowViewModel>();
        foreach (var u in users)
        {
            vms.Add(new UserRowViewModel
            {
                User = u,
                Rollen = await userMgr.GetRolesAsync(u),
                AnzahlAufrufe = await db.PropertyViews.CountAsync(v => v.UserId == u.Id)
            });
        }
        return View(vms);
    }

    public async Task<IActionResult> Details(string id)
    {
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();

        var vm = new UserDetailViewModel
        {
            User = u,
            Rollen = await userMgr.GetRolesAsync(u),
            Logins = await db.LoginEvents.AsNoTracking()
                .Where(l => l.UserId == id).OrderByDescending(l => l.CreatedAt).Take(50).ToListAsync(),
            Aufrufe = await db.PropertyViews.AsNoTracking()
                .Where(v => v.UserId == id).Include(v => v.Property)
                .OrderByDescending(v => v.CreatedAt).Take(50).ToListAsync(),
            AlleRollen = Roles.All
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetFreigabe(string id, bool freigeben)
    {
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();
        u.IstFreigegeben = freigeben;
        u.FreigegebenAm = freigeben ? DateTime.UtcNow : null;
        if (freigeben && !await userMgr.IsInRoleAsync(u, Roles.Investor))
            await userMgr.AddToRoleAsync(u, Roles.Investor);
        await db.SaveChangesAsync();
        TempData["Success"] = freigeben ? "Benutzer freigegeben." : "Freigabe entzogen.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string rolle, bool zuweisen)
    {
        var u = await userMgr.FindByIdAsync(id);
        if (u is null) return NotFound();
        if (!Roles.All.Contains(rolle)) return BadRequest();

        if (zuweisen && !await userMgr.IsInRoleAsync(u, rolle))
            await userMgr.AddToRoleAsync(u, rolle);
        else if (!zuweisen && await userMgr.IsInRoleAsync(u, rolle))
            await userMgr.RemoveFromRoleAsync(u, rolle);

        TempData["Success"] = "Rollen aktualisiert.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

public class UserRowViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public IList<string> Rollen { get; set; } = [];
    public int AnzahlAufrufe { get; set; }
}

public class UserDetailViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public IList<string> Rollen { get; set; } = [];
    public List<LoginEvent> Logins { get; set; } = [];
    public List<PropertyView> Aufrufe { get; set; } = [];
    public string[] AlleRollen { get; set; } = [];
}
