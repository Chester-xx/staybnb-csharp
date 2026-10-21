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
    [Area("Host")]
    [Authorize(Roles = Roles.Host)]
    public class PropertiesController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IHostApplicationService applications,
        IFileStorageService files,
        IActivityLogService activityLog) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var userId = userManager.GetUserId(User)!;

            // Every property has exactly one application, so the application list is the property list.
            var list = await db.HostApplications
                .Include(a => a.Property).ThenInclude(p => p.Images)
                .Include(a => a.Property).ThenInclude(p => p.CheckInProcess)
                .Where(a => a.Property.HostId == userId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (await applications.HasPendingAsync(userManager.GetUserId(User)!))
            {
                TempData["Error"] = "You already have an application under review.";
                return RedirectToAction(nameof(Index));
            }

            return View(new HostApplicationViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(60_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 60_000_000)]
        public async Task<IActionResult> Create(HostApplicationViewModel model)
        {
            var user = (await userManager.GetUserAsync(User))!;

            if (await applications.HasPendingAsync(user.Id))
            {
                TempData["Error"] = "You already have an application under review.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in applications.ValidateImages(model.Images))
            {
                ModelState.AddModelError(nameof(model.Images), error);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await applications.SubmitAsync(user, model);

            TempData["Success"] = "Your property has been submitted and is waiting for admin approval.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int[] ids)
        {
            var userId = userManager.GetUserId(User)!;

            if (ids.Length == 0)
            {
                TempData["Error"] = "Select at least one property to remove.";
                return RedirectToAction(nameof(Index));
            }

            // Only the signed-in host's own properties are considered.
            var properties = await db.HostProperties
                .Include(p => p.Images)
                .Include(p => p.Amenities)
                .Where(p => ids.Contains(p.Id) && p.HostId == userId)
                .ToListAsync();

            // Properties with any booking history are kept so bookings, check-ins and logs stay intact.
            var withBookings = await db.Bookings
                .Where(b => ids.Contains(b.PropertyId))
                .Select(b => b.PropertyId)
                .Distinct()
                .ToListAsync();

            var removable = properties.Where(p => !withBookings.Contains(p.Id)).ToList();
            var blocked = properties.Where(p => withBookings.Contains(p.Id)).ToList();

            if (removable.Count > 0)
            {
                var removableIds = removable.Select(p => p.Id).ToList();

                db.HostApplications.RemoveRange(await db.HostApplications.Where(a => removableIds.Contains(a.PropertyId)).ToListAsync());
                db.CheckInProcesses.RemoveRange(await db.CheckInProcesses.Where(c => removableIds.Contains(c.PropertyId)).ToListAsync());
                db.Reviews.RemoveRange(await db.Reviews.Where(r => removableIds.Contains(r.PropertyId)).ToListAsync());
                db.WishlistItems.RemoveRange(await db.WishlistItems.Where(w => removableIds.Contains(w.PropertyId)).ToListAsync());
                removable.ForEach(p => p.Amenities.Clear());
                db.HostProperties.RemoveRange(removable);
                await db.SaveChangesAsync();

                foreach (var property in removable)
                {
                    property.Images.ToList().ForEach(i => files.DeletePropertyImage(i.ImageUrl));
                    await activityLog.LogAsync(userId, ActivityType.Property, $"Removed property: {property.Title}");
                }

                TempData["Success"] = removable.Count == 1
                    ? "1 property was removed."
                    : $"{removable.Count} properties were removed.";
            }

            if (blocked.Count > 0)
            {
                var names = string.Join(", ", blocked.Select(p => $"'{p.Title}'"));
                TempData["Error"] = $"{names} could not be removed because it has bookings. Deactivate it instead.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var userId = userManager.GetUserId(User)!;

            var application = await db.HostApplications
                .Include(a => a.Property).ThenInclude(p => p.CheckInProcess)
                .FirstOrDefaultAsync(a => a.PropertyId == id && a.Property.HostId == userId);

            if (application is null) return NotFound();

            if (application.Status != ApplicationStatus.Approved)
            {
                TempData["Error"] = "Only approved properties can be listed.";
                return RedirectToAction(nameof(Index));
            }

            var property = application.Property;

            if (!property.IsActive && property.CheckInProcess is null)
            {
                TempData["Error"] = "Set up check-in before listing this property.";
                return RedirectToAction(nameof(Index));
            }

            property.IsActive = !property.IsActive;
            await db.SaveChangesAsync();

            await activityLog.LogAsync(userId, ActivityType.Property,
                $"{(property.IsActive ? "Activated" : "Deactivated")} property: {property.Title}");

            TempData["Success"] = property.IsActive
                ? $"'{property.Title}' is now listed."
                : $"'{property.Title}' has been taken off the marketplace.";
            return RedirectToAction(nameof(Index));
        }
    }
}
