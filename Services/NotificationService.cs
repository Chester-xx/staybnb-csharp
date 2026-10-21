using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.Services
{
    // Notification service interface
    public interface INotificationService
    {
        // Methods
        Task CreateAsync(string userId, string title, NotificationType type);
    }

    // Notification service
    public class NotificationService(ApplicationDbContext db) : INotificationService
    {
        // Define create method
        public async Task CreateAsync(string userId, string title, NotificationType type)
        {
            // Add the notification, 100 length only
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = title.Length > 100 ? title[..100] : title
            });

            // Save db changes
            await db.SaveChangesAsync();
        }
    }
}
