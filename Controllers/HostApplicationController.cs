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
    [Authorize(Roles = Roles.Guest)]
    public class HostApplicationController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileStorageService files,
        INotificationService notifications,
        IActivityLogService activityLog) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Apply()
        {
            if (await HasPendingApplicationAsync())
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
            if (await HasPendingApplicationAsync())
            {
                TempData["Info"] = "You already have an application under review.";
                return RedirectToAction(nameof(Status));
            }

            ValidateImages(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = (await userManager.GetUserAsync(User))!;
            var savedFiles = new List<string>();

            try
            {
                var property = new HostProperty
                {
                    Title = model.Title.Trim(),
                    Description = model.Description.Trim(),
                    PricePerNight = model.PricePerNight,
                    Address = model.Address.Trim(),
                    City = model.City.Trim(),
                    PropertyType = model.PropertyType,
                    HostId = user.Id,
                    IsActive = false
                };

                foreach (var image in model.Images)
                {
                    var storedName = await files.SavePropertyImageAsync(image);
                    savedFiles.Add(storedName);
                    property.Images.Add(new PropertyImage { ImageUrl = storedName });
                }

                db.HostApplications.Add(new HostApplication
                {
                    ApplicationUserId = user.Id,
                    Property = property,
                    Status = ApplicationStatus.Pending
                });

                await db.SaveChangesAsync();
            }
            catch
            {
                savedFiles.ForEach(files.DeletePropertyImage);
                throw;
            }

            await activityLog.LogAsync(user.Id, ActivityType.HostApplication, $"Submitted host application: {model.Title}");
            await notifications.CreateAsync(user.Id, "Host application submitted", NotificationType.HostApplication);
            await NotifyAdminsAsync(user);

            TempData["Success"] = "Your application has been submitted and is waiting for review.";
            return RedirectToAction(nameof(Status));
        }

        [HttpGet]
        public async Task<IActionResult> Status()
        {
            var userId = userManager.GetUserId(User)!;

            var applications = await db.HostApplications
                .Include(a => a.Property)
                .Where(a => a.ApplicationUserId == userId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(applications);
        }

        private async Task<bool> HasPendingApplicationAsync()
        {
            var userId = userManager.GetUserId(User)!;
            return await db.HostApplications.AnyAsync(a =>
                a.ApplicationUserId == userId && a.Status == ApplicationStatus.Pending);
        }

        private void ValidateImages(HostApplicationViewModel model)
        {
            if (model.Images.Count == 0)
            {
                ModelState.AddModelError(nameof(model.Images), "Please upload at least one image of your property.");
                return;
            }

            if (model.Images.Count > IFileStorageService.MaxImagesPerApplication)
            {
                ModelState.AddModelError(nameof(model.Images),
                    $"You can upload at most {IFileStorageService.MaxImagesPerApplication} images.");
                return;
            }

            foreach (var image in model.Images)
            {
                var error = files.ValidateImage(image);
                if (error is not null)
                {
                    ModelState.AddModelError(nameof(model.Images), error);
                }
            }
        }

        private async Task NotifyAdminsAsync(ApplicationUser applicant)
        {
            var admins = (await userManager.GetUsersInRoleAsync(Roles.Admin))
                .Concat(await userManager.GetUsersInRoleAsync(Roles.SuperAdmin))
                .DistinctBy(a => a.Id);

            foreach (var admin in admins)
            {
                await notifications.CreateAsync(admin.Id, $"New host application from {applicant.FullName}", NotificationType.HostApplication);
            }
        }
    }
}
