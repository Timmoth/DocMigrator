using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace DocMigrator.Json;

/// <summary>
///     Apply migrations on deserialization of a JSON document
/// </summary>
public class JsonMigrator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonMigrator" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public JsonMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    ///     Deserializes the specified JSON document into an instance of type <typeparamref name="T" /> and applies migrations.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <param name="document">The JSON document to deserialize.</param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation, containing the deserialized
    ///     object.
    /// </returns>
    public ValueTask<T?> Deserialize<T>(string document) where T : class
    {
        var migrator = _serviceProvider.GetRequiredService<JsonMigrationDeserializer<T>>();
        return migrator.Deserialize(document);
    }

    /// <summary>
    ///     Deserializes the specified JSON element into an instance of type <typeparamref name="T" /> and applies migrations.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <param name="root">The JSON element to deserialize.</param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation, containing the deserialized
    ///     object.
    /// </returns>
    public ValueTask<T?> Deserialize<T>(JsonElement root) where T : class
    {
        var migrator = _serviceProvider.GetRequiredService<JsonMigrationDeserializer<T>>();
        return migrator.Deserialize(root);
    }
}