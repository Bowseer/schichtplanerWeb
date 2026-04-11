using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Schichtplaner.Models;

public class Schicht
{
    public int Id { get; set; }

    [Required]
    public int MitarbeiterId { get; set; }
    public Mitarbeiter? Mitarbeiter { get; set; }

    [Required]
    public int StandortId { get; set; }
    public Standort? Standort { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly Datum { get; set; }

    [Required]
    public TimeSpan Beginn { get; set; }

    [Required]
    public TimeSpan Ende { get; set; }

    [Range(0, 600)]
    public int PauseMinuten { get; set; }

    public int Slot { get; set; } // 1-3

    [NotMapped]
    public decimal Stunden
    {
        get
        {
            var diff = Ende - Beginn;
            var stunden = (decimal)diff.TotalHours - (PauseMinuten / 60m);
            return stunden < 0 ? 0 : stunden;
        }
    }
}