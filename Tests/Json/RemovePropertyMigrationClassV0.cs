using System.Text.Json.Serialization;

namespace Tests.Json;

public class RemovePropertyMigrationClassV0
{
    [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; }

    [JsonPropertyName("foo")] public string Foo { get; set; } = string.Empty;
}