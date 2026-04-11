using Schichtplaner.Models;

namespace Schichtplaner.Services;

public interface ISchichtService
{
    Task<SchichtValidationResult> ValidateSchichtAsync(
        Schicht schicht,
        int? excludeSchichtId = null,
        bool allowMaxHoursOverride = false);

    Task<decimal> GetMonatsstundenAsync(
        int mitarbeiterId,
        int jahr,
        int monat,
        int? excludeSchichtId = null);
}

public class SchichtValidationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "OK";
    public bool RequiresConfirmation { get; set; }
}
