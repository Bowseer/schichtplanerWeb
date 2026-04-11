using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schichtplaner.Data;
using Schichtplaner.Models;
using Schichtplaner.Models.ViewModels;
using Schichtplaner.Services;

namespace Schichtplaner.Controllers;

[Authorize]
public class MonatsplanungController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISchichtService _schichtService;

    public MonatsplanungController(ApplicationDbContext db, ISchichtService schichtService)
    {
        _db = db;
        _schichtService = schichtService;
    }

    public async Task<IActionResult> Index(int? jahr, int? monat, int? standortId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var targetYear = jahr ?? today.Year;
        var targetMonth = monat ?? today.Month;

        var start = new DateOnly(targetYear, targetMonth, 1);
        var end = start.AddMonths(1);

        var standorte = await _db.Standorte
            .OrderBy(s => s.Name)
            .ToListAsync();

        var selectedStandort = standortId ?? standorte.FirstOrDefault()?.Id;

        var mitarbeiter = await _db.Mitarbeiter
            .Where(m => m.StandortId == selectedStandort && m.Aktiv)
            .Include(m => m.Schichten.Where(s => s.Datum >= start && s.Datum < end))
            .OrderBy(m => m.Nachname)
            .ThenBy(m => m.Vorname)
            .ToListAsync();

        var model = new MonatsplanViewModel
        {
            Jahr = targetYear,
            Monat = targetMonth,
            StandortId = selectedStandort,
            Standorte = standorte.Select(s => new StandortDto
            {
                Id = s.Id,
                Name = s.Name
            }).ToList()
        };

        model.Mitarbeiter = mitarbeiter.Select(m =>
        {
            var geplant = m.Schichten.Sum(s => s.Stunden);

            return new MitarbeiterSidebarDto
            {
                Id = m.Id,
                Name = m.VollerName,
                Reststunden = m.MaxStundenProMonat - geplant,
                Farbe = GetColorForEmployee(m.Id)
            };
        }).ToList();

        var schichtenLookup = mitarbeiter
            .SelectMany(m => m.Schichten.Select(s => new
            {
                MitarbeiterId = m.Id,
                MitarbeiterName = m.VollerName,
                Schicht = s
            }))
            .ToList();

        var firstDayOfWeek = (int)start.DayOfWeek;
        if (firstDayOfWeek == 0)
        {
            firstDayOfWeek = 7;
        }

        var kalenderStart = start.AddDays(-(firstDayOfWeek - 1));
        var wochen = new List<KalenderWocheDto>();

        for (int wocheIndex = 0; wocheIndex < 6; wocheIndex++)
        {
            var woche = new KalenderWocheDto();

            for (int tagIndex = 0; tagIndex < 7; tagIndex++)
            {
                var currentDate = kalenderStart.AddDays(wocheIndex * 7 + tagIndex);

                var tag = new KalenderTagDto
                {
                    Datum = currentDate,
                    IstSonntag = currentDate.DayOfWeek == DayOfWeek.Sunday,
                    IstFeiertag = false,
                    FeiertagName = null
                };

                for (int slot = 1; slot <= 3; slot++)
                {
                    var belegung = schichtenLookup.FirstOrDefault(x =>
                        x.Schicht.Datum == currentDate &&
                        x.Schicht.Slot == slot);

                    tag.Slots.Add(new KalenderSlotDto
                    {
                        Slot = slot,
                        MitarbeiterId = belegung?.MitarbeiterId,
                        MitarbeiterName = belegung?.MitarbeiterName,
                        Farbe = belegung != null
                            ? GetColorForEmployee(belegung.MitarbeiterId)
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> AssignSlot([FromBody] AssignSlotRequest request)
    {
        if (request.StandortId <= 0 || request.MitarbeiterId <= 0 || request.Slot is < 1 or > 3)
        {
            return BadRequest(new { success = false, message = "Ungültige Daten." });
        }

        if (!DateOnly.TryParse(request.Datum, out var datum))
        {
            return BadRequest(new { success = false, message = "Ungültiges Datum." });
        }

        var mitarbeiter = await _db.Mitarbeiter
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MitarbeiterId && m.Aktiv);

        if (mitarbeiter == null)
        {
            return NotFound(new { success = false, message = "Mitarbeiter nicht gefunden." });
        }

        if (mitarbeiter.StandortId != request.StandortId)
        {
            return BadRequest(new { success = false, message = "Mitarbeiter gehört nicht zu diesem Standort." });
        }

        var bestehendeSlotSchicht = await _db.Schichten
            .FirstOrDefaultAsync(s =>
                s.StandortId == request.StandortId &&
                s.Datum == datum &&
                s.Slot == request.Slot);

        var (beginn, ende) = GetSlotTimes(request.Slot);

        if (bestehendeSlotSchicht == null)
        {
            var neueSchicht = new Schicht
            {
                MitarbeiterId = request.MitarbeiterId,
                StandortId = request.StandortId,
                Datum = datum,
                Slot = request.Slot,
                Beginn = beginn,
                Ende = ende,
                PauseMinuten = 0
            };

            var validation = await _schichtService.ValidateSchichtAsync(neueSchicht);
            if (!validation.Success)
            {
                return BadRequest(new { success = false, message = validation.Message });
            }

            _db.Schichten.Add(neueSchicht);
        }
        else
        {
            bestehendeSlotSchicht.MitarbeiterId = request.MitarbeiterId;
            bestehendeSlotSchicht.Beginn = beginn;
            bestehendeSlotSchicht.Ende = ende;
            bestehendeSlotSchicht.PauseMinuten = 0;
            bestehendeSlotSchicht.Slot = request.Slot;
            bestehendeSlotSchicht.StandortId = request.StandortId;
            bestehendeSlotSchicht.Datum = datum;

            var validation = await _schichtService.ValidateSchichtAsync(bestehendeSlotSchicht, bestehendeSlotSchicht.Id);
            if (!validation.Success)
            {
                return BadRequest(new { success = false, message = validation.Message });
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    private static string GetColorForEmployee(int mitarbeiterId)
    {
        var hue = (mitarbeiterId * 57) % 360;
        return $"hsl({hue}, 70%, 75%)";
    }

    private static (TimeSpan Beginn, TimeSpan Ende) GetSlotTimes(int slot)
    {
        return slot switch
        {
            1 => (new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0)),
            2 => (new TimeSpan(12, 0, 0), new TimeSpan(16, 0, 0)),
            3 => (new TimeSpan(16, 0, 0), new TimeSpan(20, 0, 0)),
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };
    }
}

public class AssignSlotRequest
{
    public int StandortId { get; set; }
    public int MitarbeiterId { get; set; }
    public string Datum { get; set; } = string.Empty;
    public int Slot { get; set; }
}