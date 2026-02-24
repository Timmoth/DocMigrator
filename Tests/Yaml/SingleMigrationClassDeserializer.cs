using YamlDotNet.Serialization;
using DocMigrator.Yaml;
using Microsoft.Extensions.Logging;

namespace Tests.Yaml;

public class SingleMigrationClassDeserializer : YamlMigrationDeserializer<SingleMigrationClass>
{
  public SingleMigrationClassDeserializer(IServiceProvider serviceProvider,
      ILogger<SingleMigrationClassDeserializer> logger) :
        base(serviceProvider, logger, new List<Func<IServiceProvider, Dictionary<object,object>, ValueTask>>
      {
        ApplyMigration_1,
        ApplyMigration_2
      }, "schemaVersion")
  {
  }

  #region Migrations

  public static ValueTask ApplyMigration_1(IServiceProvider serviceProvider, Dictionary<object,object> obj)
  {
    obj["foo"] = "foo-1";
    return ValueTask.CompletedTask;
  }

  public static ValueTask ApplyMigration_2(IServiceProvider serviceProvider, Dictionary<object,object> obj)
  {
    const string key = "runsOn";
    if (obj.ContainsKey(key))
    {
      var runsOn = obj[key];
      Type t = runsOn.GetType();
      switch (runsOn)
      {
        case string r:
          obj[key] = new List<string>{r};
          break;
        case List<string> r:
          // Nothing to do
          break;
        default:
          throw new InvalidCastException($"Cannot convert from {t} to List<string>");
      }
    }
    return ValueTask.CompletedTask;
  }

  #endregion
}
