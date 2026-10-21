using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Areas.Admin.Controllers
{

    // Admin access only
    [Area("Admin")]
    [Authorize(Roles = Roles.AdminOrSuperAdmin)]
    
    // Dashboard controller
    public class DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
    {
        // define read
        public async Task<IActionResult> Index()
        {
            // Get total users, pending applications, active properties, total bookings, pending bookings, approved hosts, and recent activity
            var model = new AdminDashboardViewModel
            {
                // get lists
                TotalUsers = await userManager.Users.CountAsync(),
                PendingApplications = await db.HostApplications.CountAsync(a => a.Status == ApplicationStatus.Pending),
                ActiveProperties = await db.HostProperties.CountAsync(p => p.IsActive),
                TotalBookings = await db.Bookings.CountAsync(),
                PendingBookings = await db.Bookings.CountAsync(b => b.Status == BookingStatus.Pending),
                ApprovedHosts = (await userManager.GetUsersInRoleAsync(Roles.Host)).Count,
                
                // get recent activity, including the user, order desc, lim 8
                RecentActivity = await db.ActivityLogs
                    .Include(l => l.User)
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(8)
                    .ToListAsync(),

                // get recent applications, including property and user, order desc, lim 5
                RecentApplications = await db.HostApplications
                    .Include(a => a.ApplicationUser)
                    .Include(a => a.Property)
                    .OrderByDescending(a => a.AppliedAt)
                    .Take(5)
                    .ToListAsync()
            };

            // Return view with model
            return View(model);
        }
    }
}
