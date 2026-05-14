using System.Text.Json.Serialization;

namespace Backend.Models
{
    public class VolumeCommand : BaseCommand
    {
        [JsonPropertyName("vol")]
        public float Vol { get; set; }
    }
}
