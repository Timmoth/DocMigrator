using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.Extensions.Logging;

namespace DocMigrator.Yaml;

/// <summary>
///   Base class for deserializing YAML documents with schema migrations.
/// </summary>
/// <typeparam name="T">
///   The type to deserialize.
/// </typeparam>
public abstract class YamlMigrationDeserializer<T> where T : class
{
    private readonly ILogger<YamlMigrationDeserializer<T>> _logger;
    private readonly DeserializerBuilder _deserializerBuilder;
    private readonly SerializerBuilder _serializerBuilder;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="YamlMigrationDeserializer{T}" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The service provider used when executing migrations.
    /// </param>
    /// <param name="logger">
    ///     The logger.
    /// </param>
    /// <param name="migrations">
    ///     The ordered list of migration functions. Each migration represents a schema version increment.
    /// </param>
    /// <param name="deserializerBuilder">
    ///     Optional deserializer builder configuration.
    /// </param>
    /// <param name="serializerBuilder">
    ///     Optional serializer builder configuration.
    /// </param>
    protected YamlMigrationDeserializer(
        IServiceProvider serviceProvider,
        ILogger<YamlMigrationDeserializer<T>> logger,
        IReadOnlyList<Func<IServiceProvider, Dictionary<object, object>, ValueTask>> migrations,
        DeserializerBuilder? deserializerBuilder = null,
        SerializerBuilder? serializerBuilder = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _deserializerBuilder = deserializerBuilder ?? new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties();
        _serializerBuilder = serializerBuilder ?? new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance);

        Migrations = migrations;
    }

    /// <summary>
    ///     Gets the list of migration functions.
    /// </summary>
    public IReadOnlyList<Func<IServiceProvider, Dictionary<object, object>, ValueTask>> Migrations { get; }

    /// <summary>
    ///     Gets the current application schema version.
    /// </summary>
    /// <remarks>
    ///     The application schema version is equal to the number of registered migrations.
    /// </remarks>
    public int AppSchemaVersion => Migrations.Count;

    /// <summary>
    ///     Deserializes the specified YAML string and applies any required migrations.
    /// </summary>
    /// <param name="yaml">
    ///     The YAML string to deserialize.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation,
    ///     containing the migrated and deserialized object, or <c>null</c> if deserialization fails.
    /// </returns>
    public async ValueTask<T?> Deserialize(string yaml)
    {
        var deserializer = _deserializerBuilder.Build();

        var obj = deserializer.Deserialize<Dictionary<object, object>>(yaml);

        if (obj is null)
        {
            obj = new Dictionary<object, object>();
        }

        try
        {
            // Schema version defaults to 0
            var docSchemaVersion = GetSchemaVersion(yaml);

            if (docSchemaVersion >= AppSchemaVersion)
            {
                // No migrations to be applied
                return ConvertTo(obj);
            }

            // Apply migrations
            for (var i = docSchemaVersion; i < AppSchemaVersion; i++)
            {
                await Migrations[i](_serviceProvider, obj);
            }

            // Update schema version
            obj["schemaVersion"] = AppSchemaVersion;

            return ConvertTo(obj);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize {type}", nameof(T));
            return null;
        }
    }

    /// <summary>
    ///     Converts a migrated YAML object graph into a strongly typed instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="migratedObject">
    ///     The migrated YAML object graph.
    /// </param>
    /// <returns>
    ///     An instance of <typeparamref name="T"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="migratedObject" /> is <c>null</c>.
    /// </exception>
    /// <exception cref="YamlDotNet.Core.YamlException">
    ///     Thrown when the object cannot be deserialized into <typeparamref name="T" />.
    /// </exception>
    public T ConvertTo(Dictionary<object, object> migratedObject)
    {
        if (migratedObject is null)
            throw new ArgumentNullException(nameof(migratedObject));

        var serializer = _serializerBuilder.Build();
        var deserializer = _deserializerBuilder.Build();

        var yaml = serializer.Serialize(migratedObject);

        return deserializer.Deserialize<T>(yaml);
    }

    /// <summary>
    ///     Gets the schema version defined in the specified YAML document.
    /// </summary>
    /// <param name="yaml">
    ///     The YAML string to inspect.
    /// </param>
    /// <returns>
    ///     The schema version defined in the document, or <c>0</c> if the field
    ///     is missing, invalid, or the YAML cannot be parsed.
    /// </returns>
    public int GetSchemaVersion(string yaml)
    {
        try
        {
            var deserializer = _deserializerBuilder.Build();

            var obj = deserializer.Deserialize<Dictionary<object, object>>(yaml);

            if (obj != null &&
                obj.TryGetValue("schemaVersion", out var value) &&
                value != null)
            {
                if (value is int i)
                    return i;

                if (value is string s && int.TryParse(s, out var parsed))
                    return parsed;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }
}