using System.ComponentModel.DataAnnotations;

namespace staybnb_csharp.Models
{
    // Property image model
    public class PropertyImage
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public HostProperty Property { get; set; } = null!;

        // Generated stored file name
        [Required]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
