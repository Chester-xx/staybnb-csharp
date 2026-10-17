using System.ComponentModel.DataAnnotations;

namespace staybnb_csharp.Models
{
    public class PropertyImage
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public HostProperty Property { get; set; } = null!;

        // Holds the generated stored file name (timestamp + suffix + extension).
        [Required]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
