using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using NSubstitute;
using Oproto.FluentDynamoDb.Requests;
using Oproto.FluentDynamoDb.Requests.Interfaces;
using Oproto.FluentDynamoDb.Storage;

namespace Oproto.FluentDynamoDb.UnitTests.Integration;

/// <summary>
/// Integration tests for DynamoDB operations using mock client to verify end-to-end flows
/// </summary>
public class DynamoDbOperationsIntegrationTests
{
    private readonly IAmazonDynamoDB _mockClient;
    private readonly TestTable _table;

    public DynamoDbOperationsIntegrationTests()
    {
        _mockClient = Substitute.For<IAmazonDynamoDB>();
        _table = new TestTable(_mockClient);
    }

    public class TestTable : DynamoDbTableBase
    {
        public TestTable(IAmazonDynamoDB client) : base(client, "TestTable")
        {
        }
    }

    [Fact]
    public async Task DeleteItem_EndToEndFlow_ExecutesSuccessfully()
    {
        // Arrange
        var expectedResponse = new DeleteItemResponse
        {
            Attributes = new Dictionary<string, AttributeValue>
            {
                { "pk", new AttributeValue { S = "test-key" } },
                { "data", new AttributeValue { S = "test-data" } }
            },
            ConsumedCapacity = new ConsumedCapacity { TableName = "TestTable", CapacityUnits = 1.0 }
        };

        _mockClient.DeleteItemAsync(Arg.Any<DeleteItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var response = await _table.Delete
            .WithKey("pk", "test-key")
            .Where("attribute_exists(#data)")
            .WithAttribute("#data", "data")
            .ReturnAllOldValues()
            .ReturnTotalConsumedCapacity()
            .ExecuteAsync();

        // Assert
        response.Should().NotBeNull();
        response.Attributes.Should().ContainKey("pk");
        response.ConsumedCapacity.Should().NotBeNull();

        // Verify the request was built correctly
        await _mockClient.Received(1).DeleteItemAsync(
            Arg.Is<DeleteItemRequest>(req =>
                req.TableName == "TestTable" &&
                req.Key.ContainsKey("pk") &&
                req.Key["pk"].S == "test-key" &&
                req.ConditionExpression == "attribute_exists(#data)" &&
                req.ExpressionAttributeNames.ContainsKey("#data") &&
                req.ExpressionAttributeNames["#data"] == "data" &&
                req.ReturnValues == ReturnValue.ALL_OLD &&
                req.ReturnConsumedCapacity == ReturnConsumedCapacity.TOTAL
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ScanOperation_ThroughScannableInterface_ExecutesSuccessfully()
    {
        // Arrange
        var expectedResponse = new ScanResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new() { { "pk", new AttributeValue { S = "item1" } }, { "data", new AttributeValue { S = "value1" } } },
                new() { { "pk", new AttributeValue { S = "item2" } }, { "data", new AttributeValue { S = "value2" } } }
            },
            Count = 2,
            ScannedCount = 2,
            ConsumedCapacity = new ConsumedCapacity { TableName = "TestTable", CapacityUnits = 2.0 }
        };

        _mockClient.ScanAsync(Arg.Any<ScanRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var scannableTable = _table.AsScannable();
        var response = await scannableTable.Scan
            .WithFilter("attribute_exists(#data)")
            .WithAttribute("#data", "data")
            .WithProjection("pk, #data")
            .Take(10)
            .ReturnTotalConsumedCapacity()
            .ExecuteAsync();

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().HaveCount(2);
        response.ConsumedCapacity.Should().NotBeNull();

        // Verify the request was built correctly
        await _mockClient.Received(1).ScanAsync(
            Arg.Is<ScanRequest>(req =>
                req.TableName == "TestTable" &&
                req.FilterExpression == "attribute_exists(#data)" &&
                req.ExpressionAttributeNames.ContainsKey("#data") &&
                req.ExpressionAttributeNames["#data"] == "data" &&
                req.ProjectionExpression == "pk, #data" &&
                req.Limit == 10 &&
                req.ReturnConsumedCapacity == ReturnConsumedCapacity.TOTAL
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void ScannableTable_PassThroughOperations_WorkCorrectly()
    {
        // Arrange
        var scannableTable = _table.AsScannable();

        // Act & Assert - Verify all pass-through operations work
        scannableTable.Get.Should().NotBeNull();
        scannableTable.Put.Should().NotBeNull();
        scannableTable.Update.Should().NotBeNull();
        scannableTable.Query.Should().NotBeNull();
        scannableTable.Delete.Should().NotBeNull();
        
        // Verify properties are passed through correctly
        scannableTable.Name.Should().Be("TestTable");
        scannableTable.DynamoDbClient.Should().Be(_mockClient);
        scannableTable.UnderlyingTable.Should().Be(_table);

        // Verify builders are configured with correct table name
        scannableTable.Get.ToGetItemRequest().TableName.Should().Be("TestTable");
        scannableTable.Put.ToPutItemRequest().TableName.Should().Be("TestTable");
        scannableTable.Update.ToUpdateItemRequest().TableName.Should().Be("TestTable");
        scannableTable.Query.ToQueryRequest().TableName.Should().Be("TestTable");
        scannableTable.Delete.ToDeleteItemRequest().TableName.Should().Be("TestTable");
        scannableTable.Scan.ToScanRequest().TableName.Should().Be("TestTable");
    }

    [Fact]
    public async Task BatchGetItem_MultipleTablesAndKeys_ExecutesSuccessfully()
    {
        // Arrange
        var expectedResponse = new BatchGetItemResponse
        {
            Responses = new Dictionary<string, List<Dictionary<string, AttributeValue>>>
            {
                {
                    "TestTable", new List<Dictionary<string, AttributeValue>>
                    {
                        new() { { "pk", new AttributeValue { S = "key1" } }, { "data", new AttributeValue { S = "value1" } } },
                        new() { { "pk", new AttributeValue { S = "key2" } }, { "data", new AttributeValue { S = "value2" } } }
                    }
                },
                {
                    "OtherTable", new List<Dictionary<string, AttributeValue>>
                    {
                        new() { { "id", new AttributeValue { S = "other1" } }, { "name", new AttributeValue { S = "Other Item" } } }
                    }
                }
            },
            ConsumedCapacity = new List<ConsumedCapacity>
            {
                new() { TableName = "TestTable", CapacityUnits = 2.0 },
                new() { TableName = "OtherTable", CapacityUnits = 1.0 }
            }
        };

        _mockClient.BatchGetItemAsync(Arg.Any<BatchGetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var batchBuilder = new BatchGetItemRequestBuilder(_mockClient);
        var response = await batchBuilder
            .GetFromTable("TestTable", builder => builder
                .WithKey("pk", "key1")
                .WithKey("pk", "key2")
                .WithProjection("pk, #data")
                .WithAttribute("#data", "data")
                .UsingConsistentRead())
            .GetFromTable("OtherTable", builder => builder
                .WithKey("id", "other1")
                .WithProjection("id, #name")
                .WithAttribute("#name", "name"))
            .ReturnConsumedCapacity(ReturnConsumedCapacity.TOTAL)
            .ExecuteAsync();

        // Assert
        response.Should().NotBeNull();
        response.Responses.Should().HaveCount(2);
        response.Responses["TestTable"].Should().HaveCount(2);
        response.Responses["OtherTable"].Should().HaveCount(1);
        response.ConsumedCapacity.Should().HaveCount(2);

        // Verify the request was built correctly
        await _mockClient.Received(1).BatchGetItemAsync(
            Arg.Is<BatchGetItemRequest>(req =>
                req.RequestItems.ContainsKey("TestTable") &&
                req.RequestItems.ContainsKey("OtherTable") &&
                req.RequestItems["TestTable"].Keys.Count == 2 &&
                req.RequestItems["OtherTable"].Keys.Count == 1 &&
                req.ReturnConsumedCapacity == ReturnConsumedCapacity.TOTAL
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task BatchWriteItem_MixedOperations_ExecutesSuccessfully()
    {
        // Arrange
        var expectedResponse = new BatchWriteItemResponse
        {
            ConsumedCapacity = new List<ConsumedCapacity>
            {
                new() { TableName = "TestTable", CapacityUnits = 3.0 }
            },
            ItemCollectionMetrics = new Dictionary<string, List<ItemCollectionMetrics>>
            {
                {
                    "TestTable", new List<ItemCollectionMetrics>
                    {
                        new() { ItemCollectionKey = new Dictionary<string, AttributeValue> { { "pk", new AttributeValue { S = "key1" } } } }
                    }
                }
            }
        };

        _mockClient.BatchWriteItemAsync(Arg.Any<BatchWriteItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var batchBuilder = new BatchWriteItemRequestBuilder(_mockClient);
        var response = await batchBuilder
            .WriteToTable("TestTable", builder => builder
                .PutItem(new Dictionary<string, AttributeValue>
                {
                    { "pk", new AttributeValue { S = "key1" } },
                    { "data", new AttributeValue { S = "new-value" } }
                })
                .PutItem(new Dictionary<string, AttributeValue>
                {
                    { "pk", new AttributeValue { S = "key2" } },
                    { "data", new AttributeValue { S = "another-value" } }
                })
                .DeleteItem("pk", "key3"))
            .ReturnConsumedCapacity(ReturnConsumedCapacity.TOTAL)
            .ReturnItemCollectionMetrics()
            .ExecuteAsync();

        // Assert
        response.Should().NotBeNull();
        response.ConsumedCapacity.Should().HaveCount(1);
        response.ItemCollectionMetrics.Should().ContainKey("TestTable");

        // Verify the request was built correctly
        await _mockClient.Received(1).BatchWriteItemAsync(
            Arg.Is<BatchWriteItemRequest>(req =>
                req.RequestItems.ContainsKey("TestTable") &&
                req.RequestItems["TestTable"].Count == 3 && // 2 puts + 1 delete
                req.ReturnConsumedCapacity == ReturnConsumedCapacity.TOTAL &&
                req.ReturnItemCollectionMetrics == ReturnItemCollectionMetrics.SIZE
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void AllOperations_FollowConsistentPatterns_InterfaceCompliance()
    {
        // Test that all operations follow consistent patterns
        
        // Delete operation should implement required interfaces
        var deleteBuilder = _table.Delete;
        deleteBuilder.Should().BeAssignableTo<IWithKey<DeleteItemRequestBuilder>>();
        deleteBuilder.Should().BeAssignableTo<IWithConditionExpression<DeleteItemRequestBuilder>>();
        deleteBuilder.Should().BeAssignableTo<IWithAttributeNames<DeleteItemRequestBuilder>>();
        deleteBuilder.Should().BeAssignableTo<IWithAttributeValues<DeleteItemRequestBuilder>>();

        // Scan operation should implement required interfaces
        var scanBuilder = _table.AsScannable().Scan;
        scanBuilder.Should().BeAssignableTo<IWithAttributeNames<ScanRequestBuilder>>();
        scanBuilder.Should().BeAssignableTo<IWithAttributeValues<ScanRequestBuilder>>();

        // Batch operations should be available
        var batchGetBuilder = new BatchGetItemRequestBuilder(_mockClient);
        var batchWriteBuilder = new BatchWriteItemRequestBuilder(_mockClient);
        
        batchGetBuilder.Should().NotBeNull();
        batchWriteBuilder.Should().NotBeNull();

        // All builders should have ExecuteAsync and ToRequest methods - verify they exist by calling them
        deleteBuilder.ToDeleteItemRequest().Should().NotBeNull();
        scanBuilder.ToScanRequest().Should().NotBeNull();
        batchGetBuilder.ToBatchGetItemRequest().Should().NotBeNull();
        batchWriteBuilder.ToBatchWriteItemRequest().Should().NotBeNull();
        
        // Verify async methods exist by checking they return Task
        var deleteTask = deleteBuilder.ExecuteAsync();
        var scanTask = scanBuilder.ExecuteAsync();
        var batchGetTask = batchGetBuilder.ExecuteAsync();
        var batchWriteTask = batchWriteBuilder.ExecuteAsync();
        
        deleteTask.Should().NotBeNull();
        scanTask.Should().NotBeNull();
        batchGetTask.Should().NotBeNull();
        batchWriteTask.Should().NotBeNull();
    }

    [Fact]
    public async Task ScanOperation_ParallelScanSupport_ExecutesSuccessfully()
    {
        // Arrange
        var expectedResponse = new ScanResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new() { { "pk", new AttributeValue { S = "segment-item1" } } }
            },
            Count = 1,
            ScannedCount = 1
        };

        _mockClient.ScanAsync(Arg.Any<ScanRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act - Test parallel scan functionality
        var response = await _table.AsScannable().Scan
            .WithSegment(0, 4) // Scan segment 0 of 4 total segments
            .Take(100)
            .ExecuteAsync();

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().HaveCount(1);

        // Verify parallel scan parameters were set correctly
        await _mockClient.Received(1).ScanAsync(
            Arg.Is<ScanRequest>(req =>
                req.TableName == "TestTable" &&
                req.Segment == 0 &&
                req.TotalSegments == 4 &&
                req.Limit == 100
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void ScanAccess_OnlyThroughScannableInterface_IntentionalFriction()
    {
        // Verify that scan is NOT directly accessible from the base table
        var table = _table;
        
        // This should not compile - scan should not be directly accessible
        // table.Scan // This property should not exist
        
        // Scan should only be accessible through AsScannable()
        var scannableTable = table.AsScannable();
        scannableTable.Scan.Should().NotBeNull();
        
        // Verify the intentional friction pattern is working
        scannableTable.Should().BeAssignableTo<IScannableDynamoDbTable>();
        scannableTable.Should().BeAssignableTo<IDynamoDbTable>();
    }
}