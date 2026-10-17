using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.Services
{
    public interface INotificationService
    {
        Task CreateAsync(string userId, string title, NotificationType type);
    }

    public class NotificationService(ApplicationDbContext db) : INotificationService
    {
        public async Task CreateAsync(string userId, string title, NotificationType type)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = title.Length > 100 ? title[..100] : title
            });
            await db.SaveChangesAsync();
        }
    }
}
