namespace Tests.Yaml;

using DocMigrator.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

public class MigrationTests
{
    private static TestDeserializer Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var provider = services.BuildServiceProvider();

        return new TestDeserializer(
            provider,
            provider.GetRequiredService<ILogger<TestDeserializer>>());
    }

    #region Tests

    [Fact]
    public async Task Creates_Missing_Properties()
    {
        var sut = Setup();

        var result = await sut.Deserialize("");

        result.ShouldNotBeNull();
        result.SchemaVersion.ShouldBe(2);
        result.Foo.ShouldBe("foo-1");
    }

    [Fact]
    public async Task Increments_SchemaVersion()
    {
        var sut = Setup();

        var yaml = """
                   schemaVersion: 0
                   foo: original-value
                   """;

        var result = await sut.Deserialize(yaml);

        result.ShouldNotBeNull();
        result.SchemaVersion.ShouldBe(2);
    }

    [Fact]
    public async Task Sets_NewValue()
    {
        var sut = Setup();

        var yaml = """
                   schemaVersion: 0
                   foo: original-value
                   """;

        var result = await sut.Deserialize(yaml);

        result.ShouldNotBeNull();
        result.Foo.ShouldBe("foo-1");
    }

    [Fact]
    public async Task Does_Not_Apply_Old_Migration()
    {
        var sut = Setup();

        var yaml = """
                   schemaVersion: 1
                   foo: original-value
                   """;

        var result = await sut.Deserialize(yaml);

        result.ShouldNotBeNull();
        result.Foo.ShouldBe("original-value");
    }

    [Fact]
    public async Task Changes_Type_When_RunsOn_Is_String()
    {
        var sut = Setup();

        var yaml = """
                   schemaVersion: 1
                   foo: original-value
                   runsOn: host1
                   """;

        var result = await sut.Deserialize(yaml);

        result.ShouldNotBeNull();
        result.RunsOn.ShouldBe(new List<string> { "host1" });
    }

    [Fact]
    public async Task Ignores_Extra_Fields()
    {
        var sut = Setup();

        var yaml = """
                   schemaVersion: 1
                   foo: original-value
                   runsOn: host1
                   doesNotExist: blah
                   """;

        var result = await sut.Deserialize(yaml);

        result.ShouldNotBeNull();
        result.RunsOn.ShouldBe(new List<string> { "host1" });
    }

    [Fact]
    public async Task Defaults_To_Zero_When_SchemaVersion_Missing()
    {
        var sut = Setup();

        var yaml = """
                   foo: original-value
                   """;

        var result = await sut.Deserialize(yaml);

        result.ShouldNotBeNull();
        result.SchemaVersion.ShouldBe(2);
    }

    [Fact]
    public async Task Parses_String_SchemaVersion()
    {
        var sut = Setup();

        var yaml = """
                   schemaVersion: "0"
                   foo: original-value
                   """;

        var result = await sut.Deserialize(yaml);

        result.ShouldNotBeNull();
        result.SchemaVersion.ShouldBe(2);
    }

    [Fact]
    public async Task Invalid_String_SchemaVersion_Defaults_To_Zero()
    {
        var sut = Setup();

        var yaml = """
                   schemaVersion: "not-a-number"
                   foo: original-value
                   """;

        var result = await sut.Deserialize(yaml);

        result.ShouldNotBeNull();
        result.SchemaVersion.ShouldBe(2);
    }

    [Fact]
    public async Task Negative_SchemaVersion_Does_Not_Crash()
    {
        var sut = Setup();

        var yaml = """
                   schemaVersion: -1
                   foo: original-value
                   """;

        var result = await sut.Deserialize(yaml);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Higher_SchemaVersion_Does_Not_Migrate()
    {
        var sut = Setup();

        var yaml = """
                   schemaVersion: 999
                   foo: original-value
                   """;

        var result = await sut.Deserialize(yaml);

        result.ShouldNotBeNull();
        result.SchemaVersion.ShouldBe(999);
        result.Foo.ShouldBe("original-value");
    }

    [Fact]
    public void GetSchemaVersion_Returns_Zero_When_Invalid_Yaml()
    {
        var sut = Setup();

        var version = sut.GetSchemaVersion(":::: invalid yaml :::");

        version.ShouldBe(0);
    }

    [Fact]
    public void GetSchemaVersion_Returns_Zero_When_Field_Not_Present()
    {
        var sut = Setup();

        var version = sut.GetSchemaVersion("foo: bar");

        version.ShouldBe(0);
    }

    [Fact]
    public void GetSchemaVersion_Returns_Int_Value()
    {
        var sut = Setup();

        var version = sut.GetSchemaVersion("schemaVersion: 5");

        version.ShouldBe(5);
    }

    #endregion

    #region Supporting Test Types

    private sealed class TestDeserializer : YamlMigrationDeserializer<TestModel>
    {
        public TestDeserializer(
            IServiceProvider serviceProvider,
            ILogger<TestDeserializer> logger)
            : base(
                serviceProvider,
                logger,
                new List<Func<IServiceProvider, Dictionary<object, object>, ValueTask>>
                {
                    Migration1,
                    Migration2
                },
                "schemaVersion")
        {
        }

        private static ValueTask Migration1(IServiceProvider _, Dictionary<object, object> obj)
        {
            obj["foo"] = "foo-1";
            return ValueTask.CompletedTask;
        }

        private static ValueTask Migration2(IServiceProvider _, Dictionary<object, object> obj)
        {
            const string key = "runsOn";

            if (!obj.ContainsKey(key))
                return ValueTask.CompletedTask;

            var value = obj[key];

            switch (value)
            {
                case string s:
                    obj[key] = new List<string> { s };
                    break;

                case List<string>:
                    break;

                default:
                    throw new InvalidCastException($"Cannot convert {value?.GetType()} to List<string>");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestModel
    {
        public int SchemaVersion { get; set; }
        public string Foo { get; set; } = string.Empty;
        public List<string> RunsOn { get; set; } = new();
    }

    #endregion
}