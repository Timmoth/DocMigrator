# DocMigrator ![Logo](assets/logo.png)
[![Json](https://img.shields.io/nuget/v/DocMigrator.Json?label=Json)](https://www.nuget.org/packages/DocMigrator.Json)
[![Bson](https://img.shields.io/nuget/v/DocMigrator.Bson?label=Bson)](https://www.nuget.org/packages/DocMigrator.Bson)

Zero downtime on-the-fly document migrations.

### Overview:
When working with a document database, it's common to store multiple document schemas simultaneously. Often, this is fine since you can develop your application code to be backward compatible with previous versions. However, sometimes writing backward-compatible code can become very messy and error prone, especially when dealing with many versions of a document.

An alternative solution is to apply a migration to the entire database at once, though this is not optimal since it can lead to downtime and performance issues during the migration. Enter DocMigrator, a simple yet high-performance package that enables you to migrate your documents on the fly as they are being deserialized, ensuring zero downtime.

### Bson Demo
```csharp
// Install the nuget package
dotnet add package DocMigrator.Bson

// Define document
public class User
{
    [BsonElement("schema_version")] public int SchemaVersion { get; set; }
    [BsonElement("full_name")] public string FullName { get; set; }
    [BsonElement("avatar_url")] public string AvatarUrl { get; set; }
}

// Create Migrators
public class UserMigrationDeserializer : BsonMigrationDeserializer<User>
{
    public UserMigrationDeserializer(IServiceProvider serviceProvider,
        ILogger<UserMigrationDeserializer> logger) :
        base(serviceProvider, logger, new List<Func<IServiceProvider, BsonDocument, ValueTask>>
        {
            ApplyMigration_1,
            ApplyMigration_2
        })
    {
    }

    public static ValueTask ApplyMigration_1(IServiceProvider serviceProvider, BsonDocument document)
    {
        document["full_name"] = $"{document["first_name"]} {document["last_name"]}";
        document.Remove("first_name");
        document.Remove("last_name");
        return ValueTask.CompletedTask;
    }

    public static async ValueTask ApplyMigration_2(IServiceProvider serviceProvider, BsonDocument document)
    {
        var avatarService = serviceProvider.GetRequiredService<AvatarService>();
        document["avatar_url"] = await avatarService.GetAvatarUrl(document["id"]);
    }
}

// Register your migrators
services.AddBsonMigrator(Assembly.GetExecutingAssembly());

```

### Json Demo
```csharp
// Install the nuget package
dotnet add package DocMigrator.Json

// Define document
public class User
{
    [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; }
    [JsonPropertyName("full_name")] public string FullName { get; set; }
    [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }
}

// Create Migrators
public class UserMigrationDeserializer : JsonMigrationDeserializer<User>
{
    public UserMigrationDeserializer(IServiceProvider serviceProvider,
        ILogger<UserMigrationDeserializer> logger) :
        base(serviceProvider, logger, new List<Func<IServiceProvider, JsonObject, ValueTask>>
        {
            ApplyMigration_1,
            ApplyMigration_2
        })
    {
    }

    public static ValueTask ApplyMigration_1(IServiceProvider serviceProvider, JsonObject document)
    {
        document["full_name"] = $"{document["first_name"]} {document["last_name"]}";
        return ValueTask.CompletedTask;
    }

    public static async ValueTask ApplyMigration_2(IServiceProvider serviceProvider, JsonObject document)
    {
        var avatarService = serviceProvider.GetRequiredService<AvatarService>();
        document["avatar_url"] = await avatarService.GetAvatarUrl(document["id"]);
    }
}

// Register your migrators
services.AddJsonMigrator(Assembly.GetExecutingAssembly());

```


### Support the project 🤝

- **🌟 Star this repository**: It means a lot to me and helps with exposure.
- **🪲 Report bugs**: Report any bugs you find by creating an issue.
- **📝 Contribute**: Read the [contribution guide](https://timmoth.github.io/DocMigrator/contributing) then pick up or create an issue.
