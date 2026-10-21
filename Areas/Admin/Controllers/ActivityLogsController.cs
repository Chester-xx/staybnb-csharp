using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Areas.Admin.Controllers
{
    // Admin access only
    [Area("Admin")]
    [Authorize(Roles = Roles.AdminOrSuperAdmin)]

    // Activity logs controller    
    public class ActivityLogsController(ApplicationDbContext db) : Controller
    {
        // set max size for activities
        private const int PageSize = 25;

        // Define read
        public async Task<IActionResult> Index(ActivityType? type, int page = 1)
        {
            // Get model query
            var query = db.ActivityLogs.Include(l => l.User).AsQueryable();

            // if type is not null, filter by type
            if (type is not null) { query = query.Where(l => l.ActivityType == type); }

           // Calculate total and total pages
            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            // Get items for current page, by type; page; order by created desc
            var model = new ActivityLogListViewModel
            {
                Type = type,
                Page = page,
                TotalPages = totalPages,
                Items = await query
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync()
            };

            // Return view with model
            return View(model);
        }
    }
}
