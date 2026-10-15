using System.Text.Json.Serialization;

namespace PurdueIo.Scraper.Models
{
    public record Subject
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("description")]
        public string Name { get; set; }
    }
}