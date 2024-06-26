using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace DocMigrator.Json;

/// <summary>
///     Base class for deserializing JSON documents with schema migrations.
/// </summary>
/// <typeparam name="T">The type to deserialize.</typeparam>
public abstract class JsonMigrationDeserializer<T> where T : class
{
    private readonly ILogger<JsonMigrationDeserializer<T>> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonMigrationDeserializer{T}" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="migrations">The list of migration functions.</param>
    /// <param name="documentOptions">The JSON document options.</param>
    /// <param name="nodeOptions">The JSON node options.</param>
    /// <param name="serializerOptions">The JSON serializer options.</param>
    protected JsonMigrationDeserializer(IServiceProvider serviceProvider,
        ILogger<JsonMigrationDeserializer<T>> logger,
        IReadOnlyList<Func<IServiceProvider, JsonObject, ValueTask>> migrations,
        JsonDocumentOptions documentOptions = default,
        JsonNodeOptions nodeOptions = default,
        JsonSerializerOptions? serializerOptions = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        DocumentOptions = documentOptions;
        NodeOptions = nodeOptions;
        SerializerOptions = serializerOptions;
        Migrations = migrations;
    }

    /// <summary>
    ///     Gets or sets the JSON document options.
    /// </summary>
    public JsonDocumentOptions DocumentOptions { get; set; }

    /// <summary>
    ///     Gets or sets the JSON node options.
    /// </summary>
    public JsonNodeOptions NodeOptions { get; set; }

    /// <summary>
    ///     Gets or sets the JSON serializer options.
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; set; }

    /// <summary>
    ///     Gets the application schema version.
    /// </summary>
    public int AppSchemaVersion => Migrations.Count;

    /// <summary>
    ///     Gets the list of migration functions.
    /// </summary>
    public IReadOnlyList<Func<IServiceProvider, JsonObject, ValueTask>> Migrations { get; }

    /// <summary>
    ///     Deserializes the specified JSON string and applies migrations.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="ValueTask{T}" /> representing the asynchronous operation.</returns>
    public ValueTask<T?> Deserialize(string json)
    {
        using var document = JsonDocument.Parse(json, DocumentOptions);
        var root = document.RootElement;

        return Deserialize(root);
    }

    /// <summary>
    ///     Deserializes the specified JSON element and applies migrations.
    /// </summary>
    /// <param name="root">The JSON element to deserialize.</param>
    /// <returns>A <see cref="ValueTask{T}" /> representing the asynchronous operation.</returns>
    public async ValueTask<T?> Deserialize(JsonElement root)
    {
        try
        {
            // Schema version defaults to 0
            var docSchemaVersion = 0;
            if (root.TryGetProperty("schema_version", out var schemaVersionElement))
                // Get current schema version
            {
                docSchemaVersion = schemaVersionElement.GetInt32();
            }

            if (docSchemaVersion >= AppSchemaVersion)
                // No migrations to be applied.
            {
                return root.Deserialize<T>(SerializerOptions);
            }

            // Convert JsonElement to JsonObject
            var jsonObject = JsonObject.Create(root, NodeOptions)!;

            // Apply migrations
            for (var i = docSchemaVersion; i < AppSchemaVersion; i++)
            {
                await Migrations[i](_serviceProvider, jsonObject);
            }

            // Update schema version
            jsonObject["schema_version"] = AppSchemaVersion;

            return jsonObject.Deserialize<T>(SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize {type}", nameof(T));
            return null;
        }
    }
}