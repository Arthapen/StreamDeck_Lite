using System.Text.Json.Serialization;

namespace Backend.Models
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "comando")]
    [JsonDerivedType(typeof(VolumeCommand), typeDiscriminator: "volumen")]
    [JsonDerivedType(typeof(RouteCommand), typeDiscriminator: "cambiar_ruta")]
    public abstract class BaseCommand
    {
        [JsonPropertyName("app")]
        public string App { get; set; } = string.Empty;
    }
}
