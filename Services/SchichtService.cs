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

    public async Task<SchichtValidationResult> ValidateSchichtAsync(
        Schicht schicht,
        int? excludeSchichtId = null,
        bool allowMaxHoursOverride = false)
    {
        if (schicht.Ende <= schicht.Beginn)
        {
            return new SchichtValidationResult
            {
                Success = false,
                Message = "Das Schichtende muss nach dem Beginn liegen."
            };
        }

        var mitarbeiter = await _db.Mitarbeiter
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == schicht.MitarbeiterId);

        if (mitarbeiter == null)
        {
            return new SchichtValidationResult
            {
                Success = false,
                Message = "Mitarbeiter nicht gefunden."
            };
        }

        if (!mitarbeiter.Aktiv)
        {
            return new SchichtValidationResult
            {
                Success = false,
                Message = "Mitarbeiter ist inaktiv."
            };
        }

        if (mitarbeiter.NurSamstag && schicht.Datum.DayOfWeek != DayOfWeek.Saturday)
        {
            return new SchichtValidationResult
            {
                Success = false,
                Message = "Dieser Mitarbeiter darf nur samstags arbeiten."
            };
        }

        var overlapQuery = _db.Schichten.Where(s =>
            s.MitarbeiterId == schicht.MitarbeiterId &&
            s.Datum == schicht.Datum);

        if (excludeSchichtId.HasValue)
        {
            overlapQuery = overlapQuery.Where(s => s.Id != excludeSchichtId.Value);
        }

        var sameDayShifts = await overlapQuery.ToListAsync();

        var hasOverlap = sameDayShifts.Any(existing =>
            schicht.Beginn < existing.Ende && schicht.Ende > existing.Beginn);

        if (hasOverlap)
        {
            return new SchichtValidationResult
            {
                Success = false,
                Message = "Der Mitarbeiter hat bereits eine überschneidende Schicht an diesem Tag."
            };
        }

        var monatsstunden = await GetMonatsstundenAsync(
            schicht.MitarbeiterId,
            schicht.Datum.Year,
            schicht.Datum.Month,
            excludeSchichtId);

        var neueGesamtstunden = monatsstunden + schicht.Stunden;

        if (neueGesamtstunden > mitarbeiter.MaxStundenProMonat)
        {
            var message =
                $"Maximale Monatsarbeitszeit überschritten. Geplant: {neueGesamtstunden:F2} h / Erlaubt: {mitarbeiter.MaxStundenProMonat:F2} h";

            if (!allowMaxHoursOverride)
            {
                return new SchichtValidationResult
                {
                    Success = false,
                    Message = message,
                    RequiresConfirmation = true
                };
            }
        }

        return new SchichtValidationResult
        {
            Success = true,
            Message = "OK"
        };
    }

    public async Task<decimal> GetMonatsstundenAsync(
        int mitarbeiterId,
        int jahr,
        int monat,
        int? excludeSchichtId = null)
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