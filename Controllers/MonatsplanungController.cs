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

    public async Task<IActionResult> Index(int? jahr, int? monat, int? standortId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var targetYear = jahr ?? today.Year;
        var targetMonth = monat ?? today.Month;

        var start = new DateOnly(targetYear, targetMonth, 1);
        var end = start.AddMonths(1);

        var standorte = await _db.Standorte.ToListAsync();
        var selectedStandort = standortId ?? standorte.FirstOrDefault()?.Id;

        var mitarbeiter = await _db.Mitarbeiter
            .Where(m => m.StandortId == selectedStandort && m.Aktiv)
            .Include(m => m.Schichten.Where(s => s.Datum >= start && s.Datum < end))
            .ToListAsync();

        var random = new Random();

        var model = new MonatsplanViewModel
        {
            Jahr = targetYear,
            Monat = targetMonth,
            StandortId = selectedStandort,
            Standorte = standorte.Select(s => new StandortDto
            {
                Id = s.Id,
                Name = s.Name
            }).ToList(),
            Mitarbeiter = mitarbeiter.Select(m =>
            {
                var geplant = m.Schichten.Sum(s => s.Stunden);

                return new MitarbeiterSidebarDto
                {
                    Id = m.Id,
                    Name = m.VollerName,
                    Reststunden = m.MaxStundenProMonat - geplant,
                    Farbe = $"hsl({random.Next(0, 360)},70%,70%)"
                };
            }).ToList()
        };

        var firstDayOfWeek = (int)start.DayOfWeek;
        if (firstDayOfWeek == 0) firstDayOfWeek = 7;

        var kalenderStart = start.AddDays(-(firstDayOfWeek - 1));

        var wochen = new List<KalenderWocheDto>();

        for (int w = 0; w < 6; w++)
        {
            var woche = new KalenderWocheDto();

            for (int d = 0; d < 7; d++)
            {
                var currentDate = kalenderStart.AddDays(w * 7 + d);

                var tag = new KalenderTagDto
                {
                    Datum = currentDate,
                    IstSonntag = currentDate.DayOfWeek == DayOfWeek.Sunday,
                    IstFeiertag = false
                };

                for (int s = 1; s <= 3; s++)
                {
                    var schicht = mitarbeiter
                        .SelectMany(m => m.Schichten)
                        .FirstOrDefault(x => x.Datum == currentDate && x.Slot == s);

                    tag.Slots.Add(new KalenderSlotDto
                    {
                        Slot = s,
                        MitarbeiterName = schicht?.Mitarbeiter?.VollerName,
                        Farbe = schicht != null
                            ? model.Mitarbeiter.First(m => m.Id == schicht.MitarbeiterId).Farbe
                            : null
                    });
                }

                woche.Tage.Add(tag);
            }

            wochen.Add(woche);
        }

        model.Wochen = wochen;

        return View(model);
    }
}