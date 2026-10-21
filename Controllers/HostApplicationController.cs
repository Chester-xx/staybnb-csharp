using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Services;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Controllers
{
    // Guest only
    [Authorize(Roles = Roles.Guest)]
    // Host application controller
    public class HostApplicationController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IHostApplicationService applications) : Controller
    {
        // Get only
        [HttpGet]
        // Define apply method
        public async Task<IActionResult> Apply()
        {
            // Check if the user already has a pending application, if so, redirect to status with an info message
            if (await applications.HasPendingAsync(userManager.GetUserId(User)!))
            {
                TempData["Info"] = "You already have an application under review.";
                return RedirectToAction(nameof(Status));
            }

            // Return an empty application model with the view
            return View(new HostApplicationViewModel());
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Set request size limit to 60 MB for image uploads and form data
        [RequestSizeLimit(60_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 60_000_000)]
        // Define apply method
        public async Task<IActionResult> Apply(HostApplicationViewModel model)
        {
            // Get user obj
            var user = (await userManager.GetUserAsync(User))!;

            // Check if the user already has a pending application, if so, redirect to status with an info message
            if (await applications.HasPendingAsync(user.Id))
            {
                TempData["Info"] = "You already have an application under review.";
                return RedirectToAction(nameof(Status));
            }

            // Validate images and add errors to ModelState if any
            foreach (var error in applications.ValidateImages(model.Images)) { ModelState.AddModelError(nameof(model.Images), error); }

            // If model state is invalid, return the view with the model to show validation errors
            if (!ModelState.IsValid) { return View(model); }

            // Submit the application
            await applications.SubmitAsync(user, model);

            // Redirect to status with a success message
            TempData["Success"] = "Your application has been submitted and is waiting for review.";
            return RedirectToAction(nameof(Status));
        }

        // Get only
        [HttpGet]
        // Define status method
        public async Task<IActionResult> Status()
        {
            // Get user id
            var userId = userManager.GetUserId(User)!;

            // Get all applications for the user, including the property, ordered desc
            var list = await db.HostApplications
                .Include(a => a.Property)
                .Where(a => a.ApplicationUserId == userId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            // Return the list with the view
            return View(list);
        }
    }
}
