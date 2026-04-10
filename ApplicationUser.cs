using Schichtplaner.Models;

namespace Schichtplaner.Models.ViewModels;

public class MonatsplanViewModel
{
    public int Jahr { get; set; }
    public int Monat { get; set; }
    public List<MitarbeiterMonatsplanRow> MitarbeiterRows { get; set; } = new();
}

public class MitarbeiterMonatsplanRow
{
    public int MitarbeiterId { get; set; }
    public string MitarbeiterName { get; set; } = string.Empty;
    public string StandortName { get; set; } = string.Empty;
    public decimal MaxStunden { get; set; }
    public decimal GeplanteStunden { get; set; }
    public bool Ueberschritten { get; set; }
    public List<Schicht> Schichten { get; set; } = new();
}
