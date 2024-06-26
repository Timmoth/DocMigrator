using System.Reflection;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using DocMigrator.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks;

[MemoryDiagnoser]
public class Benchmarks
{
    private readonly string _noMigrationJson = "{ \"schema_version\": 1, \"foo\": \"bar\"  }";
    private readonly string _singleMigrationJson = "{ \"schema_version\": 0, \"foo\": \"bar\"  }";
    private JsonMigrator _migrator = default!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJsonMigrator(Assembly.GetExecutingAssembly());
        var scope = services.BuildServiceProvider().CreateScope();
        _migrator = scope.ServiceProvider.GetRequiredService<JsonMigrator>();
    }

    [Benchmark]
    public async ValueTask MigrationSerializer_NoMigration()
    {
        var model = await _migrator.Deserialize<SingleMigrationClass>(_noMigrationJson);
    }

    [Benchmark]
    public async ValueTask JsonSerializer_NoMigration()
    {
        var model = JsonSerializer.Deserialize<SingleMigrationClass>(_noMigrationJson);
    }

    [Benchmark]
    public async ValueTask MigrationSerializer_SingleMigration()
    {
        var model = await _migrator.Deserialize<SingleMigrationClass>(_singleMigrationJson);
    }

    [Benchmark]
    public async ValueTask JsonSerializer_SingleMigration()
    {
        var model = JsonSerializer.Deserialize<SingleMigrationClass>(_singleMigrationJson);
        model.Foo = "foo-1";
        model.SchemaVersion = 1;
    }
}