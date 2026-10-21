using System.ComponentModel.DataAnnotations;
using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.ViewModels
{
    // Host application view model
    public class HostApplicationViewModel
    {
        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(1, 1000000)]
        [Display(Name = "Price per night (R)")]
        public decimal PricePerNight { get; set; }

        [Required, StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Display(Name = "Property type")]
        public PropertyType PropertyType { get; set; }

        public List<IFormFile> Images { get; set; } = new();
    }
}
