using Microsoft.Azure.Cosmos.Table;
using MicrosoftTeamsIntegration.Artifacts.Services.TableStorage;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Tests.Services.TableStorage;

public class AzureTableEntityAdapterTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var originalEntity = new TestEntity { Id = 1, Name = "Test" };
        var partitionKey = "testPartition";
        var rowKey = "testRow";

        // Act
        var adapter = new AzureTableEntityAdapter<TestEntity>(originalEntity, partitionKey, rowKey);

        // Assert
        Assert.Equal(originalEntity, adapter.OriginalEntity);
        Assert.Equal(partitionKey, adapter.PartitionKey);
        Assert.Equal(rowKey, adapter.RowKey);
    }

    [Fact]
    public void WriteEntity_ShouldSerializeOriginalEntity()
    {
        // Arrange
        var originalEntity = new TestEntity { Id = 1, Name = "Test" };
        var adapter = new AzureTableEntityAdapter<TestEntity>(originalEntity, "testPartition", "testRow");

        // Act
        var properties = adapter.WriteEntity(null!);

        // Assert
        Assert.True(properties.ContainsKey(nameof(AzureTableEntityAdapter<TestEntity>.OriginalEntity)));
        var serializedEntity = properties[nameof(AzureTableEntityAdapter<TestEntity>.OriginalEntity)].StringValue;
        var deserializedEntity = JsonConvert.DeserializeObject<TestEntity>(serializedEntity);
        Assert.Equal(originalEntity.Id, deserializedEntity!.Id);
        Assert.Equal(originalEntity.Name, deserializedEntity.Name);
    }

    [Fact]
    public void ReadEntity_ShouldDeserializeOriginalEntity()
    {
        // Arrange
        var originalEntity = new TestEntity { Id = 1, Name = "Test" };
        var serializedEntity = JsonConvert.SerializeObject(originalEntity);
        var properties = new Dictionary<string, EntityProperty>
        {
            {
                nameof(AzureTableEntityAdapter<TestEntity>.OriginalEntity),
                EntityProperty.GeneratePropertyForString(serializedEntity)
            }
        };
        var adapter = new AzureTableEntityAdapter<TestEntity>();

        // Act
        adapter.ReadEntity(properties, null!);

        // Assert
        Assert.Equal(originalEntity.Id, adapter.OriginalEntity!.Id);
        Assert.Equal(originalEntity.Name, adapter.OriginalEntity.Name);
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
