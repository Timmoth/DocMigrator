using System.Text.Json.Serialization;

namespace Benchmarks;

public class SingleMigrationClass
{
    [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; }

    [JsonPropertyName("foo")] public string Foo { get; set; } = string.Empty;
}