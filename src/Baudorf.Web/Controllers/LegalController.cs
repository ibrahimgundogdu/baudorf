using Microsoft.AspNetCore.Mvc;

namespace Baudorf.Web.Controllers;

public class LegalController : Controller
{
    public IActionResult Impressum() => View();
    public IActionResult Datenschutz() => View();
    public IActionResult Agb() => View();
}
