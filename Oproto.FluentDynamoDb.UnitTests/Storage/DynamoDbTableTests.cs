using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using FluentAssertions;
using NSubstitute;
using Oproto.FluentDynamoDb.Requests;
using Oproto.FluentDynamoDb.Storage;

namespace Oproto.FluentDynamoDb.UnitTests.Storage;

public class DynamoDbTableTests
{
    public class TestTable(IAmazonDynamoDB client) : DynamoDbTableBase(client, "TestTable")
    {
        public DynamoDbIndex Gsi1 => new DynamoDbIndex(this, "gsi1");
        
        // Override to test virtual method behavior
        public override GetItemRequestBuilder Get() => base.Get();
        public override UpdateItemRequestBuilder Update() => base.Update();
        public override DeleteItemRequestBuilder Delete() => base.Delete();
    }

    [Fact]
    public void TableInitializationSuccess()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        table.Name.Should().Be("TestTable");
        table.DynamoDbClient.Should().NotBeNull();
    }

    [Fact]
    public void QueryMethodReturnsCorrectBuilderType()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var query = table.Query();
        
        query.Should().NotBeNull();
        query.Should().BeOfType<QueryRequestBuilder>();
        query.ToQueryRequest().TableName.Should().Be("TestTable");
    }

    [Fact]
    public void QueryMethodReturnsNewInstanceEachTime()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var query1 = table.Query();
        var query2 = table.Query();
        
        query1.Should().NotBeSameAs(query2);
    }

    [Fact]
    public void QueryWithExpressionConfiguresKeyConditionCorrectly()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var query = table.Query("pk = {0}", "USER#123");
        
        query.Should().NotBeNull();
        var request = query.ToQueryRequest();
        request.TableName.Should().Be("TestTable");
        request.KeyConditionExpression.Should().Be("pk = :p0");
        request.ExpressionAttributeValues.Should().ContainKey(":p0");
        request.ExpressionAttributeValues[":p0"].S.Should().Be("USER#123");
    }

    [Fact]
    public void QueryWithCompositeKeyExpressionConfiguresCorrectly()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var query = table.Query("pk = {0} AND sk > {1}", "USER#123", "2024-01-01");
        
        var request = query.ToQueryRequest();
        request.KeyConditionExpression.Should().Be("pk = :p0 AND sk > :p1");
        request.ExpressionAttributeValues.Should().ContainKey(":p0");
        request.ExpressionAttributeValues.Should().ContainKey(":p1");
        request.ExpressionAttributeValues[":p0"].S.Should().Be("USER#123");
        request.ExpressionAttributeValues[":p1"].S.Should().Be("2024-01-01");
    }

    [Fact]
    public void GetMethodReturnsCorrectBuilderType()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var get = table.Get();
        
        get.Should().NotBeNull();
        get.Should().BeOfType<GetItemRequestBuilder>();
        get.ToGetItemRequest().TableName.Should().Be("TestTable");
    }

    [Fact]
    public void GetMethodReturnsNewInstanceEachTime()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var get1 = table.Get();
        var get2 = table.Get();
        
        get1.Should().NotBeSameAs(get2);
    }

    [Fact]
    public void UpdateMethodReturnsCorrectBuilderType()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var update = table.Update();
        
        update.Should().NotBeNull();
        update.Should().BeOfType<UpdateItemRequestBuilder>();
        update.ToUpdateItemRequest().TableName.Should().Be("TestTable");
    }

    [Fact]
    public void UpdateMethodReturnsNewInstanceEachTime()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var update1 = table.Update();
        var update2 = table.Update();
        
        update1.Should().NotBeSameAs(update2);
    }

    [Fact]
    public void DeleteMethodReturnsCorrectBuilderType()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var delete = table.Delete();
        
        delete.Should().NotBeNull();
        delete.Should().BeOfType<DeleteItemRequestBuilder>();
        delete.ToDeleteItemRequest().TableName.Should().Be("TestTable");
    }

    [Fact]
    public void DeleteMethodReturnsNewInstanceEachTime()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var delete1 = table.Delete();
        var delete2 = table.Delete();
        
        delete1.Should().NotBeSameAs(delete2);
    }

    [Fact]
    public void PutMethodReturnsCorrectBuilderType()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var put = table.Put();
        
        put.Should().NotBeNull();
        put.Should().BeOfType<PutItemRequestBuilder>();
        put.ToPutItemRequest().TableName.Should().Be("TestTable");
    }

    [Fact]
    public void PutMethodReturnsNewInstanceEachTime()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var put1 = table.Put();
        var put2 = table.Put();
        
        put1.Should().NotBeSameAs(put2);
    }

    [Fact]
    public void VirtualGetMethodCanBeOverridden()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var get = table.Get();
        
        // Verify the method can be called and returns correct type
        get.Should().NotBeNull();
        get.Should().BeOfType<GetItemRequestBuilder>();
    }

    [Fact]
    public void VirtualUpdateMethodCanBeOverridden()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var update = table.Update();
        
        // Verify the method can be called and returns correct type
        update.Should().NotBeNull();
        update.Should().BeOfType<UpdateItemRequestBuilder>();
    }

    [Fact]
    public void VirtualDeleteMethodCanBeOverridden()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var delete = table.Delete();
        
        // Verify the method can be called and returns correct type
        delete.Should().NotBeNull();
        delete.Should().BeOfType<DeleteItemRequestBuilder>();
    }

    [Fact]
    public void QueryOnIndexReturnsBuilder()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var query = table.Gsi1.Query();
        
        query.Should().NotBeNull();
        query.ToQueryRequest().TableName.Should().Be("TestTable");
        query.ToQueryRequest().IndexName.Should().Be("gsi1");
    }

    [Fact]
    public void QueryOnIndexReturnsNewInstanceEachTime()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var query1 = table.Gsi1.Query();
        var query2 = table.Gsi1.Query();
        
        query1.Should().NotBeSameAs(query2);
    }
}