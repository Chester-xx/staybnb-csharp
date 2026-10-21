using System.Text.Json;

namespace staybnb_csharp.Models
{
    /// <summary>The host's check-in rules and required documents, stored as JSON in CheckInProcess.StepsJson.</summary>
    public class CheckInSettings
    {
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

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        public static CheckInSettings FromJson(string? json)
        {
            try
            {
                return JsonSerializer.Deserialize<CheckInSettings>(json ?? string.Empty, JsonOptions) ?? new CheckInSettings();
            }
            catch (JsonException)
            {
                return new CheckInSettings();
            }
        }
    }
}
