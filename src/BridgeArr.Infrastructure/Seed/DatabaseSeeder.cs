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
    /// <summary>The built-in admin role name used for authorization policies.</summary>
    public const string AdminRole = "Admin";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await SeedRolesAsync(roleManager, logger);
            await SeedAdminUserAsync(userManager, logger);

            var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            await EnvironmentIntegrationSeeder.SeedAsync(db, configuration, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database.");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(AdminRole));
            if (result.Succeeded)
            {
                logger.LogInformation("Created role {Role}", AdminRole);
            }
            else
            {
                logger.LogError(
                    "Failed to create role {Role}: {Errors}",
                    AdminRole,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string adminEmail = "admin@bridgearr.local";
        const string adminUsername = "admin";

        var existing = await userManager.FindByNameAsync(adminUsername);
        if (existing is not null)
        {
            // Ensure existing admin is in the Admin role
            if (!await userManager.IsInRoleAsync(existing, AdminRole))
            {
                await userManager.AddToRoleAsync(existing, AdminRole);
            }
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

        await userManager.AddToRoleAsync(admin, AdminRole);

        logger.LogInformation(
            "Admin user created. Username: {Username}, Password: admin (change on first login)",
            adminUsername);
    }
}
