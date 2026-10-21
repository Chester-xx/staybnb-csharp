using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.Services;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Areas.Admin.Controllers
{

    // Admin access only
    [Area("Admin")]
    [Authorize(Roles = Roles.SuperAdmin)]
    
    // Users controller
    public class UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IActivityLogService activityLog) : Controller
    {

        // Define read
        public async Task<IActionResult> Index(string? search)
        {
            // Get model of users by search query
            var model = new UserManagementViewModel
            {
                Search = search,
                Guests = await GetUsersAsync(Roles.Guest, search),
                Hosts = await GetUsersAsync(Roles.Host, search),
                Admins = await GetUsersAsync(Roles.Admin, search)
            };

            // Return model with view
            return View(model);
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define User promotion method
        public async Task<IActionResult> Promote(string id)
        {
            // Get current user object
            var user = await userManager.FindByIdAsync(id);
            
            // Return not found if user doesnt exist
            if (user is null) return NotFound();

            // Check if user is not a guest or if they have a pending host application - return if so
            if (!await userManager.IsInRoleAsync(user, Roles.Guest))
            {
                TempData["Error"] = "Only Guests can be promoted to Admin.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user has a pending host application, user cannot be a host and admin at the same time
            if (await db.HostApplications.AnyAsync(a => a.ApplicationUserId == id && a.Status == ApplicationStatus.Pending))
            {
                TempData["Error"] = $"{user.FullName} has a pending host application. Review it before promoting this user.";
                return RedirectToAction(nameof(Index));
            }

            // Get the super admin user object
            var superAdmin = (await userManager.GetUserAsync(User))!;

            // Attempt to swap guest to admin, if it fails return error message
            if (!await SwapRoleAsync(user, from: Roles.Guest, to: Roles.Admin))
            {
                TempData["Error"] = "The promotion failed. No changes were made.";
                return RedirectToAction(nameof(Index));
            }

            // Log the role change activity
            await activityLog.LogAsync(superAdmin.Id, ActivityType.RoleChange, $"Role changed Guest to Admin: {user.Email}");

            // Set success message and redirect to index
            TempData["Success"] = $"{user.FullName} is now an Admin.";
            return RedirectToAction(nameof(Index));
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define User demotion method
        public async Task<IActionResult> MakeGuest(string id)
        {
            // Get current user object
            var user = await userManager.FindByIdAsync(id);
            
            // Return not found if user doesnt exist
            if (user is null) return NotFound();

            // Check if user is a host or admin, if not return error message
            var wasHost = await userManager.IsInRoleAsync(user, Roles.Host);
            var wasAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);

            // If user is a superadmin or already a guest, return error message
            if (!wasHost && !wasAdmin)
            {
                TempData["Error"] = "Only Hosts and Admins can be changed back to a Guest.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user has active bookings, if so return error message
            if (wasHost && await db.Bookings.AnyAsync(b => b.Property.HostId == id && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved || b.Status == BookingStatus.CheckedIn)))
            {
                TempData["Error"] = $"{user.FullName} has active bookings. Resolve them before changing this user back to a Guest.";
                return RedirectToAction(nameof(Index));
            }

            // Get the super admin user object
            var superAdmin = (await userManager.GetUserAsync(User))!;
            
            // Determine the role to remove based on whether the user was a host or admin
            var fromRole = wasHost ? Roles.Host : Roles.Admin;

            // start a new transaction for atomicity
            await using var transaction = await db.Database.BeginTransactionAsync();

            // Add the user to the Guest role and remove them from their previous role
            var added = await userManager.AddToRoleAsync(user, Roles.Guest);
            var removed = added.Succeeded ? await userManager.RemoveFromRoleAsync(user, fromRole) : added;

            // If the removal failed, roll back the transaction and return an error message
            if (!removed.Succeeded)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "The role change failed. No changes were made.";
                return RedirectToAction(nameof(Index));
            }

            // If the user was a host, deactivate their listings
            if (wasHost)
            {
                // Take the former host's listings off the marketplace
                var properties = await db.HostProperties.Where(p => p.HostId == id).ToListAsync();
                properties.ForEach(p => p.IsActive = false);
                
                // Save db changes
                await db.SaveChangesAsync();
            }

            // Commit transaction
            await transaction.CommitAsync();

            // Log the role change activity
            await activityLog.LogAsync(superAdmin.Id, ActivityType.RoleChange, $"Role changed {fromRole} to Guest: {user.Email}");

            // Set success message and redirect to index
            TempData["Success"] = wasHost ? $"{user.FullName} is now a Guest and their listings were deactivated." : $"{user.FullName} is now a Guest.";
            return RedirectToAction(nameof(Index));
        }

        // Helper method to get users by role and opt search
        private async Task<List<ApplicationUser>> GetUsersAsync(string role, string? search)
        {
            // Get users in the specified role
            var users = (await userManager.GetUsersInRoleAsync(role)).AsEnumerable();

            // If a search term is provided, filter users by email or full name
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                users = users.Where(u => (u.Email ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) || u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            // Return the filtered and sorted list of users
            return users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
        }

        // Helper method to swap roles for a user within a transaction
        private async Task<bool> SwapRoleAsync(ApplicationUser user, string from, string to)
        {
            // Start a new transaction for atomicity
            await using var transaction = await db.Database.BeginTransactionAsync();

            // Add the user to the new role
            var added = await userManager.AddToRoleAsync(user, to);
            
            // If adding to the new role succeeded, remove the user from the old role; otherwise, keep the result of the add operation
            var removed = added.Succeeded ? await userManager.RemoveFromRoleAsync(user, from) : added;

            // If the removal failed, roll back the transaction and return false
            if (!removed.Succeeded)
            {
                await transaction.RollbackAsync();
                return false;
            }

            // Commit the transaction and return true
            await transaction.CommitAsync();
            return true;
        }
    }
}
