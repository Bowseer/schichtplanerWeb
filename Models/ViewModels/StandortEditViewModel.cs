using System.ComponentModel.DataAnnotations;

namespace Schichtplaner.Models.ViewModels;

public class StandortEditViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Adresse { get; set; }

    public Bundesland Bundesland { get; set; } = Bundesland.Berlin;

    public List<StandortSlotZeitEditItem> SlotZeiten { get; set; } = new();
}

public class StandortSlotZeitEditItem
{
    public int Wochentag { get; set; }
    public int Slot { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public string WochentagName { get; set; } = string.Empty;

    public bool Aktiv { get; set; } = true;

    [Required]
    public string Beginn { get; set; } = "08:00";

    [Required]
    public string Ende { get; set; } = "12:00";
}
