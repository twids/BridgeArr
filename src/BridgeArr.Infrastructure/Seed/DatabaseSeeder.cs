using BridgeArr.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Infrastructure.Seed;

/// <summary>
/// Seeds the database with initial data.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await SeedAdminUserAsync(userManager, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database.");
            throw;
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string adminEmail = "admin@bridgearr.local";
        const string adminUsername = "admin";

        if (await userManager.FindByNameAsync(adminUsername) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminUsername,
            Email = adminEmail,
            EmailConfirmed = true,
            MustChangePassword = true,
            DisplayName = "Administrator"
        };

        var result = await userManager.CreateAsync(admin, "admin");
        if (!result.Succeeded)
        {
            logger.LogError(
                "Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogInformation(
            "Admin user created. Username: {Username}, Password: admin (change on first login)",
            adminUsername);
    }
}
