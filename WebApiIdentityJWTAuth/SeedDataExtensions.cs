using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class SeedDataExtensions
{
    public static async Task SeedUsersAndRolesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate(); //skapar databasen och kör ev migreringar

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        if (await userManager.FindByNameAsync("user") == null)
        {
            var user = new IdentityUser { UserName = "user", Email = "user@example.com" };
            await userManager.CreateAsync(user, "Password123!");
        }

        if (await userManager.FindByNameAsync("admin") == null)
        {
            var admin = new IdentityUser { UserName = "admin", Email = "admin@example.com" };
            await userManager.CreateAsync(admin, "Password123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
