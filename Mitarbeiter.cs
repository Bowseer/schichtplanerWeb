using Microsoft.AspNetCore.Identity;

namespace Schichtplaner.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
