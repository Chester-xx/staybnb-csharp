using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using staybnb_csharp.Models;
using staybnb_csharp.Services;

namespace staybnb_csharp.Controllers
{
    // Signed in users only
    [Authorize]
    // Profile pictures controller, pictures live outside wwwroot so they are served here
    public class ProfilePicturesController(UserManager<ApplicationUser> userManager, IFileStorageService files) : Controller
    {
        // Define show method
        public async Task<IActionResult> Show(string id)
        {
            // Get the user and their profile picture, if there is no file, return NotFound
            var user = await userManager.FindByIdAsync(id);
            var file = files.GetProfilePicture(user?.ProfilePictureUrl);
            if (file is null) return NotFound();

            // Return the file, cached privately for a day
            Response.Headers.CacheControl = "private, max-age=86400";
            return PhysicalFile(file.Value.Path, file.Value.ContentType);
        }
    }
}
