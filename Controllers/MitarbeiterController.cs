using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Schichtplaner.Data;
using Schichtplaner.Models;

namespace Schichtplaner.Controllers;

[Authorize]
public class MitarbeiterController : Controller
{
    private readonly ApplicationDbContext _db;

    public MitarbeiterController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? standortId)
    {
        var query = _db.Mitarbeiter
            .Include(m => m.Standort)
            .AsQueryable();

        if (standortId.HasValue)
        {
            query = query.Where(m => m.StandortId == standortId.Value);
        }

        var mitarbeiter = await query
            .OrderBy(m => m.Nachname)
            .ThenBy(m => m.Vorname)
            .ToListAsync();

        ViewBag.StandortFilter = new SelectList(
            await _db.Standorte.OrderBy(s => s.Name).ToListAsync(),
            "Id",
            "Name",
            standortId);

        return View(mitarbeiter);
    }

    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Create()
    {
        await LoadStandorteAsync();
        return View(new Mitarbeiter());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Create(Mitarbeiter model)
    {
        if (!ModelState.IsValid)
        {
            await LoadStandorteAsync(model.StandortId);
            return View(model);
        }

        _db.Mitarbeiter.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.Mitarbeiter.FindAsync(id);
        if (model == null) return NotFound();

        await LoadStandorteAsync(model.StandortId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Edit(int id, Mitarbeiter model)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadStandorteAsync(model.StandortId);
            return View(model);
        }

        _db.Update(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.Mitarbeiter.Include(m => m.Standort).FirstOrDefaultAsync(m => m.Id == id);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var model = await _db.Mitarbeiter.FindAsync(id);
        if (model == null) return NotFound();

        _db.Mitarbeiter.Remove(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadStandorteAsync(int? selectedId = null)
    {
        var standorte = await _db.Standorte.OrderBy(s => s.Name).ToListAsync();
        ViewBag.StandortId = new SelectList(standorte, "Id", "Name", selectedId);
    }
}
