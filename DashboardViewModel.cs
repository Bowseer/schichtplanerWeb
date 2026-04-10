using Microsoft.AspNetCore.Identity;
using Schichtplaner.Models;

namespace Schichtplaner.Data;

public static class SeedData
{
    public static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { "Admin", "Planer" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task EnsureAdminAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var email = configuration["SeedAdmin:Email"] ?? "admin@example.local";
        var password = configuration["SeedAdmin:Password"] ?? "Admin1234";

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            if (!await userManager.IsInRoleAsync(existingUser, "Admin"))
            {
                await userManager.AddToRoleAsync(existingUser, "Admin");
            }
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Administrator"
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Admin user could not be created: {errors}");
        }

        await userManager.AddToRoleAsync(admin, "Admin");
    }
}
