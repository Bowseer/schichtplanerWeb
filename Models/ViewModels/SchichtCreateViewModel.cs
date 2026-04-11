using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Schichtplaner.Models.ViewModels;

public class SchichtCreateViewModel
{
    [Required]
    public int MitarbeiterId { get; set; }

    [Required]
    public int StandortId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly Datum { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public TimeSpan Beginn { get; set; }

    [Required]
    public TimeSpan Ende { get; set; }

    [Range(0, 600)]
    public int PauseMinuten { get; set; }

    public List<SelectListItem> MitarbeiterListe { get; set; } = new();
    public List<SelectListItem> StandortListe { get; set; } = new();
}
