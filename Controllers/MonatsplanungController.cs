using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    private readonly IFeiertagService _feiertagService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MonatsplanungController(
        ApplicationDbContext db,
        ISchichtService schichtService,
        IFeiertagService feiertagService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _schichtService = schichtService;
        _feiertagService = feiertagService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? jahr, int? monat, int? standortId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var targetYear = jahr ?? today.Year;
        var targetMonth = monat ?? today.Month;

        var monthStart = new DateOnly(targetYear, targetMonth, 1);
        var monthEnd = monthStart.AddMonths(1);

        var standorte = await _db.Standorte
            .OrderBy(s => s.Name)
            .ToListAsync();

        var user = await _userManager.GetUserAsync(User);

        var selectedStandortId = standortId
            ?? user?.DefaultStandortId
            ?? standorte.FirstOrDefault()?.Id;

        if (selectedStandortId == null)
        {
            return View(new MonatsplanViewModel
            {
                Jahr = targetYear,
                Monat = targetMonth
            });
        }

        var standort = await _db.Standorte
            .Include(s => s.StandardSlotZeiten)
            .FirstAsync(s => s.Id == selectedStandortId.Value);

        var slotOverrides = await _db.TagesSlotZeiten
            .Where(t => t.StandortId == selectedStandortId.Value &&
                        t.Datum >= monthStart &&
                        t.Datum < monthEnd)
            .ToListAsync();

        var mitarbeiter = await _db.Mitarbeiter
            .Where(m => m.StandortId == selectedStandortId.Value && m.Aktiv)
            .Include(m => m.Schichten.Where(s => s.Datum >= monthStart && s.Datum < monthEnd))
            .OrderBy(m => m.Nachname)
            .ThenBy(m => m.Vorname)
            .ToListAsync();

        var colorMap = mitarbeiter
            .Select((m, index) => new
            {
                MitarbeiterId = m.Id,
                Farbe = GetColorForIndex(index)
            })
            .ToDictionary(x => x.MitarbeiterId, x => x.Farbe);

        var feiertage = _feiertagService.GetFeiertage(targetYear, standort.Bundesland);

        var model = BuildMonatsplanViewModel(
            targetYear,
            targetMonth,
            selectedStandortId.Value,
            standorte,
            standort,
            mitarbeiter,
            slotOverrides,
            feiertage,
            colorMap);

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

        var standort = await _db.Standorte
            .Include(s => s.StandardSlotZeiten)
            .AsNoTracking()
            .FirstAsync(s => s.Id == request.StandortId);

        var overrideZeit = await _db.TagesSlotZeiten
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.StandortId == request.StandortId &&
                t.Datum == datum &&
                t.Slot == request.Slot);

        var slotZeit = overrideZeit != null
            ? (overrideZeit.Beginn, overrideZeit.Ende)
            : GetDefaultSlotZeit(standort, request.Slot, datum);

        var bestehendeSlotSchicht = await _db.Schichten
            .FirstOrDefaultAsync(s =>
                s.StandortId == request.StandortId &&
                s.Datum == datum &&
                s.Slot == request.Slot);

        int? oldMitarbeiterId = bestehendeSlotSchicht?.MitarbeiterId;
        Schicht? oldSourceSchicht = null;

        if (request.SourceStandortId.HasValue &&
            !string.IsNullOrWhiteSpace(request.SourceDatum) &&
            request.SourceSlot.HasValue &&
            DateOnly.TryParse(request.SourceDatum, out var sourceDatum))
        {
            oldSourceSchicht = await _db.Schichten.FirstOrDefaultAsync(s =>
                s.StandortId == request.SourceStandortId.Value &&
                s.Datum == sourceDatum &&
                s.Slot == request.SourceSlot.Value);

            if (oldSourceSchicht != null &&
                (oldSourceSchicht.StandortId != request.StandortId ||
                 oldSourceSchicht.Datum != datum ||
                 oldSourceSchicht.Slot != request.Slot))
            {
                _db.Schichten.Remove(oldSourceSchicht);
            }
        }

        if (bestehendeSlotSchicht == null)
        {
            var neueSchicht = new Schicht
            {
                MitarbeiterId = request.MitarbeiterId,
                StandortId = request.StandortId,
                Datum = datum,
                Slot = request.Slot,
                Beginn = slotZeit.Item1,
                Ende = slotZeit.Item2,
                PauseMinuten = 0
            };

            var validation = await _schichtService.ValidateSchichtAsync(
                neueSchicht,
                allowMaxHoursOverride: request.ForceMaxHoursOverride);

            if (!validation.Success)
            {
                if (validation.RequiresConfirmation)
                {
                    return StatusCode(StatusCodes.Status409Conflict, new
                    {
                        success = false,
                        message = validation.Message,
                        requiresConfirmation = true
                    });
                }

                return BadRequest(new { success = false, message = validation.Message });
            }

            _db.Schichten.Add(neueSchicht);
        }
        else
        {
            bestehendeSlotSchicht.MitarbeiterId = request.MitarbeiterId;
            bestehendeSlotSchicht.Beginn = slotZeit.Item1;
            bestehendeSlotSchicht.Ende = slotZeit.Item2;
            bestehendeSlotSchicht.PauseMinuten = 0;
            bestehendeSlotSchicht.Slot = request.Slot;
            bestehendeSlotSchicht.StandortId = request.StandortId;
            bestehendeSlotSchicht.Datum = datum;

            var validation = await _schichtService.ValidateSchichtAsync(
                bestehendeSlotSchicht,
                bestehendeSlotSchicht.Id,
                request.ForceMaxHoursOverride);

            if (!validation.Success)
            {
                if (validation.RequiresConfirmation)
                {
                    return StatusCode(StatusCodes.Status409Conflict, new
                    {
                        success = false,
                        message = validation.Message,
                        requiresConfirmation = true
                    });
                }

                return BadRequest(new { success = false, message = validation.Message });
            }
        }

        await _db.SaveChangesAsync();

        var changedEmployeeIds = new List<int> { request.MitarbeiterId };

        if (oldMitarbeiterId.HasValue && oldMitarbeiterId.Value != request.MitarbeiterId)
        {
            changedEmployeeIds.Add(oldMitarbeiterId.Value);
        }

        if (oldSourceSchicht != null && oldSourceSchicht.MitarbeiterId != request.MitarbeiterId)
        {
            changedEmployeeIds.Add(oldSourceSchicht.MitarbeiterId);
        }

        var employeeRest = await BuildEmployeeRestMap(
            request.StandortId,
            datum.Year,
            datum.Month,
            changedEmployeeIds);

        return Ok(new
        {
            success = true,
            slot = new
            {
                standortId = request.StandortId,
                datum = datum.ToString("yyyy-MM-dd"),
                slot = request.Slot,
                mitarbeiterId = request.MitarbeiterId,
                mitarbeiterName = mitarbeiter.VollerName,
                farbe = await GetEmployeeColor(request.StandortId, request.MitarbeiterId)
            },
            removedSource = oldSourceSchicht != null
                ? new
                {
                    standortId = oldSourceSchicht.StandortId,
                    datum = oldSourceSchicht.Datum.ToString("yyyy-MM-dd"),
                    slot = oldSourceSchicht.Slot
                }
                : null,
            employeeRest
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> RemoveSlot([FromBody] RemoveSlotRequest request)
    {
        if (request.StandortId <= 0 || request.Slot is < 1 or > 3)
        {
            return BadRequest(new { success = false, message = "Ungültige Daten." });
        }

        if (!DateOnly.TryParse(request.Datum, out var datum))
        {
            return BadRequest(new { success = false, message = "Ungültiges Datum." });
        }

        var schicht = await _db.Schichten.FirstOrDefaultAsync(s =>
            s.StandortId == request.StandortId &&
            s.Datum == datum &&
            s.Slot == request.Slot);

        if (schicht == null)
        {
            return Ok(new { success = true });
        }

        var mitarbeiterId = schicht.MitarbeiterId;

        _db.Schichten.Remove(schicht);
        await _db.SaveChangesAsync();

        var employeeRest = await BuildEmployeeRestMap(
            request.StandortId,
            datum.Year,
            datum.Month,
            new List<int> { mitarbeiterId });

        return Ok(new
        {
            success = true,
            slot = new
            {
                standortId = request.StandortId,
                datum = datum.ToString("yyyy-MM-dd"),
                slot = request.Slot,
                mitarbeiterId = (int?)null,
                mitarbeiterName = (string?)null,
                farbe = (string?)null
            },
            employeeRest
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> SaveSlotTime([FromBody] SaveSlotTimeRequest request)
    {
        if (request.StandortId <= 0 || request.Slot is < 1 or > 3)
        {
            return BadRequest(new { success = false, message = "Ungültige Daten." });
        }

        if (!TimeSpan.TryParse(request.Beginn, out var beginn) || !TimeSpan.TryParse(request.Ende, out var ende))
        {
            return BadRequest(new { success = false, message = "Ungültige Uhrzeit." });
        }

        if (ende <= beginn)
        {
            return BadRequest(new { success = false, message = "Ende muss nach Beginn liegen." });
        }

        var standort = await _db.Standorte
            .Include(s => s.StandardSlotZeiten)
            .FirstOrDefaultAsync(s => s.Id == request.StandortId);

        if (standort == null)
        {
            return NotFound(new { success = false, message = "Standort nicht gefunden." });
        }

        if (string.IsNullOrWhiteSpace(request.Datum))
        {
            return BadRequest(new
            {
                success = false,
                message = "Standardzeiten werden jetzt im Standort gepflegt. Bitte Standort bearbeiten."
            });
        }

        if (!DateOnly.TryParse(request.Datum, out var datum))
        {
            return BadRequest(new { success = false, message = "Ungültiges Datum." });
        }

        var overrideZeit = await _db.TagesSlotZeiten
            .FirstOrDefaultAsync(t =>
                t.StandortId == request.StandortId &&
                t.Datum == datum &&
                t.Slot == request.Slot);

        if (overrideZeit == null)
        {
            overrideZeit = new TagesSlotZeit
            {
                StandortId = request.StandortId,
                Datum = datum,
                Slot = request.Slot,
                Beginn = beginn,
                Ende = ende
            };
            _db.TagesSlotZeiten.Add(overrideZeit);
        }
        else
        {
            overrideZeit.Beginn = beginn;
            overrideZeit.Ende = ende;
        }

        var schichten = await _db.Schichten
            .Where(s => s.StandortId == request.StandortId &&
                        s.Datum == datum &&
                        s.Slot == request.Slot)
            .ToListAsync();

        foreach (var schicht in schichten)
        {
            schicht.Beginn = beginn;
            schicht.Ende = ende;
        }

        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    private MonatsplanViewModel BuildMonatsplanViewModel(
        int targetYear,
        int targetMonth,
        int selectedStandortId,
        List<Standort> standorte,
        Standort standort,
        List<Mitarbeiter> mitarbeiter,
        List<TagesSlotZeit> slotOverrides,
        Dictionary<DateOnly, string> feiertage,
        Dictionary<int, string> colorMap)
    {
        var monthStart = new DateOnly(targetYear, targetMonth, 1);
        var monthEnd = monthStart.AddMonths(1);

        var model = new MonatsplanViewModel
        {
            Jahr = targetYear,
            Monat = targetMonth,
            StandortId = selectedStandortId,
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
                    Farbe = colorMap[m.Id]
                };
            }).ToList(),
            StandardSlotZeiten = BuildStandardSlotZeitenForDisplay(standort),
            FeiertageImMonat = feiertage
                .Where(f => f.Key >= monthStart && f.Key < monthEnd)
                .OrderBy(f => f.Key)
                .Select(f => new FeiertagListeDto
                {
                    Datum = f.Key.ToString("dd.MM.yyyy"),
                    Name = f.Value
                })
                .ToList()
        };

        var schichtenLookup = mitarbeiter
            .SelectMany(m => m.Schichten.Select(s => new
            {
                MitarbeiterId = m.Id,
                MitarbeiterName = m.VollerName,
                Schicht = s
            }))
            .ToList();

        var firstDayOfWeek = (int)monthStart.DayOfWeek;
        if (firstDayOfWeek == 0)
        {
            firstDayOfWeek = 7;
        }

        var kalenderStart = monthStart.AddDays(-(firstDayOfWeek - 1));
        var wochen = new List<KalenderWocheDto>();

        for (int wocheIndex = 0; wocheIndex < 6; wocheIndex++)
        {
            var weekStart = kalenderStart.AddDays(wocheIndex * 7);
            var weekDates = Enumerable.Range(0, 7).Select(offset => weekStart.AddDays(offset)).ToList();

            if (weekDates.All(d => d.Month != targetMonth))
            {
                continue;
            }

            var woche = new KalenderWocheDto
            {
                KalenderWoche = ISOWeek.GetWeekOfYear(weekStart.ToDateTime(TimeOnly.MinValue))
            };

            foreach (var currentDate in weekDates)
            {
                feiertage.TryGetValue(currentDate, out var feiertagName);

                var tag = new KalenderTagDto
                {
                    Datum = currentDate,
                    IstSonntag = currentDate.DayOfWeek == DayOfWeek.Sunday,
                    IstFeiertag = feiertagName != null,
                    FeiertagName = feiertagName
                };

                for (int slot = 1; slot <= 3; slot++)
                {
                    var belegung = schichtenLookup.FirstOrDefault(x =>
                        x.Schicht.Datum == currentDate &&
                        x.Schicht.Slot == slot);

                    var slotZeit = GetSlotZeitForDate(standort, slotOverrides, currentDate, slot);

                    tag.Slots.Add(new KalenderSlotDto
                    {
                        Slot = slot,
                        SlotName = GetSlotName(slot),
                        Beginn = slotZeit.Beginn.ToString(@"hh\:mm"),
                        Ende = slotZeit.Ende.ToString(@"hh\:mm"),
                        MitarbeiterId = belegung?.MitarbeiterId,
                        MitarbeiterName = belegung?.MitarbeiterName,
                        Farbe = belegung != null ? colorMap[belegung.MitarbeiterId] : null
                    });
                }

                woche.Tage.Add(tag);
            }

            wochen.Add(woche);
        }

        model.Wochen = wochen;
        return model;
    }

    private List<StandardSlotZeitDto> BuildStandardSlotZeitenForDisplay(Standort standort)
    {
        var sampleMonday = new DateOnly(2026, 1, 5);

        return new List<StandardSlotZeitDto>
        {
            BuildStandardSlotZeitDto(standort, 1, sampleMonday),
            BuildStandardSlotZeitDto(standort, 2, sampleMonday),
            BuildStandardSlotZeitDto(standort, 3, sampleMonday)
        };
    }

    private StandardSlotZeitDto BuildStandardSlotZeitDto(Standort standort, int slot, DateOnly datum)
    {
        var zeit = GetDefaultSlotZeit(standort, slot, datum);

        return new StandardSlotZeitDto
        {
            Slot = slot,
            SlotName = GetSlotName(slot),
            Beginn = zeit.Beginn.ToString(@"hh\:mm"),
            Ende = zeit.Ende.ToString(@"hh\:mm")
        };
    }

    private async Task<Dictionary<string, decimal>> BuildEmployeeRestMap(
        int standortId,
        int jahr,
        int monat,
        List<int> employeeIds)
    {
        var employees = await _db.Mitarbeiter
            .Where(m => m.StandortId == standortId && employeeIds.Contains(m.Id))
            .ToListAsync();

        var result = new Dictionary<string, decimal>();

        foreach (var employee in employees)
        {
            var stunden = await _schichtService.GetMonatsstundenAsync(employee.Id, jahr, monat);
            result[employee.Id.ToString()] = employee.MaxStundenProMonat - stunden;
        }

        return result;
    }

    private async Task<string> GetEmployeeColor(int standortId, int mitarbeiterId)
    {
        var mitarbeiterIds = await _db.Mitarbeiter
            .Where(m => m.StandortId == standortId && m.Aktiv)
            .OrderBy(m => m.Nachname)
            .ThenBy(m => m.Vorname)
            .Select(m => m.Id)
            .ToListAsync();

        var index = mitarbeiterIds.FindIndex(id => id == mitarbeiterId);
        if (index < 0)
        {
            index = 0;
        }

        return GetColorForIndex(index);
    }

    private static (TimeSpan Beginn, TimeSpan Ende) GetSlotZeitForDate(
        Standort standort,
        List<TagesSlotZeit> overrides,
        DateOnly datum,
        int slot)
    {
        var overrideZeit = overrides.FirstOrDefault(t => t.Datum == datum && t.Slot == slot);
        if (overrideZeit != null)
        {
            return (overrideZeit.Beginn, overrideZeit.Ende);
        }

        return GetDefaultSlotZeit(standort, slot, datum);
    }

    private static (TimeSpan Beginn, TimeSpan Ende) GetDefaultSlotZeit(
        Standort standort,
        int slot,
        DateOnly datum)
    {
        var wochentag = datum.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)datum.DayOfWeek;

        var standard = standort.StandardSlotZeiten
            .FirstOrDefault(s => s.Wochentag == wochentag && s.Slot == slot && s.Aktiv);

        if (standard == null)
        {
            return slot switch
            {
                1 => (new TimeSpan(10, 0, 0), new TimeSpan(17, 0, 0)),
                2 => (new TimeSpan(12, 0, 0), new TimeSpan(16, 0, 0)),
                3 => (new TimeSpan(15, 0, 0), new TimeSpan(20, 0, 0)),
                _ => throw new ArgumentOutOfRangeException(nameof(slot))
            };
        }

        return (standard.Beginn, standard.Ende);
    }

    private static string GetSlotName(int slot)
    {
        return slot switch
        {
            1 => "Früh",
            2 => "Flex",
            3 => "Spät",
            _ => "?"
        };
    }

    private static string GetColorForIndex(int index)
    {
        double hue = (index * 137.508) % 360;
        return $"hsl({hue:F0}, 70%, 75%)";
    }
}

public class AssignSlotRequest
{
    public int StandortId { get; set; }
    public int MitarbeiterId { get; set; }
    public string Datum { get; set; } = string.Empty;
    public int Slot { get; set; }
    public bool ForceMaxHoursOverride { get; set; }
    public int? SourceStandortId { get; set; }
    public string? SourceDatum { get; set; }
    public int? SourceSlot { get; set; }
}

public class RemoveSlotRequest
{
    public int StandortId { get; set; }
    public string Datum { get; set; } = string.Empty;
    public int Slot { get; set; }
}

public class SaveSlotTimeRequest
{
    public int StandortId { get; set; }
    public string? Datum { get; set; }
    public int Slot { get; set; }
    public string Beginn { get; set; } = string.Empty;
    public string Ende { get; set; } = string.Empty;
}
