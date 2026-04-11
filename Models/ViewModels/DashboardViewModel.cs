namespace Schichtplaner.Models.ViewModels;

public class DashboardViewModel
{
    public int AnzahlStandorte { get; set; }
    public int AnzahlMitarbeiter { get; set; }
    public int AnzahlSchichtenDiesenMonat { get; set; }
    public List<string> Warnungen { get; set; } = new();
}
