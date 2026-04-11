using Schichtplaner.Models;

namespace Schichtplaner.Services;

public class FeiertagService : IFeiertagService
{
    public Dictionary<DateOnly, string> GetFeiertage(int year, Bundesland bundesland)
    {
        var feiertage = new Dictionary<DateOnly, string>();
        var easterSunday = GetEasterSunday(year);

        Add(feiertage, new DateOnly(year, 1, 1), "Neujahr");
        Add(feiertage, easterSunday.AddDays(-2), "Karfreitag");
        Add(feiertage, easterSunday.AddDays(1), "Ostermontag");
        Add(feiertage, new DateOnly(year, 5, 1), "Tag der Arbeit");
        Add(feiertage, easterSunday.AddDays(39), "Christi Himmelfahrt");
        Add(feiertage, easterSunday.AddDays(50), "Pfingstmontag");
        Add(feiertage, new DateOnly(year, 10, 3), "Tag der Deutschen Einheit");
        Add(feiertage, new DateOnly(year, 12, 25), "1. Weihnachtstag");
        Add(feiertage, new DateOnly(year, 12, 26), "2. Weihnachtstag");

        if (bundesland is Bundesland.BadenWuerttemberg or Bundesland.Bayern or Bundesland.SachsenAnhalt)
        {
            Add(feiertage, new DateOnly(year, 1, 6), "Heilige Drei Könige");
        }

        if (bundesland is Bundesland.BadenWuerttemberg or Bundesland.Bayern or Bundesland.Hessen
            or Bundesland.NordrheinWestfalen or Bundesland.RheinlandPfalz or Bundesland.Saarland)
        {
            Add(feiertage, easterSunday.AddDays(60), "Fronleichnam");
        }

        if (bundesland is Bundesland.BadenWuerttemberg or Bundesland.Bayern or Bundesland.NordrheinWestfalen
            or Bundesland.RheinlandPfalz or Bundesland.Saarland)
        {
            Add(feiertage, new DateOnly(year, 11, 1), "Allerheiligen");
        }

        if (bundesland is Bundesland.Brandenburg or Bundesland.Bremen or Bundesland.Hamburg
            or Bundesland.MecklenburgVorpommern or Bundesland.Niedersachsen or Bundesland.Sachsen
            or Bundesland.SachsenAnhalt or Bundesland.SchleswigHolstein or Bundesland.Thueringen)
        {
            Add(feiertage, new DateOnly(year, 10, 31), "Reformationstag");
        }

        if (bundesland == Bundesland.Berlin)
        {
            Add(feiertage, new DateOnly(year, 3, 8), "Internationaler Frauentag");
        }

        if (bundesland == Bundesland.Sachsen)
        {
            Add(feiertage, GetBussUndBettag(year), "Buß- und Bettag");
        }

        if (bundesland == Bundesland.Thueringen)
        {
            Add(feiertage, new DateOnly(year, 9, 20), "Weltkindertag");
        }

        return feiertage;
    }

    private static void Add(Dictionary<DateOnly, string> feiertage, DateOnly date, string name)
    {
        feiertage[date] = name;
    }

    private static DateOnly GetEasterSunday(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;

        return new DateOnly(year, month, day);
    }

    private static DateOnly GetBussUndBettag(int year)
    {
        var christmas = new DateOnly(year, 12, 25);
        var weekday = (int)christmas.DayOfWeek;
        var sundayBased = weekday == 0 ? 7 : weekday;
        var firstAdvent = christmas.AddDays(-(sundayBased + 21));
        return firstAdvent.AddDays(-11);
    }
}