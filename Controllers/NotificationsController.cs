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
    [Authorize]
    public class NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
    {
        public async Task<IActionResult> Index(string? filter)
        {
            var userId = userManager.GetUserId(User)!;
            var mine = db.Notifications.Where(n => n.UserId == userId);
            var todayStart = DateTime.Today.ToUniversalTime();

            filter = filter is "unread" or "read" or "today" ? filter : "all";

            var query = filter switch
            {
                "unread" => mine.Where(n => !n.IsRead),
                "read" => mine.Where(n => n.IsRead),
                "today" => mine.Where(n => n.CreatedAt >= todayStart),
                _ => mine
            };

            var model = new NotificationListViewModel
            {
                Filter = filter,
                All = await mine.CountAsync(),
                Unread = await mine.CountAsync(n => !n.IsRead),
                Read = await mine.CountAsync(n => n.IsRead),
                Today = await mine.CountAsync(n => n.CreatedAt >= todayStart),
                Items = await query.OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync()
            };

            return View(model);
        }

        // Opening a notification marks it as read and takes the user to the relevant page for their role.
        public async Task<IActionResult> Open(int id)
        {
            var notification = await FindOwnAsync(id);
            if (notification is null) return NotFound();

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await db.SaveChangesAsync();
            }

            return LocalRedirect(Destination(notification.Type) ?? "/");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, string? filter)
        {
            var notification = await FindOwnAsync(id);
            if (notification is null) return NotFound();

            notification.IsRead = !notification.IsRead;
            await db.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { filter });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead(string? filter)
        {
            var userId = userManager.GetUserId(User)!;

            await db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            return RedirectToAction(nameof(Index), new { filter });
        }

        private Task<Notification?> FindOwnAsync(int id)
        {
            var userId = userManager.GetUserId(User)!;
            return db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        }

        private string? Destination(NotificationType type)
        {
            var isHost = User.IsInRole(Roles.Host);
            var isStaff = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);

            return type switch
            {
                NotificationType.HostApplication => isStaff
                    ? Url.Action("Index", "HostRequests", new { area = "Admin" })
                    : isHost
                        ? Url.Action("Index", "Properties", new { area = "Host" })
                        : Url.Action("Status", "HostApplication", new { area = "" }),

                NotificationType.Booking => isHost
                    ? Url.Action("Index", "BookingRequests", new { area = "Host" })
                    : Url.Action("Index", "Bookings", new { area = "" }),

                NotificationType.CheckIn => isHost
                    ? Url.Action("Index", "CheckIns", new { area = "Host" })
                    : Url.Action("Index", "Bookings", new { area = "" }),

                NotificationType.Message => Url.Action("Index", "Messages", new { area = "" }),

                _ => Url.Action("Index", "Notifications", new { area = "" })
            };
        }
    }
}
