using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.Services;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Areas.Host.Controllers
{
    // Host only
    [Area("Host")]
    [Authorize(Roles = Roles.Host)]
    
    // Properties controller
    public class PropertiesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IHostApplicationService applications, IFileStorageService files, IActivityLogService activityLog) : Controller
    {
        // Define read
        public async Task<IActionResult> Index()
        {
            // Get user id
            var userId = userManager.GetUserId(User)!;

            // Get all applications for the host's properties, including related property data and images, ordered desc
            var list = await db.HostApplications
                .Include(a => a.Property).ThenInclude(p => p.Images)
                .Include(a => a.Property).ThenInclude(p => p.CheckInProcess)
                .Where(a => a.Property.HostId == userId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            // Return the list with the view
            return View(list);
        }

        // Get only
        [HttpGet]
        // Define create new property method
        public async Task<IActionResult> Create()
        {
            // Check if the user already has a pending application, if so, redirect to index with an error message
            if (await applications.HasPendingAsync(userManager.GetUserId(User)!))
            {
                TempData["Error"] = "You already have an application under review.";
                return RedirectToAction(nameof(Index));
            }

            // Return host application model with view
            return View(new HostApplicationViewModel());
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Set request size limit to 60 MB for image uploads + form data
        [RequestSizeLimit(60_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 60_000_000)]
        // Define create new property method
        public async Task<IActionResult> Create(HostApplicationViewModel model)
        {
            // Get user obj
            var user = (await userManager.GetUserAsync(User))!;

            // Check if the user already has a pending application, if so, redirect to index with an error message
            if (await applications.HasPendingAsync(user.Id))
            {
                TempData["Error"] = "You already have an application under review.";
                return RedirectToAction(nameof(Index));
            }

            // Validate images and add errors to ModelState if any (.ext / size / count)
            foreach (var error in applications.ValidateImages(model.Images)) { ModelState.AddModelError(nameof(model.Images), error); }

            // If model state is invalid, return the view with the model to show validation errors
            if (!ModelState.IsValid) { return View(model); }

            // Submit the application
            await applications.SubmitAsync(user, model);

           // Redirect to index with a success message
            TempData["Success"] = "Your property has been submitted and is waiting for admin approval.";
            return RedirectToAction(nameof(Index));
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define delete properties method
        public async Task<IActionResult> Delete(int[] ids)
        {
            // Get user id
            var userId = userManager.GetUserId(User)!;

            // Check if no properties are selected for del, if so, return to index with an error message
            if (ids.Length == 0)
            {
                TempData["Error"] = "Select at least one property to remove.";
                return RedirectToAction(nameof(Index));
            }

            // Get all properties for the host with the given ids, including related images and amenities
            var properties = await db.HostProperties
                .Include(p => p.Images)
                .Include(p => p.Amenities)
                .Where(p => ids.Contains(p.Id) && p.HostId == userId)
                .ToListAsync();

            // Get all property ids that have bookings (distinct)
            var withBookings = await db.Bookings
                .Where(b => ids.Contains(b.PropertyId))
                .Select(b => b.PropertyId)
                .Distinct()
                .ToListAsync();

            // Separate properties into removable (no bookings) and blocked (has bookings)
            var removable = properties.Where(p => !withBookings.Contains(p.Id)).ToList();
            var blocked = properties.Where(p => withBookings.Contains(p.Id)).ToList();

            // If there are removable properties
            if (removable.Count > 0)
            {
                // Get the ids of the removable properties
                var removableIds = removable.Select(p => p.Id).ToList();

                // Remove the applications, check-in processes, reviews, wishlist items, and amenities for the removable properties
                db.HostApplications.RemoveRange(await db.HostApplications.Where(a => removableIds.Contains(a.PropertyId)).ToListAsync());
                db.CheckInProcesses.RemoveRange(await db.CheckInProcesses.Where(c => removableIds.Contains(c.PropertyId)).ToListAsync());
                db.Reviews.RemoveRange(await db.Reviews.Where(r => removableIds.Contains(r.PropertyId)).ToListAsync());
                db.WishlistItems.RemoveRange(await db.WishlistItems.Where(w => removableIds.Contains(w.PropertyId)).ToListAsync());
                removable.ForEach(p => p.Amenities.Clear());
                db.HostProperties.RemoveRange(removable);
                
                // Save db changes
                await db.SaveChangesAsync();

                // Delete the images for the removable properties and log the activity
                foreach (var property in removable)
                {
                    property.Images.ToList().ForEach(i => files.DeletePropertyImage(i.ImageUrl));
                    await activityLog.LogAsync(userId, ActivityType.Property, $"Removed property: {property.Title}");
                }

                // Set success message in TempData based on the number of properties removed
                TempData["Success"] = removable.Count == 1 ? "1 property was removed." : $"{removable.Count} properties were removed.";
            }

            // If there are blocked properties, set an error message in TempData with their titles
            if (blocked.Count > 0)
            {
                var names = string.Join(", ", blocked.Select(p => $"'{p.Title}'"));
                TempData["Error"] = $"{names} could not be removed because it has bookings. Deactivate it instead.";
            }

            // Redirect to index
            return RedirectToAction(nameof(Index));
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define toggle active status method
        public async Task<IActionResult> ToggleActive(int id)
        {
            // Get user id
            var userId = userManager.GetUserId(User)!;

            // Get host application for the property by its id, including the property and its checkin process
            var application = await db.HostApplications
                .Include(a => a.Property).ThenInclude(p => p.CheckInProcess)
                .FirstOrDefaultAsync(a => a.PropertyId == id && a.Property.HostId == userId);

            // If the application is null, return NotFound
            if (application is null) return NotFound();

            // Check if the application is approved, if not, set an error message in TempData and redirect to index
            if (application.Status != ApplicationStatus.Approved)
            {
                TempData["Error"] = "Only approved properties can be listed.";
                return RedirectToAction(nameof(Index));
            }

            // Get the property from the application
            var property = application.Property;

            // Check if the property is already listed, if so, return BadRequest
            if (!property.IsActive && property.CheckInProcess is null)
            {
                TempData["Error"] = "Set up check-in before listing this property.";
                return RedirectToAction(nameof(Index));
            }

            // Toggle the active status of the property
            property.IsActive = !property.IsActive;

            // Save db changes
            await db.SaveChangesAsync();

            // Log activity
            await activityLog.LogAsync(userId, ActivityType.Property, $"{(property.IsActive ? "Activated" : "Deactivated")} property: {property.Title}");

            // Set success message in TempData based on the new active status of the property, and redirect to index
            TempData["Success"] = property.IsActive
                ? $"'{property.Title}' is now listed."
                : $"'{property.Title}' has been taken off the marketplace.";
            return RedirectToAction(nameof(Index));
        }
    }
}
