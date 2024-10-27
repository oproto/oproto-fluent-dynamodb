using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using NSubstitute;
using Oproto.FluentDynamoDb.Requests;

namespace Oproto.FluentDynamoDb.UnitTests.Requests;

public class UpdateItemRequestBuilderTests
{
    
    #region Keys
    
    [Fact]
    public void ForTableSuccess()
    {
        var builder = new UpdateItemRequestBuilder(Substitute.For<IAmazonDynamoDB>());
        builder.ForTable("TestTable");
        var req = builder.ToUpdateItemRequest();
        req.Should().NotBeNull();
        req.TableName.Should().Be("TestTable");
    }
    
    [Fact]
    public void WithKeyPkStringValueSuccess()
    {
        var builder = new UpdateItemRequestBuilder(Substitute.For<IAmazonDynamoDB>());
        builder.WithKey("pk", "1");
        var req = builder.ToUpdateItemRequest();
        req.Should().NotBeNull();
        req.Key.Should().NotBeNull();
        req.Key.Should().ContainKey("pk");
        req.Key.Keys.Count.Should().Be(1);
        req.Key["pk"].S.Should().Be("1");
    }
    
    [Fact]
    public void WithKeyPkSkStringValueSuccess()
    {
        var builder = new UpdateItemRequestBuilder(Substitute.For<IAmazonDynamoDB>());
        builder.WithKey("pk", "1", "sk", "abcd");
        var req = builder.ToUpdateItemRequest();
        req.Should().NotBeNull();
        req.Key.Should().NotBeNull();
        req.Key.Should().ContainKey("pk");
        req.Key.Should().ContainKey("sk");
        req.Key.Keys.Count.Should().Be(2);
        req.Key["pk"].S.Should().Be("1");
        req.Key["sk"].S.Should().Be("abcd");
    }
    
    [Fact]
    public void WithKeyPkSkAttributeValueSuccess()
    {
        var builder = new UpdateItemRequestBuilder(Substitute.For<IAmazonDynamoDB>());
        builder.WithKey("pk", new AttributeValue() { S = "1" }, "sk", new AttributeValue() { S = "abcd" });
        var req = builder.ToUpdateItemRequest();
        req.Should().NotBeNull();
        req.Key.Should().NotBeNull();
        req.Key.Should().ContainKey("pk");
        req.Key.Should().ContainKey("sk");
        req.Key.Keys.Count.Should().Be(2);
        req.Key["pk"].S.Should().Be("1");
        req.Key["sk"].S.Should().Be("abcd");
    }
    
    #endregion Keys
}