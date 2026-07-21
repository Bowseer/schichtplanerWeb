namespace Schichtplaner.Models;

public enum Bundesland
{
    [System.ComponentModel.DataAnnotations.Display(Name = "Baden-Württemberg")]
    BadenWuerttemberg = 1,
    Bayern = 2,
    Berlin = 3,
    Brandenburg = 4,
    Bremen = 5,
    Hamburg = 6,
    Hessen = 7,
    [System.ComponentModel.DataAnnotations.Display(Name = "Mecklenburg-Vorpommern")]
    MecklenburgVorpommern = 8,
    Niedersachsen = 9,
    [System.ComponentModel.DataAnnotations.Display(Name = "Nordrhein-Westfalen")]
    NordrheinWestfalen = 10,
    [System.ComponentModel.DataAnnotations.Display(Name = "Rheinland-Pfalz")]
    RheinlandPfalz = 11,
    Saarland = 12,
    Sachsen = 13,
    [System.ComponentModel.DataAnnotations.Display(Name = "Sachsen-Anhalt")]
    SachsenAnhalt = 14,
    [System.ComponentModel.DataAnnotations.Display(Name = "Schleswig-Holstein")]
    SchleswigHolstein = 15,
    [System.ComponentModel.DataAnnotations.Display(Name = "Thüringen")]
    Thueringen = 16
}
