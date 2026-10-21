using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.Services;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Controllers
{
    // Guest only
    [Authorize(Roles = Roles.Guest)]
    // Bookings controller
    public class BookingsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IPricingService pricing, INotificationService notifications, IActivityLogService activityLog) : Controller
    {
        // Booking limits
        private const int MaxGuests = 10;
        private const int MaxNights = 365;

        // Define read method
        public async Task<IActionResult> Index()
        {
            // Get guest id
            var guestId = userManager.GetUserId(User)!;

            // Get all bookings for the guest, including related property images and check-in, ordered desc
            var bookings = await db.Bookings
                .Include(b => b.Property).ThenInclude(p => p.Images)
                .Include(b => b.GuestCheckIn)
                .Where(b => b.GuestId == guestId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            // Return the list with the view
            return View(bookings);
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define create booking method
        public async Task<IActionResult> Create(BookingCreateViewModel model)
        {
            // Get the active property, if it does not exist, return NotFound
            var property = await db.HostProperties.FirstOrDefaultAsync(p => p.Id == model.PropertyId && p.IsActive);
            if (property is null) return NotFound();

            // Get the dates and number of nights
            var checkIn = model.CheckInDate.Date;
            var checkOut = model.CheckOutDate.Date;
            var nights = (checkOut - checkIn).Days;

            // Validate the booking details, then check the dates are still available
            var error = Validate(checkIn, nights, model.NumberOfGuests) ?? (await HasConflictAsync(property.Id, checkIn, checkOut) ? "Those dates are no longer available for this property." : null);

            // If there is an error, redirect back to the property with an error message
            if (error is not null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Details", "Properties", new { id = property.Id });
            }

            // Get the guest and calculate the price
            var guest = (await userManager.GetUserAsync(User))!;
            var price = pricing.Calculate(property.PricePerNight, nights);

            // Create the booking as pending
            var booking = new Booking
            {
                PropertyId = property.Id,
                GuestId = guest.Id,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                NumberOfGuests = model.NumberOfGuests,
                TotalPrice = price.Total,
                Status = BookingStatus.Pending
            };

            // Save db changes
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            // Log the activity and notify the host and the guest
            await activityLog.LogAsync(guest.Id, ActivityType.BookingStatusChange, $"Booking #{booking.Id} requested (Pending): {property.Title}");
            await notifications.CreateAsync(property.HostId, $"New booking request: {property.Title}", NotificationType.Booking);
            await notifications.CreateAsync(guest.Id, $"Booking requested: {property.Title}", NotificationType.Booking);

            // Redirect to index with a success message
            TempData["Success"] = "Your booking request was sent to the host.";
            return RedirectToAction(nameof(Index));
        }

        // Define validate method, returns an error message or null
        private static string? Validate(DateTime checkIn, int nights, int guests)
        {
            // Check the dates and number of guests
            if (checkIn < DateTime.Today) return "Check-in cannot be in the past.";
            if (nights < 1) return "Check-out must be after check-in.";
            if (nights > MaxNights) return $"Bookings can be at most {MaxNights} nights.";
            if (guests < 1 || guests > MaxGuests) return $"Number of guests must be between 1 and {MaxGuests}.";

            // No error
            return null;
        }

        // Define has conflict method, checks for approved or checked-in bookings overlapping the dates
        private Task<bool> HasConflictAsync(int propertyId, DateTime checkIn, DateTime checkOut) => db.Bookings.AnyAsync(b => b.PropertyId == propertyId && (b.Status == BookingStatus.Approved || b.Status == BookingStatus.CheckedIn) && b.CheckInDate < checkOut && b.CheckOutDate > checkIn);
    }
}
