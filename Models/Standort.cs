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

    public TimeSpan FruehBeginn { get; set; } = new(8, 0, 0);
    public TimeSpan FruehEnde { get; set; } = new(12, 0, 0);

    public TimeSpan TagBeginn { get; set; } = new(12, 0, 0);
    public TimeSpan TagEnde { get; set; } = new(16, 0, 0);

    public TimeSpan SpaetBeginn { get; set; } = new(16, 0, 0);
    public TimeSpan SpaetEnde { get; set; } = new(20, 0, 0);

    public ICollection<Mitarbeiter> Mitarbeiter { get; set; } = new List<Mitarbeiter>();
    public ICollection<Schicht> Schichten { get; set; } = new List<Schicht>();
    public ICollection<TagesSlotZeit> TagesSlotZeiten { get; set; } = new List<TagesSlotZeit>();
}