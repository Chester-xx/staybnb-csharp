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
    [Authorize(Roles = Roles.Guest)]
    public class HostApplicationController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IHostApplicationService applications) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Apply()
        {
            if (await applications.HasPendingAsync(userManager.GetUserId(User)!))
            {
                TempData["Info"] = "You already have an application under review.";
                return RedirectToAction(nameof(Status));
            }

            return View(new HostApplicationViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(60_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 60_000_000)]
        public async Task<IActionResult> Apply(HostApplicationViewModel model)
        {
            var user = (await userManager.GetUserAsync(User))!;

            if (await applications.HasPendingAsync(user.Id))
            {
                TempData["Info"] = "You already have an application under review.";
                return RedirectToAction(nameof(Status));
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

            TempData["Success"] = "Your application has been submitted and is waiting for review.";
            return RedirectToAction(nameof(Status));
        }

        [HttpGet]
        public async Task<IActionResult> Status()
        {
            var userId = userManager.GetUserId(User)!;

            var list = await db.HostApplications
                .Include(a => a.Property)
                .Where(a => a.ApplicationUserId == userId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(list);
        }
    }
}
