using DocMigrator.Bson;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Tests.Bson;

public class BsonMigrationClassDeserializer : BsonMigrationDeserializer<BsonMigrationClass>
{
    public BsonMigrationClassDeserializer(IServiceProvider serviceProvider,
        ILogger<BsonMigrationClassDeserializer> logger) :
        base(serviceProvider, logger, new List<Func<IServiceProvider, BsonDocument, ValueTask>>
        {
            ApplyMigration_1
        })
    {
    }


    public static ValueTask ApplyMigration_1(IServiceProvider serviceProvider, BsonDocument jsonObject)
    {
        jsonObject["foo"] = "foo-1";
        return ValueTask.CompletedTask;
    }
}