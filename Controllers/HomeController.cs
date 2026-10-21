using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.ViewModels;
using System.Diagnostics;

namespace staybnb_csharp.Controllers
{
    // Home controller
    public class HomeController(ApplicationDbContext db) : Controller
    {
        // Define read method, filtered by city and property type
        public async Task<IActionResult> Index(string? city, PropertyType? propertyType)
        {
            // Get all active properties
            var active = db.HostProperties.Where(p => p.IsActive);

            // Include the images
            var query = active.Include(p => p.Images).AsQueryable();

            // Filter by city and property type if given
            if (!string.IsNullOrWhiteSpace(city)) { query = query.Where(p => p.City == city); }
            if (propertyType is not null) { query = query.Where(p => p.PropertyType == propertyType); }

            // Build the model with the filters, the distinct cities and the properties ordered desc
            var model = new PropertyListViewModel
            {
                City = city,
                PropertyType = propertyType,
                Cities = await active.Select(p => p.City).Distinct().OrderBy(c => c).ToListAsync(),
                Properties = await query.OrderByDescending(p => p.Id).ToListAsync()
            };

            // Return the model with the view
            return View(model);
        }

        // Define privacy method
        public IActionResult Privacy() { return View(); }

        // Define error method, never cached
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() { return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); }
    }
}
