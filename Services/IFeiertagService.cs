using Schichtplaner.Models;

namespace Schichtplaner.Services;

public interface IFeiertagService
{
    Dictionary<DateOnly, string> GetFeiertage(int year, Bundesland bundesland);
}
