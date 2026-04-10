using Microsoft.AspNetCore.Identity;
using Schichtplaner.Models;

namespace Schichtplaner.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        await SeedData.EnsureRolesAsync(roleManager);
        await SeedData.EnsureAdminAsync(userManager, configuration);
    }
}
