using System.Text.Json.Nodes;
using DocMigrator.Json;
using Microsoft.Extensions.Logging;

namespace Tests.Json;

public class RemovePropertyMigrationClassDeserializer : JsonMigrationDeserializer<RemovePropertyMigrationClassV1>
{
    public RemovePropertyMigrationClassDeserializer(IServiceProvider serviceProvider,
        ILogger<RemovePropertyMigrationClassDeserializer> logger) :
        base(serviceProvider, logger, new List<Func<IServiceProvider, JsonObject, ValueTask>>
        {
            ApplyMigration_1
        })
    {
    }

    #region Migrations

    public static ValueTask ApplyMigration_1(IServiceProvider serviceProvider, JsonObject jsonObject)
    {
        jsonObject["bar"] = jsonObject["foo"]!.ToString();
        return ValueTask.CompletedTask;
    }

    #endregion
}