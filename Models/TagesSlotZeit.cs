using System.ComponentModel.DataAnnotations;

namespace Schichtplaner.Models;

public class TagesSlotZeit
{
    public int Id { get; set; }

    [Required]
    public int StandortId { get; set; }

    public Standort? Standort { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly Datum { get; set; }

    [Range(1, 3)]
    public int Slot { get; set; }

    [Required]
    public TimeSpan Beginn { get; set; }

    [Required]
    public TimeSpan Ende { get; set; }
}
