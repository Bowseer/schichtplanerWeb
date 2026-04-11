namespace Schichtplaner.Models.ViewModels;

public class MonatsplanViewModel
{
    public int Jahr { get; set; }
    public int Monat { get; set; }

    public int? StandortId { get; set; }
    public List<StandortDto> Standorte { get; set; } = new();

    public List<MitarbeiterSidebarDto> Mitarbeiter { get; set; } = new();
    public List<StandardSlotZeitDto> StandardSlotZeiten { get; set; } = new();

    public List<KalenderWocheDto> Wochen { get; set; } = new();
}

public class StandortDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MitarbeiterSidebarDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Farbe { get; set; } = string.Empty;
    public decimal Reststunden { get; set; }
}

public class StandardSlotZeitDto
{
    public int Slot { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public string Beginn { get; set; } = string.Empty;
    public string Ende { get; set; } = string.Empty;
}

public class KalenderWocheDto
{
    public int KalenderWoche { get; set; }
    public List<KalenderTagDto> Tage { get; set; } = new();
}

public class KalenderTagDto
{
    public DateOnly Datum { get; set; }
    public bool IstSonntag { get; set; }
    public bool IstFeiertag { get; set; }
    public string? FeiertagName { get; set; }

    public List<KalenderSlotDto> Slots { get; set; } = new();
}

public class KalenderSlotDto
{
    public int Slot { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public string Beginn { get; set; } = string.Empty;
    public string Ende { get; set; } = string.Empty;
    public int? MitarbeiterId { get; set; }
    public string? MitarbeiterName { get; set; }
    public string? Farbe { get; set; }
}