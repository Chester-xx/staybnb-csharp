using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.Models
{
    // Host property model
    public class HostProperty
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerNight { get; set; }

        [Required]
        public string HostId { get; set; } = string.Empty;
        public ApplicationUser Host { get; set; } = null!;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        public PropertyType PropertyType { get; set; }

        public bool IsActive { get; set; }

        // Methods
        public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
        public CheckInProcess? CheckInProcess { get; set; }
    }
}
