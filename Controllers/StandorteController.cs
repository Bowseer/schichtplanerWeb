using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schichtplaner.Data;
using Schichtplaner.Models;
using Schichtplaner.Models.ViewModels;

namespace Schichtplaner.Controllers;

[Authorize]
public class StandorteController : Controller
{
    private readonly ApplicationDbContext _db;

    public StandorteController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var standorte = await _db.Standorte
            .OrderBy(s => s.Name)
            .ToListAsync();

        return View(standorte);
    }

    [Authorize(Roles = "Admin,Planer")]
    public IActionResult Create()
    {
        var vm = BuildDefaultViewModel();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Create(StandortEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            EnsureSlotMetadata(vm);
            return View(vm);
        }

        var standort = new Standort
        {
            Name = vm.Name,
            Adresse = vm.Adresse,
            Bundesland = vm.Bundesland
        };

        _db.Standorte.Add(standort);
        await _db.SaveChangesAsync();

        var slotZeiten = BuildStandortSlotZeiten(vm, standort.Id);
        _db.StandortSlotZeiten.AddRange(slotZeiten);

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Edit(int id)
    {
        var standort = await _db.Standorte
            .Include(s => s.StandardSlotZeiten)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (standort == null)
        {
            return NotFound();
        }

        var vm = new StandortEditViewModel
        {
            Id = standort.Id,
            Name = standort.Name,
            Adresse = standort.Adresse,
            Bundesland = standort.Bundesland,
            SlotZeiten = BuildViewModelSlotZeiten(standort.StandardSlotZeiten.ToList())
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Edit(int id, StandortEditViewModel vm)
    {
        if (id != vm.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            EnsureSlotMetadata(vm);
            return View(vm);
        }

        var standort = await _db.Standorte
            .Include(s => s.StandardSlotZeiten)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (standort == null)
        {
            return NotFound();
        }

        standort.Name = vm.Name;
        standort.Adresse = vm.Adresse;
        standort.Bundesland = vm.Bundesland;

        _db.StandortSlotZeiten.RemoveRange(standort.StandardSlotZeiten);

        var neueZeiten = BuildStandortSlotZeiten(vm, standort.Id);
        _db.StandortSlotZeiten.AddRange(neueZeiten);

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var standort = await _db.Standorte.FirstOrDefaultAsync(s => s.Id == id);
        if (standort == null)
        {
            return NotFound();
        }

        return View(standort);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var standort = await _db.Standorte
            .Include(s => s.StandardSlotZeiten)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (standort == null)
        {
            return NotFound();
        }

        _db.StandortSlotZeiten.RemoveRange(standort.StandardSlotZeiten);
        _db.Standorte.Remove(standort);

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private static StandortEditViewModel BuildDefaultViewModel()
    {
        return new StandortEditViewModel
        {
            SlotZeiten = BuildDefaultSlotZeiten()
        };
    }

    private static List<StandortSlotZeitEditItem> BuildDefaultSlotZeiten()
    {
        var result = new List<StandortSlotZeitEditItem>();

        for (int wochentag = 1; wochentag <= 7; wochentag++)
        {
            result.Add(new StandortSlotZeitEditItem
            {
                Wochentag = wochentag,
                WochentagName = GetWochentagName(wochentag),
                Slot = 1,
                SlotName = "Früh",
                Aktiv = true,
                Beginn = "10:00",
                Ende = "17:00"
            });

            result.Add(new StandortSlotZeitEditItem
            {
                Wochentag = wochentag,
                WochentagName = GetWochentagName(wochentag),
                Slot = 2,
                SlotName = "Flex",
                Aktiv = false,
                Beginn = "12:00",
                Ende = "16:00"
            });

            result.Add(new StandortSlotZeitEditItem
            {
                Wochentag = wochentag,
                WochentagName = GetWochentagName(wochentag),
                Slot = 3,
                SlotName = "Spät",
                Aktiv = true,
                Beginn = "15:00",
                Ende = "20:00"
            });
        }

        return result;
    }

    private static List<StandortSlotZeitEditItem> BuildViewModelSlotZeiten(List<StandortSlotZeit> zeiten)
    {
        var result = new List<StandortSlotZeitEditItem>();

        for (int wochentag = 1; wochentag <= 7; wochentag++)
        {
            for (int slot = 1; slot <= 3; slot++)
            {
                var eintrag = zeiten.FirstOrDefault(z => z.Wochentag == wochentag && z.Slot == slot);

                result.Add(new StandortSlotZeitEditItem
                {
                    Wochentag = wochentag,
                    WochentagName = GetWochentagName(wochentag),
                    Slot = slot,
                    SlotName = GetSlotName(slot),
                    Aktiv = eintrag?.Aktiv ?? (slot != 2),
                    Beginn = (eintrag?.Beginn ?? GetDefaultBeginn(slot)).ToString(@"hh\:mm"),
                    Ende = (eintrag?.Ende ?? GetDefaultEnde(slot)).ToString(@"hh\:mm")
                });
            }
        }

        return result;
    }

    private static List<StandortSlotZeit> BuildStandortSlotZeiten(StandortEditViewModel vm, int standortId)
    {
        var result = new List<StandortSlotZeit>();

        foreach (var item in vm.SlotZeiten)
        {
            if (!TimeSpan.TryParse(item.Beginn, out var beginn))
            {
                beginn = GetDefaultBeginn(item.Slot);
            }

            if (!TimeSpan.TryParse(item.Ende, out var ende))
            {
                ende = GetDefaultEnde(item.Slot);
            }

            result.Add(new StandortSlotZeit
            {
                StandortId = standortId,
                Wochentag = item.Wochentag,
                Slot = item.Slot,
                Aktiv = item.Aktiv,
                Beginn = beginn,
                Ende = ende
            });
        }

        return result;
    }

    private static void EnsureSlotMetadata(StandortEditViewModel vm)
    {
        foreach (var item in vm.SlotZeiten)
        {
            item.WochentagName = GetWochentagName(item.Wochentag);
            item.SlotName = GetSlotName(item.Slot);
        }
    }

    private static string GetWochentagName(int wochentag)
    {
        return wochentag switch
        {
            1 => "Montag",
            2 => "Dienstag",
            3 => "Mittwoch",
            4 => "Donnerstag",
            5 => "Freitag",
            6 => "Samstag",
            7 => "Sonntag",
            _ => "?"
        };
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

    private static TimeSpan GetDefaultBeginn(int slot)
    {
        return slot switch
        {
            1 => new TimeSpan(10, 0, 0),
            2 => new TimeSpan(12, 0, 0),
            3 => new TimeSpan(15, 0, 0),
            _ => new TimeSpan(8, 0, 0)
        };
    }

    private static TimeSpan GetDefaultEnde(int slot)
    {
        return slot switch
        {
            1 => new TimeSpan(17, 0, 0),
            2 => new TimeSpan(16, 0, 0),
            3 => new TimeSpan(20, 0, 0),
            _ => new TimeSpan(12, 0, 0)
        };
    }
}