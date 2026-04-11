using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schichtplaner.Data;
using Schichtplaner.Models.ViewModels;

namespace Schichtplaner.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Today;
        var monthStart = new DateTime(now.Year, now.Month, 1).Date;
        var monthEnd = monthStart.AddMonths(1);

        var model = new DashboardViewModel
        {
            AnzahlStandorte = await _db.Standorte.CountAsync(),
            AnzahlMitarbeiter = await _db.Mitarbeiter.CountAsync(m => m.Aktiv),
            AnzahlSchichtenDiesenMonat = await _db.Schichten.CountAsync(s => s.Datum >= monthStart && s.Datum < monthEnd)
        };

        var mitarbeiter = await _db.Mitarbeiter.Include(m => m.Schichten).ToListAsync();
        foreach (var m in mitarbeiter)
        {
            var stunden = m.Schichten.Where(s => s.Datum >= monthStart && s.Datum < monthEnd).Sum(s => s.Stunden);
            if (stunden > m.MaxStundenProMonat)
            {
                model.Warnungen.Add($"{m.VollerName}: {stunden:F2} h geplant, erlaubt {m.MaxStundenProMonat:F2} h");
            }
        }

        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
