using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.Services;
using staybnb_csharp.ViewModels;
using System.Text.Json;

namespace staybnb_csharp.Controllers
{
    // Properties controller
    public class PropertiesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IPricingService pricing) : Controller
    {
        // Define details method
        public async Task<IActionResult> Details(int id)
        {
            // Get the property by its id, including the images and the host, if it does not exist, return NotFound
            var property = await db.HostProperties
                .Include(p => p.Images)
                .Include(p => p.Host)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (property is null) return NotFound();

            // Check if the viewer is the owner or staff
            var userId = userManager.GetUserId(User);
            var isOwner = userId is not null && property.HostId == userId;
            var isStaff = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);

            // Unlisted properties are only visible to their host and to admins
            if (!property.IsActive && !isOwner && !isStaff) return NotFound();

            // Build the model with the fee percentages
            var model = new PropertyDetailsViewModel
            {
                Property = property,
                CleaningFeePercent = pricing.CleaningFeePercent,
                ServiceFeePercent = pricing.ServiceFeePercent
            };

            // Get the upcoming approved or checked-in stays that already block dates, the check-out day is free for the next guest
            var booked = await db.Bookings
                .Where(b => b.PropertyId == id && (b.Status == BookingStatus.Approved || b.Status == BookingStatus.CheckedIn) && b.CheckOutDate > DateTime.Today)
                .OrderBy(b => b.CheckInDate)
                .Select(b => new { b.CheckInDate, b.CheckOutDate })
                .ToListAsync();

            // Set the booked ranges and their json for the booking calculator
            model.BookedRanges = booked.Select(b => new BookedRange(b.CheckInDate.ToString("yyyy-MM-dd"), b.CheckOutDate.ToString("yyyy-MM-dd"))).ToList();
            model.BookedRangesJson = JsonSerializer.Serialize(model.BookedRanges, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            // Set the reason the viewer cannot book, if any
            if (User.Identity?.IsAuthenticated != true) { model.NeedsLogin = true; model.BookingBlockedReason = "Log in with a guest account to book this property."; }
            else if (!User.IsInRole(Roles.Guest)) { model.BookingBlockedReason = "Only guest accounts can make bookings."; }
            else if (!property.IsActive) { model.BookingBlockedReason = "This property is not currently listed."; }

            // Return the model with the view
            return View(model);
        }
    }
}
