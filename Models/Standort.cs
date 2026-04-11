using System.ComponentModel.DataAnnotations;

namespace Schichtplaner.Models;

public class Standort
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Adresse { get; set; }

    public Bundesland Bundesland { get; set; } = Bundesland.Berlin;

    public ICollection<Mitarbeiter> Mitarbeiter { get; set; } = new List<Mitarbeiter>();
    public ICollection<Schicht> Schichten { get; set; } = new List<Schicht>();
    public ICollection<TagesSlotZeit> TagesSlotZeiten { get; set; } = new List<TagesSlotZeit>();
    public ICollection<StandortSlotZeit> StandardSlotZeiten { get; set; } = new List<StandortSlotZeit>();
}
