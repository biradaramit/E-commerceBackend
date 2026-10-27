using DAL.Identity;
using Microsoft.AspNetCore.Identity;

namespace DAL.Seed;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Admin", "Customer" };

        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@ecommerce.com";
        const string adminPassword = "Admin@12345";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                FirstName = "System",
                LastName = "Admin",
                Email = adminEmail,
                UserName = adminEmail,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(
                adminUser,
                adminPassword);

            if (!result.Succeeded)
            {
                string errors = string.Join(
                    ", ",
                    result.Errors.Select(error => error.Description));

                throw new Exception(
                    $"Admin creation failed: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(
                adminUser,
                "Admin");

            if (!roleResult.Succeeded)
            {
                string errors = string.Join(
                    ", ",
                    roleResult.Errors.Select(error => error.Description));

                throw new Exception(
                    $"Admin role assignment failed: {errors}");
            }
        }
    }
}