using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.Services;

namespace staybnb_csharp.Areas.Admin.Controllers
{

    // Admin access only
    [Area("Admin")]
    [Authorize(Roles = Roles.AdminOrSuperAdmin)]
    
    // Host requests controller
    public class HostRequestsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, INotificationService notifications, IActivityLogService activityLog) : Controller
    {
        // Define read
        public async Task<IActionResult> Index(ApplicationStatus? status)
        {
            // Create model query, including user and property
            var query = db.HostApplications
                .Include(a => a.ApplicationUser)
                .Include(a => a.Property)
                .AsQueryable();

            // Filter by status if provided
            if (status is not null) { query = query.Where(a => a.Status == status); }

            // Get applications, order by status, desc
            var applications = await query
                .OrderBy(a => a.Status == ApplicationStatus.Pending ? 0 : 1)
                .ThenByDescending(a => a.AppliedAt)
                .ToListAsync();

            // Set filter
            ViewData["Filter"] = status;

            // Return view with model
            return View(applications);
        }

        // Define specific read
        public async Task<IActionResult> Details(int id)
        {
            // Get application by id
            var application = await LoadApplicationAsync(id);
            
            // Return model with view or return not found header
            return application is null ? NotFound() : View(application);
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // 
        public async Task<IActionResult> Approve(int id)
        {
            // Get specific application by id
            var application = await LoadApplicationAsync(id);
            
            // If it doesnt exist, return not found
            if (application is null) return NotFound();

            // If the applications status is NOT pending, redirect to not process again
            if (application.Status != ApplicationStatus.Pending)
            {
                TempData["Error"] = "This application has already been reviewed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Get the current logged in admin and the associated applicant, init applicant role tracker flag
            var admin = (await userManager.GetUserAsync(User))!;
            var applicant = application.ApplicationUser;
            var swappedRole = false;

            // Declare atomic transaction
            await using var transaction = await db.Database.BeginTransactionAsync();

            // Set application status to approved, clear rejection
            application.Status = ApplicationStatus.Approved;
            application.RejectionReason = null;
            
            // NB! the property is listed later, once the host has completed the required checkin setup
            // Save db changes
            await db.SaveChangesAsync();

            // If applicant is a guest
            if (await userManager.IsInRoleAsync(applicant, Roles.Guest))
            {
                // Attempt to add applicant to host role
                var added = await userManager.AddToRoleAsync(applicant, Roles.Host);

                // Attempt to remove applicant from guest role
                var removed = added.Succeeded ? await userManager.RemoveFromRoleAsync(applicant, Roles.Guest) : added;

                // If role change failed, rollback transaction
                if (!removed.Succeeded)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "The role change failed, so the application was not approved.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Update role swap tracker
                swappedRole = true;
            }

            // Commit to the transaction
            await transaction.CommitAsync();

            // Log the updated role activity
            await activityLog.LogAsync(admin.Id, ActivityType.HostApplication, $"Approved application #{application.Id}: {application.Property.Title}");
            
            // If role swap occurred, log the role change activity as well
            if (swappedRole) { await activityLog.LogAsync(admin.Id, ActivityType.RoleChange, $"Role changed Guest to Host: {applicant.Email}"); }

            // Create a new notification for the applicant
            await notifications.CreateAsync(applicant.Id, $"Host application approved: {application.Property.Title}", NotificationType.HostApplication);

            // Redirect to index, parsing application approved (new host) OR (new property added)
            TempData["Success"] = swappedRole ? $"Application approved. {applicant.FullName} is now a Host." : "Application approved.";
            return RedirectToAction(nameof(Index));
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Reject an application
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            // Get application by id
            var application = await LoadApplicationAsync(id);
            
            // Return not found if application is null
            if (application is null) return NotFound();

            // Redirect back to requests if the application has already been reviewed
            if (application.Status != ApplicationStatus.Pending)
            {
                TempData["Error"] = "This application has already been reviewed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check if reason is provided and not empty and redirect if no reason is provided
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Please give a reason so the applicant knows why they were rejected.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Get the current logged in admin user
            var admin = (await userManager.GetUserAsync(User))!;

            // Update application status, rejection reason, and active status
            application.Status = ApplicationStatus.Rejected;
            application.RejectionReason = reason.Trim().Length > 500 ? reason.Trim()[..500] : reason.Trim();
            application.Property.IsActive = false;

            // Save db changes
            await db.SaveChangesAsync();

            // Log rejection, plus create new rejection notification for applicant
            await activityLog.LogAsync(admin.Id, ActivityType.HostApplication, $"Rejected application #{application.Id}: {application.Property.Title}");
            await notifications.CreateAsync(application.ApplicationUserId, $"Host application rejected: {application.Property.Title}", NotificationType.HostApplication);

            // Redirect back to applications page, notify admin
            TempData["Success"] = "Application rejected and the applicant has been notified.";
            return RedirectToAction(nameof(Index));
        }

        // Gets the application by id, including the applicant and property
        private Task<HostApplication?> LoadApplicationAsync(int id) =>
            db.HostApplications
                .Include(a => a.ApplicationUser)
                .Include(a => a.Property).ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(a => a.Id == id);
    }
}
