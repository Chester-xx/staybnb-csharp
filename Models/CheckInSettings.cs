using System.Text.Json;

namespace staybnb_csharp.Models
{
    // Check-in settings, stored as json in the check-in process steps
    public class CheckInSettings
    {
        // Document types a host can require
        public static readonly string[] DocumentOptions =
        {
            "ID document",
            "Passport",
            "Driver's licence",
            "Proof of address",
            "Vehicle registration"
        };

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public List<string> Rules { get; set; } = new();
        public List<string> RequiredDocuments { get; set; } = new();

        // Define to json method
        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        // Define from json method, falls back to empty settings when the json is missing or invalid
        public static CheckInSettings FromJson(string? json)
        {
            try { return JsonSerializer.Deserialize<CheckInSettings>(json ?? string.Empty, JsonOptions) ?? new CheckInSettings(); }
            catch (JsonException) { return new CheckInSettings(); }
        }
    }
}
