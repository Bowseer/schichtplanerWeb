using System.ComponentModel.DataAnnotations;

namespace Schichtplaner.Models;

public class StandortSlotZeit
{
    public int Id { get; set; }

    [Required]
    public int StandortId { get; set; }

    public Standort? Standort { get; set; }

    [Range(1, 7)]
    public int Wochentag { get; set; } // 1 = Montag, 7 = Sonntag

    [Range(1, 3)]
    public int Slot { get; set; } // 1 = Früh, 2 = Flex, 3 = Spät

    public bool Aktiv { get; set; } = true;

    [Required]
    public TimeSpan Beginn { get; set; }

    [Required]
    public TimeSpan Ende { get; set; }
}
