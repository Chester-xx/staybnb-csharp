using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Services
{
    // Host application service interface
    public interface IHostApplicationService
    {
        // Methods
        Task<bool> HasPendingAsync(string userId);
        IEnumerable<string> ValidateImages(IReadOnlyCollection<IFormFile> images);
        Task SubmitAsync(ApplicationUser user, HostApplicationViewModel model);
    }

    // Host application service
    public class HostApplicationService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IFileStorageService files, INotificationService notifications,IActivityLogService activityLog) : IHostApplicationService
    {
        // Define has pending method
        public Task<bool> HasPendingAsync(string userId) =>
            db.HostApplications.AnyAsync(a => a.ApplicationUserId == userId && a.Status == ApplicationStatus.Pending);

        // Define validate images method
        public IEnumerable<string> ValidateImages(IReadOnlyCollection<IFormFile> images)
        {
            // At least one image is required
            if (images.Count == 0)
            {
                yield return "Please upload at least one image of your property.";
                yield break;
            }

            // No more than the max number of images
            if (images.Count > IFileStorageService.MaxImagesPerApplication)
            {
                yield return $"You can upload at most {IFileStorageService.MaxImagesPerApplication} images.";
                yield break;
            }

            // Validate each image and return any errors
            foreach (var image in images)
            {
                var error = files.ValidateImage(image);
                if (error is not null) yield return error;
            }
        }

        // Define submit method
        public async Task SubmitAsync(ApplicationUser user, HostApplicationViewModel model)
        {
            // Track saved image files so they can be cleaned up if saving fails
            var savedFiles = new List<string>();

            try
            {
                // Build the property from the form, inactive until approved
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

                // Save each image to storage and attach it to the property
                foreach (var image in model.Images)
                {
                    var storedName = await files.SavePropertyImageAsync(image);
                    savedFiles.Add(storedName);
                    property.Images.Add(new PropertyImage { ImageUrl = storedName });
                }

                // Add the application as pending
                db.HostApplications.Add(new HostApplication
                {
                    ApplicationUserId = user.Id,
                    Property = property,
                    Status = ApplicationStatus.Pending
                });

                // Save db changes
                await db.SaveChangesAsync();
            }
            catch
            {
                // Delete the saved images so no orphaned files remain, then rethrow
                savedFiles.ForEach(files.DeletePropertyImage);
                throw;
            }

            // Check if the user is already a host, meaning this is an additional property
            var isAdditional = await userManager.IsInRoleAsync(user, Roles.Host);

            // Log the activity
            await activityLog.LogAsync(user.Id, ActivityType.HostApplication, isAdditional ? $"Submitted additional property application: {model.Title}" : $"Submitted host application: {model.Title}");

            // Notify the applicant
            await notifications.CreateAsync(user.Id, isAdditional ? "Additional property application submitted" : "Host application submitted", NotificationType.HostApplication);

            // Get all admins and super admins (distinct)
            var admins = (await userManager.GetUsersInRoleAsync(Roles.Admin)).Concat(await userManager.GetUsersInRoleAsync(Roles.SuperAdmin)).DistinctBy(a => a.Id);

            // Notify each admin of the new application
            foreach (var admin in admins) { await notifications.CreateAsync(admin.Id, isAdditional ? $"New property application from {user.FullName}" : $"New host application from {user.FullName}", NotificationType.HostApplication); }
        }
    }
}
