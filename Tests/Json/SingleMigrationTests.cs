using System.Reflection;
using System.Text.Json;
using DocMigrator.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Json;

public class SingleMigrationTests
{
    public JsonMigrator Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJsonMigrator(Assembly.GetExecutingAssembly());
        var scope = services.BuildServiceProvider().CreateScope();
        return scope.ServiceProvider.GetRequiredService<JsonMigrator>();
    }

    [Fact]
    public async Task MigrationDeserializer_Creates_Missing_Properties()
    {
        // Given
        var migrationDeserializer = Setup();

        // When
        var deserialized = await migrationDeserializer.Deserialize<SingleMigrationClass>("{}");

        // Then
        if (deserialized == null)
        {
            Assert.Fail("Failed to deserialize");
        }

        deserialized.SchemaVersion.Should().Be(1);
        deserialized.Foo.Should().Be("foo-1");
    }

    [Fact]
    public async Task MigrationDeserializer_Increments_SchemaVersion()
    {
        // Given
        var migrationDeserializer = Setup();

        var original = JsonSerializer.Serialize(new SingleMigrationClass
        {
            SchemaVersion = 0,
            Foo = "original-value"
        });

        // When
        var deserialized = await migrationDeserializer.Deserialize<SingleMigrationClass>(original);

        // Then
        if (deserialized == null)
        {
            Assert.Fail("Failed to deserialize");
        }

        deserialized.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public async Task MigrationDeserializer_Sets_NewValue()
    {
        // Given
        var migrationDeserializer = Setup();

        var original = JsonSerializer.Serialize(new SingleMigrationClass
        {
            SchemaVersion = 0,
            Foo = "original-value"
        });

        // When
        var deserialized = await migrationDeserializer.Deserialize<SingleMigrationClass>(original);

        // Then
        if (deserialized == null)
        {
            Assert.Fail("Failed to deserialize");
        }

        deserialized.Foo.Should().Be("foo-1");
    }

    [Fact]
    public async Task MigrationDeserializer_DoesNot_Apply_Old_Migration()
    {
        // Given
        var migrationDeserializer = Setup();

        var original = JsonSerializer.Serialize(new SingleMigrationClass
        {
            SchemaVersion = 1,
            Foo = "original-value"
        });

        // When
        var deserialized = await migrationDeserializer.Deserialize<SingleMigrationClass>(original);

        // Then
        if (deserialized == null)
        {
            Assert.Fail("Failed to deserialize");
        }

        deserialized.Foo.Should().Be("original-value");
    }
}