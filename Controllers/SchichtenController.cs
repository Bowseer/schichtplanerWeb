using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Schichtplaner.Data;
using Schichtplaner.Models;
using Schichtplaner.Models.ViewModels;
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

    public async Task<IActionResult> Index()
    {
        var schichten = await _db.Schichten.Include(s => s.Mitarbeiter).Include(s => s.Standort)
            .OrderByDescending(s => s.Datum).ThenBy(s => s.Beginn).ToListAsync();
        return View(schichten);
    }

    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Create()
    {
        var vm = new SchichtCreateViewModel
        {
            Datum = DateTime.Today,
            Beginn = new TimeSpan(9, 0, 0),
            Ende = new TimeSpan(17, 0, 0)
        };
        await LoadListsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Create(SchichtCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await LoadListsAsync(vm);
            return View(vm);
        }

        var entity = new Schicht
        {
            MitarbeiterId = vm.MitarbeiterId,
            StandortId = vm.StandortId,
            Datum = vm.Datum.Date,
            Beginn = vm.Beginn,
            Ende = vm.Ende,
            PauseMinuten = vm.PauseMinuten
        };

        var validation = await _schichtService.ValidateSchichtAsync(entity);
        if (!validation.Success)
        {
            ModelState.AddModelError(string.Empty, validation.Message);
            await LoadListsAsync(vm);
            return View(vm);
        }

        _db.Schichten.Add(entity);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _db.Schichten.FindAsync(id);
        if (entity == null) return NotFound();

        var vm = new SchichtEditViewModel
        {
            Id = entity.Id,
            MitarbeiterId = entity.MitarbeiterId,
            StandortId = entity.StandortId,
            Datum = entity.Datum,
            Beginn = entity.Beginn,
            Ende = entity.Ende,
            PauseMinuten = entity.PauseMinuten
        };

        await LoadListsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Edit(int id, SchichtEditViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadListsAsync(vm);
            return View(vm);
        }

        var entity = await _db.Schichten.FindAsync(id);
        if (entity == null) return NotFound();

        entity.MitarbeiterId = vm.MitarbeiterId;
        entity.StandortId = vm.StandortId;
        entity.Datum = vm.Datum.Date;
        entity.Beginn = vm.Beginn;
        entity.Ende = vm.Ende;
        entity.PauseMinuten = vm.PauseMinuten;

        var validation = await _schichtService.ValidateSchichtAsync(entity, entity.Id);
        if (!validation.Success)
        {
            ModelState.AddModelError(string.Empty, validation.Message);
            await LoadListsAsync(vm);
            return View(vm);
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Schichten.Include(s => s.Mitarbeiter).Include(s => s.Standort).FirstOrDefaultAsync(s => s.Id == id);
        return entity == null ? NotFound() : View(entity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var entity = await _db.Schichten.FindAsync(id);
        if (entity == null) return NotFound();

        _db.Schichten.Remove(entity);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadListsAsync(SchichtCreateViewModel vm)
    {
        vm.MitarbeiterListe = await _db.Mitarbeiter.Where(m => m.Aktiv)
            .OrderBy(m => m.Nachname).ThenBy(m => m.Vorname)
            .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.VollerName })
            .ToListAsync();

        vm.StandortListe = await _db.Standorte.OrderBy(s => s.Name)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();
    }

    private async Task LoadListsAsync(SchichtEditViewModel vm)
    {
        vm.MitarbeiterListe = await _db.Mitarbeiter.Where(m => m.Aktiv)
            .OrderBy(m => m.Nachname).ThenBy(m => m.Vorname)
            .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.VollerName })
            .ToListAsync();

        vm.StandortListe = await _db.Standorte.OrderBy(s => s.Name)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();
    }
}
