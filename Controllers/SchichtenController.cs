using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Schichtplaner.Data;
using Schichtplaner.Models;
using Schichtplaner.Services;

namespace Schichtplaner.Controllers;

[Authorize]
public class SchichtenController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISchichtService _schichtService;

    public SchichtenController(ApplicationDbContext db, ISchichtService schichtService)
    {
        _db = db;
        _schichtService = schichtService;
    }

    public async Task<IActionResult> Index(int? mitarbeiterId, int? standortId, int? jahr, int? monat)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var selectedYear = jahr ?? today.Year;
        var selectedMonth = monat ?? today.Month;

        var start = new DateOnly(selectedYear, selectedMonth, 1);
        var end = start.AddMonths(1);

        var query = _db.Schichten
            .Include(s => s.Mitarbeiter)
            .Include(s => s.Standort)
            .Where(s => s.Datum >= start && s.Datum < end)
            .AsQueryable();

        if (mitarbeiterId.HasValue)
        {
            query = query.Where(s => s.MitarbeiterId == mitarbeiterId.Value);
        }

        if (standortId.HasValue)
        {
            query = query.Where(s => s.StandortId == standortId.Value);
        }

        var schichten = await query
            .OrderBy(s => s.Datum)
            .ThenBy(s => s.Slot)
            .ThenBy(s => s.Beginn)
            .ToListAsync();

        ViewBag.MitarbeiterId = new SelectList(
            await _db.Mitarbeiter
                .OrderBy(m => m.Nachname)
                .ThenBy(m => m.Vorname)
                .ToListAsync(),
            "Id",
            "VollerName",
            mitarbeiterId);

        ViewBag.StandortId = new SelectList(
            await _db.Standorte
                .OrderBy(s => s.Name)
                .ToListAsync(),
            "Id",
            "Name",
            standortId);

        ViewBag.Jahr = selectedYear;
        ViewBag.Monat = selectedMonth;

        return View(schichten);
    }

    private string GetSlotName(int slot)
    {
        return slot switch
        {
            1 => "Früh",
            2 => "Flex",
            3 => "Spät",
            _ => "?"
        };
    }
}