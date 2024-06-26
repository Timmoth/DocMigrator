using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace DocMigrator.Bson;

/// <summary>
///     Base class for deserializing BSON documents with schema migrations.
/// </summary>
/// <typeparam name="T">The type of the document to deserialize.</typeparam>
public abstract class BsonMigrationDeserializer<T> where T : class
{
    private readonly ILogger<BsonMigrationDeserializer<T>> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BsonMigrationDeserializer{T}" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="migrations">The list of migration functions.</param>
    protected BsonMigrationDeserializer(IServiceProvider serviceProvider,
        ILogger<BsonMigrationDeserializer<T>> logger,
        IReadOnlyList<Func<IServiceProvider, BsonDocument, ValueTask>> migrations)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        Migrations = migrations;
    }

    /// <summary>
    ///     Gets the application schema version.
    /// </summary>
    public int AppSchemaVersion => Migrations.Count;

    /// <summary>
    ///     Gets the list of migration functions.
    /// </summary>
    public IReadOnlyList<Func<IServiceProvider, BsonDocument, ValueTask>> Migrations { get; }

    /// <summary>
    ///     Deserializes the BSON document and applies migrations.
    /// </summary>
    /// <param name="document">The BSON document to deserialize.</param>
    /// <returns>The deserialized document.</returns>
    public async ValueTask<T?> Deserialize(BsonDocument document)
    {
        try
        {
            // Schema version defaults to 0
            var docSchemaVersion = 0;
            if (document.TryGetValue("schema_version", out var schemaVersionElement))
                // Get current schema version
            {
                docSchemaVersion = schemaVersionElement.AsInt32;
            }

            if (docSchemaVersion >= AppSchemaVersion)
                // No migrations to be applied.
            {
                return BsonSerializer.Deserialize<T>(document);
            }

            // Apply migrations
            for (var i = docSchemaVersion; i < AppSchemaVersion; i++)
            {
                await Migrations[i](_serviceProvider, document);
            }

            // Update schema version
            document["schema_version"] = AppSchemaVersion;
            return BsonSerializer.Deserialize<T>(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize {type}", nameof(T));
            return null;
        }
    }
}