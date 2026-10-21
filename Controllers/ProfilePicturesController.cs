using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using staybnb_csharp.Models;
using staybnb_csharp.Services;

namespace staybnb_csharp.Controllers
{
    // Profile pictures live outside wwwroot, so they are served here to signed-in users.
    [Authorize]
    public class ProfilePicturesController(UserManager<ApplicationUser> userManager, IFileStorageService files) : Controller
    {
        public async Task<IActionResult> Show(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            var file = files.GetProfilePicture(user?.ProfilePictureUrl);
            if (file is null) return NotFound();

            Response.Headers.CacheControl = "private, max-age=86400";
            return PhysicalFile(file.Value.Path, file.Value.ContentType);
        }
    }
}
