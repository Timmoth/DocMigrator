using System.Reflection;
using System.Text.Json;
using DocMigrator.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Json;

public class RemovePropertyMigrationTests
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

        var original = JsonSerializer.Serialize(new RemovePropertyMigrationClassV0
        {
            SchemaVersion = 0,
            Foo = "original-value"
        });

        // When
        var deserialized = await migrationDeserializer.Deserialize<RemovePropertyMigrationClassV1>(original);

        // Then
        if (deserialized == null)
        {
            Assert.Fail("Failed to deserialize");
        }

        deserialized.SchemaVersion.Should().Be(1);
        deserialized.Bar.Should().Be("original-value");
    }
}