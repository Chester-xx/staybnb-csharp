using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.ViewModels;
using System.Diagnostics;

namespace staybnb_csharp.Controllers
{
    public class HomeController(ApplicationDbContext db) : Controller
    {
        public async Task<IActionResult> Index(string? city, PropertyType? propertyType)
        {
            var active = db.HostProperties.Where(p => p.IsActive);

            var query = active.Include(p => p.Images).AsQueryable();

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(p => p.City == city);
            }

            if (propertyType is not null)
            {
                query = query.Where(p => p.PropertyType == propertyType);
            }

            var model = new PropertyListViewModel
            {
                City = city,
                PropertyType = propertyType,
                Cities = await active.Select(p => p.City).Distinct().OrderBy(c => c).ToListAsync(),
                Properties = await query.OrderByDescending(p => p.Id).ToListAsync()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
