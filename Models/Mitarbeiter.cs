using System.ComponentModel.DataAnnotations;

namespace Schichtplaner.Models;

public class Mitarbeiter
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Vorname { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Nachname { get; set; } = string.Empty;

    [Range(0, 400)]
    public decimal MaxStundenProMonat { get; set; }

    public EmploymentType EmploymentType { get; set; }

    public bool NurSamstag { get; set; }
    public bool Aktiv { get; set; } = true;

    [Required]
    public int StandortId { get; set; }
    public Standort? Standort { get; set; }

    public ICollection<Schicht> Schichten { get; set; } = new List<Schicht>();

    public string VollerName => $"{Vorname} {Nachname}";
}
