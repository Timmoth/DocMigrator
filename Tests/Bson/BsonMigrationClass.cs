using MongoDB.Bson.Serialization.Attributes;

namespace Tests.Bson;

public class BsonMigrationClass
{
    [BsonElement("schema_version")] public int SchemaVersion { get; set; }

    [BsonElement("foo")] public string Foo { get; set; } = string.Empty;
}