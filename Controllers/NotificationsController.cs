using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Controllers
{
    // Signed in users only
    [Authorize]
    // Notifications controller
    public class NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
    {
        // Define read method, filtered by all, unread, read or today
        public async Task<IActionResult> Index(string? filter)
        {
            // Get the user's notifications and the start of today
            var userId = userManager.GetUserId(User)!;
            var mine = db.Notifications.Where(n => n.UserId == userId);
            var todayStart = DateTime.Today.ToUniversalTime();

            // Fall back to all if the filter is not recognised
            filter = filter is "unread" or "read" or "today" ? filter : "all";

            // Apply the filter
            var query = filter switch
            {
                "unread" => mine.Where(n => !n.IsRead),
                "read" => mine.Where(n => n.IsRead),
                "today" => mine.Where(n => n.CreatedAt >= todayStart),
                _ => mine
            };

            // Build the model with the counts per filter and the latest 100 notifications
            var model = new NotificationListViewModel
            {
                Filter = filter,
                All = await mine.CountAsync(),
                Unread = await mine.CountAsync(n => !n.IsRead),
                Read = await mine.CountAsync(n => n.IsRead),
                Today = await mine.CountAsync(n => n.CreatedAt >= todayStart),
                Items = await query.OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync()
            };

            // Return the model with the view
            return View(model);
        }

        // Define open method, marks the notification as read and redirects to the relevant page for the user's role
        public async Task<IActionResult> Open(int id)
        {
            // Get the user's notification, if it does not exist, return NotFound
            var notification = await FindOwnAsync(id);
            if (notification is null) return NotFound();

            // Mark it as read and save db changes
            if (!notification.IsRead) { notification.IsRead = true; await db.SaveChangesAsync(); }

            // Redirect to the destination for the notification type
            return LocalRedirect(Destination(notification.Type) ?? "/");
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define toggle read method
        public async Task<IActionResult> Toggle(int id, string? filter)
        {
            // Get the user's notification, if it does not exist, return NotFound
            var notification = await FindOwnAsync(id);
            if (notification is null) return NotFound();

            // Flip the read status and save db changes
            notification.IsRead = !notification.IsRead;
            await db.SaveChangesAsync();

            // Redirect to index keeping the filter
            return RedirectToAction(nameof(Index), new { filter });
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define mark all read method
        public async Task<IActionResult> MarkAllRead(string? filter)
        {
            // Get user id
            var userId = userManager.GetUserId(User)!;

            // Mark all of the user's unread notifications as read
            await db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            // Redirect to index keeping the filter
            return RedirectToAction(nameof(Index), new { filter });
        }

        // Define find own method, only the user's own notifications can be found
        private Task<Notification?> FindOwnAsync(int id)
        {
            // Get user id
            var userId = userManager.GetUserId(User)!;

            // Get the notification by its id for the user
            return db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        }

        // Define destination method, the page to open for a notification type depends on the user's role
        private string? Destination(NotificationType type)
        {
            // Get the user's roles
            var isHost = User.IsInRole(Roles.Host);
            var isStaff = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);

            // Pick the destination by notification type
            return type switch
            {
                NotificationType.HostApplication => isStaff ? Url.Action("Index", "HostRequests", new { area = "Admin" }) : isHost ? Url.Action("Index", "Properties", new { area = "Host" }) : Url.Action("Status", "HostApplication", new { area = "" }),
                NotificationType.Booking => isHost ? Url.Action("Index", "BookingRequests", new { area = "Host" }) : Url.Action("Index", "Bookings", new { area = "" }),
                NotificationType.CheckIn => isHost ? Url.Action("Index", "CheckIns", new { area = "Host" }) : Url.Action("Index", "Bookings", new { area = "" }),
                NotificationType.Message => Url.Action("Index", "Messages", new { area = "" }),
                _ => Url.Action("Index", "Notifications", new { area = "" })
            };
        }
    }
}
