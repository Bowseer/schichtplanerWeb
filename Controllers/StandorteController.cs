using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schichtplaner.Data;
using Schichtplaner.Models;

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
        return View(await _db.Standorte.OrderBy(s => s.Name).ToListAsync());
    }

    [Authorize(Roles = "Admin,Planer")]
    public IActionResult Create() => View(new Standort());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Create(Standort model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Standorte.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Edit(int id)
    {
        var standort = await _db.Standorte.FindAsync(id);
        return standort == null ? NotFound() : View(standort);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Planer")]
    public async Task<IActionResult> Edit(int id, Standort model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);
        _db.Update(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var standort = await _db.Standorte.FindAsync(id);
        return standort == null ? NotFound() : View(standort);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var standort = await _db.Standorte.FindAsync(id);
        if (standort == null) return NotFound();

        _db.Standorte.Remove(standort);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
