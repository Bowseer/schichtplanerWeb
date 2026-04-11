using Microsoft.AspNetCore.Identity;

namespace Schichtplaner.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public bool MustChangePassword { get; set; } = false;

    public int? DefaultStandortId { get; set; }
    public Standort? DefaultStandort { get; set; }

    public bool IsDisabled { get; set; } = false;
}
