using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace DocMigrator.Bson;

/// <summary>
///     Apply migrations on deserialization of a BSON document
/// </summary>
public class BsonMigrator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BsonMigrator" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public BsonMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    ///     Deserializes the specified BSON document into an instance of type <typeparamref name="T" /> and applies migrations.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the document to.</typeparam>
    /// <param name="document">The BSON document to deserialize.</param>
    /// <returns>A <see cref="ValueTask{T}" /> representing the asynchronous operation with the deserialized object.</returns>
    public ValueTask<T?> Deserialize<T>(BsonDocument document) where T : class
    {
        var migrator = _serviceProvider.GetRequiredService<BsonMigrationDeserializer<T>>();
        return migrator.Deserialize(document);
    }
}