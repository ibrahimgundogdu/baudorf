using Baudorf.Web.Data;
using Baudorf.Web.Models;
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
            OffeneBewerbungen = await db.CareerApplications.CountAsync(c => c.Status == LeadStatus.Neu)
                                + await db.TippgeberApplications.CountAsync(t => t.Status == LeadStatus.Neu)
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
    public int OffeneBewerbungen { get; set; }
}
