using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class ActivityController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = new ActivityViewModel
        {
            Logins = await db.LoginEvents.AsNoTracking()
                .OrderByDescending(l => l.CreatedAt).Take(100).ToListAsync(),
            Aufrufe = await db.PropertyViews.AsNoTracking()
                .Include(v => v.Property).Include(v => v.User)
                .OrderByDescending(v => v.CreatedAt).Take(100).ToListAsync(),
            WhatsApp = await db.WhatsAppClicks.AsNoTracking()
                .Include(w => w.Property).Include(w => w.User)
                .OrderByDescending(w => w.CreatedAt).Take(100).ToListAsync()
        };
        return View(vm);
    }
}

public class ActivityViewModel
{
    public List<LoginEvent> Logins { get; set; } = [];
    public List<PropertyView> Aufrufe { get; set; } = [];
    public List<WhatsAppClick> WhatsApp { get; set; } = [];
}
