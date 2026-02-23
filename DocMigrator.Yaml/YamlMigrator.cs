using YamlDotNet.Core;
using Microsoft.Extensions.DependencyInjection;

namespace DocMigrator.Yaml;

/// <summary>
///   Applies registered migrations when deserializing YAML documents.
/// </summary>
public class YamlMigrator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="YamlMigrator" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The service provider used to resolve <see cref="YamlMigrationDeserializer{T}" /> instances.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="serviceProvider" /> is <c>null</c>.
    /// </exception>
    public YamlMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider 
            ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    ///     Deserializes the specified YAML document into an instance of type
    ///     <typeparamref name="T" /> and applies any required schema migrations.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the object to deserialize.
    /// </typeparam>
    /// <param name="document">
    ///     The YAML document to deserialize. If no <c>schemaVersion</c> is present,
    ///     version <c>0</c> is assumed.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation,
    ///     containing the migrated and deserialized object, or <c>null</c> if deserialization fails.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if no <see cref="YamlMigrationDeserializer{T}" /> is registered.
    /// </exception>
    public ValueTask<T?> Deserialize<T>(string document) where T : class
    {
        var migrator = _serviceProvider.GetRequiredService<YamlMigrationDeserializer<T>>();
        return migrator.Deserialize(document);
    }

    /// <summary>
    ///     Gets the schema version defined in the specified YAML document.
    /// </summary>
    /// <typeparam name="T">
    ///     The type associated with the migration deserializer.
    /// </typeparam>
    /// <param name="document">
    ///     The YAML document to inspect.
    /// </param>
    /// <returns>
    ///     The schema version defined in the document, or <c>0</c> if the field
    ///     is missing, invalid, or the document cannot be parsed.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if no <see cref="YamlMigrationDeserializer{T}" /> is registered.
    /// </exception>
    public int GetSchemaVersion<T>(string document) where T : class
    {
        var migrator = _serviceProvider.GetRequiredService<YamlMigrationDeserializer<T>>();
        return migrator.GetSchemaVersion(document);
    }
}