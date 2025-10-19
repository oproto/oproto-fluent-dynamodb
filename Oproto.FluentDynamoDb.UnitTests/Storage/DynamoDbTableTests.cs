using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using FluentAssertions;
using NSubstitute;
using Oproto.FluentDynamoDb.Storage;

namespace Oproto.FluentDynamoDb.UnitTests.Storage;

public class DynamoDbTableTests
{
    public class TestTable(IAmazonDynamoDB client) : DynamoDbTableBase(client, "TestTable")
    {

        public DynamoDbIndex Gsi1 => new DynamoDbIndex(this, "gsi1");
    }

    [Fact]
    public void TableInitializationSuccess()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        table.Name.Should().Be("TestTable");
        table.DynamoDbClient.Should().NotBeNull();
    }

    [Fact]
    public void TableGetReturnsBuilder()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var get1 = table.Get;
        get1.Should().NotBeNull();
        get1.ToGetItemRequest().TableName.Should().Be("TestTable");
        var get2 = table.Get;
        get1.Should().NotBeSameAs(get2);
    }

    [Fact]
    public void TableQueryReturnsBuilder()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var query1 = table.Query;
        query1.Should().NotBeNull();
        query1.ToQueryRequest().TableName.Should().Be("TestTable");
        var query2 = table.Query;
        query1.Should().NotBeSameAs(query2);
    }

    [Fact]
    public void TablePutReturnsBuilder()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var put1 = table.Put;
        put1.Should().NotBeNull();
        put1.ToPutItemRequest().TableName.Should().Be("TestTable");
        var put2 = table.Put;
        put1.Should().NotBeSameAs(put2);
    }

    [Fact]
    public void TableUpdateReturnsBuilder()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var update1 = table.Update;
        update1.Should().NotBeNull();
        update1.ToUpdateItemRequest().TableName.Should().Be("TestTable");
        var update2 = table.Update;
        update1.Should().NotBeSameAs(update2);
    }

    [Fact]
    public void TableDeleteReturnsBuilder()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        // TODO table..Should().NotBeNull();
    }

    [Fact]
    public void TableQueryOnIndexReturnsBuilder()
    {
        var table = new TestTable(Substitute.For<IAmazonDynamoDB>());
        var query1 = table.Gsi1.Query;
        query1.Should().NotBeNull();
        query1.ToQueryRequest().TableName.Should().Be("TestTable");
        query1.ToQueryRequest().IndexName.Should().Be("gsi1");
        var query2 = table.Gsi1.Query;
        query1.Should().NotBeSameAs(query2);
    }
}