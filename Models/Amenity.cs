using System.ComponentModel.DataAnnotations;

namespace staybnb_csharp.Models
{
    // Amenity model
    public class Amenity
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string IconClass { get; set; } = string.Empty;

        public ICollection<HostProperty> Properties { get; set; } = new List<HostProperty>();
    }
}
