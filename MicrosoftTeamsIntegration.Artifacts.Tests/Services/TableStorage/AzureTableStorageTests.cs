using FakeItEasy;
using Microsoft.Azure.Cosmos.Table;
using MicrosoftTeamsIntegration.Artifacts.Services.TableStorage;

namespace MicrosoftTeamsIntegration.Artifacts.Tests.Services.TableStorage;

public class AzureTableStorageTests
{
    private readonly CloudTable _mockTable;
    private readonly AzureTableStorage _azureTableStorage;

    public AzureTableStorageTests()
    {
        var mockTableClient = A.Fake<CloudTableClient>(options =>
            options.WithArgumentsForConstructor([new Uri("http://localhost:10002/devstoreaccount1"), null, null]));
        _mockTable = A.Fake<CloudTable>(options =>
            options.WithArgumentsForConstructor([new Uri("http://localhost:10002/devstoreaccount1/testtable"), null]));
        A.CallTo(() => _mockTable.CreateIfNotExistsAsync(A<CancellationToken>._)).Returns(true);
        A.CallTo(() => mockTableClient.GetTableReference(A<string>._)).Returns(_mockTable);
        _azureTableStorage = new AzureTableStorage("testAppId", mockTableClient);
    }

    [Fact]
    public async Task Retrieve_ShouldReturnEntity()
    {
        // Arrange
        var partitionKey = "testPartition";
        var rowKey = "testRow";
        var expectedEntity = new TestEntity { PartitionKey = partitionKey, RowKey = rowKey };
        var tableResult = new TableResult { Result = expectedEntity };

        A.CallTo(() => _mockTable.ExecuteAsync(A<TableOperation>._, A<CancellationToken>._)).Returns(tableResult);

        // Act
        var result = await _azureTableStorage.Retrieve<TestEntity>(partitionKey, rowKey);

        // Assert
        Assert.Equal(expectedEntity, result);
    }

    [Fact]
    public async Task Insert_ShouldInsertEntity()
    {
        // Arrange
        var entity = new TestEntity { PartitionKey = "testPartition", RowKey = "testRow" };

        A.CallTo(() => _mockTable.ExecuteAsync(A<TableOperation>._, A<CancellationToken>._)).Returns(new TableResult());

        // Act
        await _azureTableStorage.Insert(entity);

        // Assert
        A.CallTo(() => _mockTable.ExecuteAsync(A<TableOperation>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Delete_ShouldDeleteEntity()
    {
        // Arrange
        var entity = new TestEntity { PartitionKey = "testPartition", RowKey = "testRow", ETag = "*" };

        A.CallTo(() => _mockTable.ExecuteAsync(A<TableOperation>._, A<CancellationToken>._)).Returns(new TableResult());

        // Act
        await _azureTableStorage.Delete(entity);

        // Assert
        A.CallTo(() => _mockTable.ExecuteAsync(A<TableOperation>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task InsertOrMerge_ShouldInsertOrMergeEntity()
    {
        // Arrange
        var entity = new TestEntity { PartitionKey = "testPartition", RowKey = "testRow" };

        A.CallTo(() => _mockTable.ExecuteAsync(A<TableOperation>._, A<CancellationToken>._)).Returns(new TableResult());

        // Act
        await _azureTableStorage.InsertOrMerge(entity);

        // Assert
        A.CallTo(() => _mockTable.ExecuteAsync(A<TableOperation>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task InsertOrReplace_ShouldInsertOrReplaceEntity()
    {
        // Arrange
        var entity = new TestEntity { PartitionKey = "testPartition", RowKey = "testRow" };

        A.CallTo(() => _mockTable.ExecuteAsync(A<TableOperation>._, A<CancellationToken>._)).Returns(new TableResult());

        // Act
        await _azureTableStorage.InsertOrReplace(entity);

        // Assert
        A.CallTo(() => _mockTable.ExecuteAsync(A<TableOperation>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Replace_ShouldReplaceEntity()
    {
        // Arrange
        var entity = new TestEntity { PartitionKey = "testPartition", RowKey = "testRow" };

        A.CallTo(() => _mockTable.ExecuteAsync(A<TableOperation>._, A<CancellationToken>._)).Returns(new TableResult());

        // Act
        await _azureTableStorage.Replace(entity);

        // Assert
        A.CallTo(() => _mockTable.ExecuteAsync(A<TableOperation>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    private class TestEntity : TableEntity
    {
    }
}
