using System.Text.Json.Serialization;

namespace Backend.Models
{
    public class RouteCommand : BaseCommand
    {
        [JsonPropertyName("dispositivo_id")]
        public string DispositivoId { get; set; } = string.Empty;
    }
}
