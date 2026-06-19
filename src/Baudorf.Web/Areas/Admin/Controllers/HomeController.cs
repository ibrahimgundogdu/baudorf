using Baudorf.Web.Data;
using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = new DashboardViewModel
        {
            VeroeffentlichteObjekte = await db.Properties.CountAsync(p => p.IstVeroeffentlicht),
            ObjekteGesamt = await db.Properties.CountAsync(),
            NeueLeads = await db.Leads.CountAsync(l => l.Status == LeadStatus.Neu),
            LeadsGesamt = await db.Leads.CountAsync(),
            Benutzer = await db.Users.CountAsync(),
            WartendeFreigaben = await db.Users.CountAsync(u => !u.IstFreigegeben),
            OffeneBewerbungen = await db.CareerApplications.CountAsync(c => c.Status == LeadStatus.Neu)
                                + await db.TippgeberApplications.CountAsync(t => t.Status == LeadStatus.Neu),
            LetzteLeads = await db.Leads.AsNoTracking().OrderByDescending(l => l.CreatedAt).Take(6).ToListAsync(),
            LetzteLogins = await db.LoginEvents.AsNoTracking().OrderByDescending(l => l.CreatedAt).Take(6).ToListAsync(),
            LetzteAufrufe = await db.PropertyViews.AsNoTracking()
                .Include(v => v.Property).Include(v => v.User)
                .OrderByDescending(v => v.CreatedAt).Take(6).ToListAsync(),
            WhatsAppKlicks = await db.WhatsAppClicks.CountAsync()
        };
        return View(vm);
    }
}

public class DashboardViewModel
{
    public int VeroeffentlichteObjekte { get; set; }
    public int ObjekteGesamt { get; set; }
    public int NeueLeads { get; set; }
    public int LeadsGesamt { get; set; }
    public int Benutzer { get; set; }
    public int WartendeFreigaben { get; set; }
    public int OffeneBewerbungen { get; set; }
    public int WhatsAppKlicks { get; set; }
    public List<Lead> LetzteLeads { get; set; } = [];
    public List<LoginEvent> LetzteLogins { get; set; } = [];
    public List<PropertyView> LetzteAufrufe { get; set; } = [];
}
