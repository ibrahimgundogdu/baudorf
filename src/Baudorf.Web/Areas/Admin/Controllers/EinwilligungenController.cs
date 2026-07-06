using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

/// <summary>Cookie-Einwilligungen (DSGVO-Nachweis) — nur lesend.</summary>
[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class EinwilligungenController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var eintraege = await db.ConsentLogs.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Take(300)
            .ToListAsync();

        var vm = new EinwilligungenViewModel
        {
            Eintraege = eintraege,
            Gesamt = await db.ConsentLogs.CountAsync(),
            Akzeptiert = await db.ConsentLogs.CountAsync(c => c.Aktion == "accept"),
            Abgelehnt = await db.ConsentLogs.CountAsync(c => c.Aktion == "reject"),
            Angepasst = await db.ConsentLogs.CountAsync(c => c.Aktion == "custom")
        };
        return View(vm);
    }
}

public class EinwilligungenViewModel
{
    public List<ConsentLog> Eintraege { get; set; } = [];
    public int Gesamt { get; set; }
    public int Akzeptiert { get; set; }
    public int Abgelehnt { get; set; }
    public int Angepasst { get; set; }
}
