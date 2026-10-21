using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.Services;

namespace staybnb_csharp.Areas.Host.Controllers
{
    // Hosts only
    [Area("Host")]
    [Authorize(Roles = Roles.Host)]
    
    // Booking requests controller
    public class BookingRequestsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, INotificationService notifications, IActivityLogService activityLog) : Controller
    {
        // Define read
        public async Task<IActionResult> Index()
        {
            // Get the host's user ID
            var userId = userManager.GetUserId(User)!;

            // Get hosts bookings, including property, guest, order by pending and desc
            var bookings = await db.Bookings
                .Include(b => b.Property)
                .Include(b => b.Guest)
                .Where(b => b.Property.HostId == userId)
                .OrderBy(b => b.Status == BookingStatus.Pending ? 0 : 1)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();

            // Return model with view
            return View(bookings);
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Approve booking request
        public async Task<IActionResult> Approve(int id)
        {
            // Get booking request by ID, if not found return not found
            var booking = await LoadOwnBookingAsync(id);
            if (booking is null) return NotFound();

            // Check if booking is pending, if not return error
            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "This booking request has already been handled.";
                return RedirectToAction(nameof(Index));
            }

            // Get the host's user ID
            var hostId = userManager.GetUserId(User)!;

            // Check for conflicting bookings, overlapping dates etc
            var conflict = await db.Bookings.AnyAsync(b => b.Id != booking.Id && b.PropertyId == booking.PropertyId && (b.Status == BookingStatus.Approved || b.Status == BookingStatus.CheckedIn) && b.CheckInDate < booking.CheckOutDate && b.CheckOutDate > booking.CheckInDate);

            // If there is a conflict, return error
            if (conflict)
            {
                TempData["Error"] = "These dates overlap with another approved booking for this property. Reject this request instead.";
                return RedirectToAction(nameof(Index));
            }

            // If no conflict, approve the booking
            booking.Status = BookingStatus.Approved;

            // Save db changes
            await db.SaveChangesAsync();

            // Log activity and notify guest
            await activityLog.LogAsync(hostId, ActivityType.BookingStatusChange, $"Booking #{booking.Id} Pending to Approved: {booking.Property.Title}");
            await notifications.CreateAsync(booking.GuestId, $"Booking approved: {booking.Property.Title}", NotificationType.Booking);

            // Notify host of success and redirect to index
            TempData["Success"] = "Booking approved and the guest has been notified.";
            return RedirectToAction(nameof(Index));
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Reject booking request
        public async Task<IActionResult> Reject(int id)
        {
            // Get booking request by ID, if not found return not found
            var booking = await LoadOwnBookingAsync(id);
            if (booking is null) return NotFound();

            // Check if booking is pending, if not return error
            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "This booking request has already been handled.";
                return RedirectToAction(nameof(Index));
            }

            // Get host user id
            var hostId = userManager.GetUserId(User)!;

            // Set booking status to rejected
            booking.Status = BookingStatus.Rejected;
            
            // Save db changes
            await db.SaveChangesAsync();

            // Log activity and notify guest
            await activityLog.LogAsync(hostId, ActivityType.BookingStatusChange, $"Booking #{booking.Id} Pending to Rejected: {booking.Property.Title}");
            await notifications.CreateAsync(booking.GuestId, $"Booking rejected: {booking.Property.Title}", NotificationType.Booking);

            // Notify host of success and redirect to index
            TempData["Success"] = "Booking rejected and the guest has been notified.";
            return RedirectToAction(nameof(Index));
        }

        // Only load bookings that belong to the host
        private Task<Booking?> LoadOwnBookingAsync(int id)
        {
            var hostId = userManager.GetUserId(User)!;
            return db.Bookings.Include(b => b.Property).FirstOrDefaultAsync(b => b.Id == id && b.Property.HostId == hostId);
        }
    }
}
