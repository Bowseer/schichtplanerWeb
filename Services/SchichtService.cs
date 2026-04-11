using Microsoft.EntityFrameworkCore;
using Schichtplaner.Data;
using Schichtplaner.Models;

namespace Schichtplaner.Services;

public class SchichtService : ISchichtService
{
    private readonly ApplicationDbContext _db;

    public SchichtService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Message)> ValidateSchichtAsync(Schicht schicht, int? excludeSchichtId = null)
    {
        if (schicht.Ende <= schicht.Beginn)
        {
            return (false, "Das Schichtende muss nach dem Beginn liegen.");
        }

        var mitarbeiter = await _db.Mitarbeiter
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == schicht.MitarbeiterId);

        if (mitarbeiter == null)
        {
            return (false, "Mitarbeiter nicht gefunden.");
        }

        if (!mitarbeiter.Aktiv)
        {
            return (false, "Mitarbeiter ist inaktiv.");
        }

        var schichtDatum = schicht.Datum;

        if (mitarbeiter.NurSamstag &&
            schichtDatum.ToDateTime(TimeOnly.MinValue).DayOfWeek != DayOfWeek.Saturday)
        {
            return (false, "Dieser Mitarbeiter darf nur samstags arbeiten.");
        }

        var overlapQuery = _db.Schichten.Where(s =>
            s.MitarbeiterId == schicht.MitarbeiterId &&
            s.Datum == schichtDatum);

        if (excludeSchichtId.HasValue)
        {
            overlapQuery = overlapQuery.Where(s => s.Id != excludeSchichtId.Value);
        }

        var sameDayShifts = await overlapQuery.ToListAsync();

        var hasOverlap = sameDayShifts.Any(existing =>
            schicht.Beginn < existing.Ende && schicht.Ende > existing.Beginn);

        if (hasOverlap)
        {
            return (false, "Der Mitarbeiter hat bereits eine überschneidende Schicht an diesem Tag.");
        }

        var monatsstunden = await GetMonatsstundenAsync(
            schicht.MitarbeiterId,
            schichtDatum.Year,
            schichtDatum.Month,
            excludeSchichtId);

        var neueGesamtstunden = monatsstunden + schicht.Stunden;

        if (neueGesamtstunden > mitarbeiter.MaxStundenProMonat)
        {
            return (false,
                $"Maximale Monatsarbeitszeit überschritten. Geplant: {neueGesamtstunden:F2} h / Erlaubt: {mitarbeiter.MaxStundenProMonat:F2} h");
        }

        return (true, "OK");
    }

    public async Task<decimal> GetMonatsstundenAsync(int mitarbeiterId, int jahr, int monat, int? excludeSchichtId = null)
    {
        var monthStart = new DateOnly(jahr, monat, 1);
        var monthEnd = monthStart.AddMonths(1);

        var query = _db.Schichten.Where(s =>
            s.MitarbeiterId == mitarbeiterId &&
            s.Datum >= monthStart &&
            s.Datum < monthEnd);

        if (excludeSchichtId.HasValue)
        {
            query = query.Where(s => s.Id != excludeSchichtId.Value);
        }

        var schichten = await query.ToListAsync();
        return schichten.Sum(s => s.Stunden);
    }
}