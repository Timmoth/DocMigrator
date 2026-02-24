using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.Extensions.Logging;

namespace DocMigrator.Yaml;

/// <summary>
/// Base class for deserializing YAML documents with schema migrations.
/// </summary>
/// <typeparam name="T">The target type.</typeparam>
public abstract class YamlMigrationDeserializer<T> where T : class
{
    private readonly ILogger<YamlMigrationDeserializer<T>> _logger;
    private readonly DeserializerBuilder _deserializerBuilder;
    private readonly SerializerBuilder _serializerBuilder;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The YAML property name that stores the schema version.
    /// </summary>
    private readonly string _schemaVersionPropertyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlMigrationDeserializer{T}"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider used by migrations.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="migrations">Ordered migration steps.</param>
    /// <param name="schemaVersionPropertyName">
    /// The YAML property name that represents the schema version.
    /// </param>
    /// <param name="deserializerBuilder">Optional deserializer configuration.</param>
    /// <param name="serializerBuilder">Optional serializer configuration.</param>
    protected YamlMigrationDeserializer(
        IServiceProvider serviceProvider,
        ILogger<YamlMigrationDeserializer<T>> logger,
        IReadOnlyList<Func<IServiceProvider, Dictionary<object, object>, ValueTask>> migrations,
        string schemaVersionPropertyName,
        DeserializerBuilder? deserializerBuilder = null,
        SerializerBuilder? serializerBuilder = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _schemaVersionPropertyName = schemaVersionPropertyName 
            ?? throw new ArgumentNullException(nameof(schemaVersionPropertyName));

        _deserializerBuilder = deserializerBuilder ?? new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties();

        _serializerBuilder = serializerBuilder ?? new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance);

        Migrations = migrations;
    }

    /// <summary>
    /// Gets the registered migration steps.
    /// </summary>
    public IReadOnlyList<Func<IServiceProvider, Dictionary<object, object>, ValueTask>> Migrations { get; }

    /// <summary>
    /// Gets the current application schema version.
    /// </summary>
    public int AppSchemaVersion => Migrations.Count;

    /// <summary>
    /// Deserializes YAML and applies required migrations.
    /// </summary>
    /// <param name="yaml">The YAML content.</param>
    /// <returns>The migrated object or <c>null</c> on failure.</returns>
    public async ValueTask<T?> Deserialize(string yaml)
    {
        var deserializer = _deserializerBuilder.Build();
        var obj = deserializer.Deserialize<Dictionary<object, object>>(yaml)
                  ?? new Dictionary<object, object>();
        
        try
        {
            var docSchemaVersion = GetSchemaVersion(yaml);

            
            if (docSchemaVersion < AppSchemaVersion)
            {
                for (var i = docSchemaVersion; i < AppSchemaVersion; i++)
                {
                    await Migrations[i](_serviceProvider, obj);
                }

                obj[_schemaVersionPropertyName] = AppSchemaVersion;
            }

            return ConvertTo(obj);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize {Type}", typeof(T).Name);
            return null;
        }
    }

    /// <summary>
    /// Converts a migrated object graph into <typeparamref name="T"/>.
    /// </summary>
    /// <param name="migratedObject">The migrated dictionary.</param>
    /// <returns>A strongly typed instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="migratedObject"/> is null.
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
    /// Reads the schema version from YAML.
    /// </summary>
    /// <param name="yaml">The YAML content.</param>
    /// <returns>The parsed schema version or 0 if missing/invalid.</returns>
    public int GetSchemaVersion(string yaml)
    {
        try
        {
            var deserializer = _deserializerBuilder.Build();
            var obj = deserializer.Deserialize<Dictionary<object, object>>(yaml);

            if (obj != null &&
                obj.TryGetValue(_schemaVersionPropertyName, out var value) &&
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