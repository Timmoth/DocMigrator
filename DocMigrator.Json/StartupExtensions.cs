using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DocMigrator.Json;

/// <summary>
///     Extension methods for registering JSON migration deserializers.
/// </summary>
public static class StartupExtensions
{
    /// <summary>
    ///     Registers JSON migration deserializers from the specified assembly.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to register the deserializers with.</param>
    /// <param name="assembly">The assembly containing the deserializers.</param>
    public static void AddJsonMigrator(this IServiceCollection services, Assembly assembly)
    {
        var baseType = typeof(JsonMigrationDeserializer<>);

        // Find all migration deserializers
        var types = assembly.GetTypes()
            .Where(t => t.BaseType is { IsGenericType: true }
                        && t.BaseType.GetGenericTypeDefinition() == baseType)
            .ToList();

        foreach (var consumerType in types)
        {
            // Get the generic argument
            var payloadType = consumerType.BaseType!.GetGenericArguments()[0];

            // Create the closed generic type
            var closedGenericType = baseType.MakeGenericType(payloadType);

            // Register the service
            services.AddScoped(closedGenericType, consumerType);
        }

        services.AddScoped<JsonMigrator>();
    }
}