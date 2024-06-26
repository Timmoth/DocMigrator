using System.Text.Json.Nodes;
using DocMigrator.Json;
using Microsoft.Extensions.Logging;

namespace Tests.Json;

public class SingleMigrationClassDeserializer : JsonMigrationDeserializer<SingleMigrationClass>
{
    public SingleMigrationClassDeserializer(IServiceProvider serviceProvider,
        ILogger<SingleMigrationClassDeserializer> logger) :
        base(serviceProvider, logger, new List<Func<IServiceProvider, JsonObject, ValueTask>>
        {
            ApplyMigration_1
        })
    {
    }

    #region Migrations

    public static ValueTask ApplyMigration_1(IServiceProvider serviceProvider, JsonObject jsonObject)
    {
        jsonObject["foo"] = "foo-1";
        return ValueTask.CompletedTask;
    }

    #endregion
}