using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Services
{
    public interface IHostApplicationService
    {
        Task<bool> HasPendingAsync(string userId);
        IEnumerable<string> ValidateImages(IReadOnlyCollection<IFormFile> images);
        Task SubmitAsync(ApplicationUser user, HostApplicationViewModel model);
    }

    public class HostApplicationService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileStorageService files,
        INotificationService notifications,
        IActivityLogService activityLog) : IHostApplicationService
    {
        public Task<bool> HasPendingAsync(string userId) =>
            db.HostApplications.AnyAsync(a => a.ApplicationUserId == userId && a.Status == ApplicationStatus.Pending);

        public IEnumerable<string> ValidateImages(IReadOnlyCollection<IFormFile> images)
        {
            if (images.Count == 0)
            {
                yield return "Please upload at least one image of your property.";
                yield break;
            }

            if (images.Count > IFileStorageService.MaxImagesPerApplication)
            {
                yield return $"You can upload at most {IFileStorageService.MaxImagesPerApplication} images.";
                yield break;
            }

            foreach (var image in images)
            {
                var error = files.ValidateImage(image);
                if (error is not null) yield return error;
            }
        }

        public async Task SubmitAsync(ApplicationUser user, HostApplicationViewModel model)
        {
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

            var isAdditional = await userManager.IsInRoleAsync(user, Roles.Host);

            await activityLog.LogAsync(user.Id, ActivityType.HostApplication,
                isAdditional
                    ? $"Submitted additional property application: {model.Title}"
                    : $"Submitted host application: {model.Title}");

            await notifications.CreateAsync(user.Id,
                isAdditional ? "Additional property application submitted" : "Host application submitted",
                NotificationType.HostApplication);

            var admins = (await userManager.GetUsersInRoleAsync(Roles.Admin))
                .Concat(await userManager.GetUsersInRoleAsync(Roles.SuperAdmin))
                .DistinctBy(a => a.Id);

            foreach (var admin in admins)
            {
                await notifications.CreateAsync(admin.Id,
                    isAdditional
                        ? $"New property application from {user.FullName}"
                        : $"New host application from {user.FullName}",
                    NotificationType.HostApplication);
            }
        }
    }
}
