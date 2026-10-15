using System.Text.Json.Serialization;

namespace PurdueIo.Scraper.Models
{
    public record Term
    {
        [JsonPropertyName("code")]
        public string Id { get; init; }

        [JsonPropertyName("description")]
        public string Name { get; init; }
    }
}