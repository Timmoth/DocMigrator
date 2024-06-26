using System.Text.Json.Serialization;

namespace Tests.Json;

public class RemovePropertyMigrationClassV1
{
    [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; }

    [JsonPropertyName("bar")] public string Bar { get; set; } = string.Empty;
}