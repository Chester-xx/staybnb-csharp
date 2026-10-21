using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Models;

namespace staybnb_csharp.Data
{
    public static class RoleSeeder
    {
        // Super admin seed
        private const string SuperAdminEmail = "superadmin@staybnb.com";
        private const string SuperAdminPassword = "SuperAdmin123!";

        // Seed roles and super admin user
        public static async Task SeedAsync(IServiceProvider services)
        {
            // Get db context
            var db = services.GetRequiredService<ApplicationDbContext>();
            
            // Migrate db on new compile
            await db.Database.MigrateAsync();

            // Get role and user managers
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Create roles if they don't exist
            foreach (var role in new[] { Roles.Guest, Roles.Host, Roles.Admin, Roles.SuperAdmin }) { if (!await roleManager.RoleExistsAsync(role)) { await roleManager.CreateAsync(new IdentityRole(role)); } }

            // Create super admin user if it doesn't exist
            if (await userManager.FindByEmailAsync(SuperAdminEmail) is null)
            {
                var superAdmin = new ApplicationUser
                {
                    UserName = SuperAdminEmail,
                    Email = SuperAdminEmail,
                    EmailConfirmed = true,
                    FirstName = "Super",
                    LastName = "Admin"
                };

                // Create and check success, add role
                var result = await userManager.CreateAsync(superAdmin, SuperAdminPassword);
                if (result.Succeeded) { await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin); }
            }
        }
    }
}
