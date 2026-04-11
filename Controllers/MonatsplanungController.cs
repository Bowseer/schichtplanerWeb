using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schichtplaner.Data;
using Schichtplaner.Models.ViewModels;

namespace Schichtplaner.Controllers;

[Authorize]
public class MonatsplanungController : Controller
{
    private readonly ApplicationDbContext _db;

    public MonatsplanungController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? jahr, int? monat)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var targetYear = jahr ?? today.Year;
        var targetMonth = monat ?? today.Month;

        var start = new DateOnly(jahr, monat, 1);
        var end = start.AddMonths(1);

        

        var mitarbeiter = await _db.Mitarbeiter
            .Include(m => m.Standort)
            .Include(m => m.Schichten.Where(s => s.Datum >= start && s.Datum < end))
            .Where(m => m.Aktiv)
            .OrderBy(m => m.Nachname)
            .ThenBy(m => m.Vorname)
            .ToListAsync();

        var model = new MonatsplanViewModel
        {
            Jahr = targetYear,
            Monat = targetMonth,
            MitarbeiterRows = mitarbeiter.Select(m => new MitarbeiterMonatsplanRow
            {
                MitarbeiterId = m.Id,
                MitarbeiterName = m.VollerName,
                StandortName = m.Standort?.Name ?? "-",
                MaxStunden = m.MaxStundenProMonat,
                GeplanteStunden = m.Schichten.Sum(s => s.Stunden),
                Ueberschritten = m.Schichten.Sum(s => s.Stunden) > m.MaxStundenProMonat,
                Schichten = m.Schichten.OrderBy(s => s.Datum).ThenBy(s => s.Beginn).ToList()
            }).ToList()
        };

        return View(model);
    }
}