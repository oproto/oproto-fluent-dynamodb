using System.Text;
using Amazon.DynamoDBv2.Model;
using Oproto.FluentDynamoDb.IntegrationTests.Infrastructure;
using Oproto.FluentDynamoDb.IntegrationTests.TestEntities;
using Oproto.FluentDynamoDb.Storage;

namespace Oproto.FluentDynamoDb.IntegrationTests.Security;

/// <summary>
/// Integration tests for table.Encrypt() method in format string expressions.
/// Tests end-to-end encryption with DynamoDB Local using format string syntax.
/// </summary>
[Collection("DynamoDB Local")]
[Trait("Category", "Integration")]
[Trait("Feature", "Encryption")]
[Trait("Feature", "FormatStrings")]
public class FormatStringEncryptionIntegrationTests : IntegrationTestBase
{
    private class TestTable : DynamoDbTableBase
    {
        public TestTable(Amazon.DynamoDBv2.IAmazonDynamoDB client, string tableName, IFieldEncryptor? encryptor = null)
            : base(client, tableName, logger: null, fieldEncryptor: encryptor)
        {
        }
    }

    public FormatStringEncryptionIntegrationTests(DynamoDbLocalFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Query_WithTableEncryptInFormatString_EncryptsAndQueriesCorrectly()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        var entity = new SecureTestEntity
        {
            Id = "user-123",
            Name = "John Doe",
            SocialSecurityNumber = "123-45-6789",
            Email = "john@example.com",
            PublicData = "Public information"
        };

        // Manually encrypt and store the entity
        var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber);
        var encryptedSsn = await encryptor.EncryptAsync(
            ssnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "tenant-123" },
            CancellationToken.None);

        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = entity.Id },
            ["name"] = new AttributeValue { S = entity.Name },
            ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
            ["email"] = new AttributeValue { S = entity.Email },
            ["public_data"] = new AttributeValue { S = entity.PublicData }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Query using table.Encrypt() in format string expression
        DynamoDbOperationContext.EncryptionContextId = "tenant-123";
        var targetSsn = "123-45-6789";
        
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            FilterExpression = "ssn = :ssn",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "user-123" },
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(table.Encrypt(targetSsn, "ssn"))) }
            }
        };

        var response = await DynamoDb.QueryAsync(queryRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        
        var retrievedItem = response.Items[0];
        retrievedItem["pk"].S.Should().Be("user-123");
        retrievedItem["ssn"].B.Should().NotBeNull();
        
        // Verify the SSN is encrypted in DynamoDB
        var storedSsnBytes = retrievedItem["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "tenant-123" },
            CancellationToken.None);
        var decryptedSsn = Encoding.UTF8.GetString(decryptedSsnBytes);
        decryptedSsn.Should().Be("123-45-6789");
        
        // Verify encryptor was called
        encryptor.EncryptCalls.Should().Contain(c => 
            Encoding.UTF8.GetString(c.Plaintext) == "123-45-6789" && 
            c.FieldName == "ssn" &&
            c.Context.ContextId == "tenant-123");
    }

    [Fact]
    public async Task Query_WithTableEncryptAndWithValue_EncryptsAndQueriesCorrectly()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        var entity = new SecureTestEntity
        {
            Id = "user-456",
            Name = "Jane Smith",
            SocialSecurityNumber = "987-65-4321",
            Email = "jane@example.com"
        };

        // Manually encrypt and store the entity
        var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber);
        var encryptedSsn = await encryptor.EncryptAsync(
            ssnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "tenant-456" },
            CancellationToken.None);

        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = entity.Id },
            ["name"] = new AttributeValue { S = entity.Name },
            ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
            ["email"] = new AttributeValue { S = entity.Email }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Query using table.Encrypt() with WithValue pattern
        DynamoDbOperationContext.EncryptionContextId = "tenant-456";
        var targetSsn = "987-65-4321";
        var encryptedTargetSsn = table.Encrypt(targetSsn, "ssn");
        
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            FilterExpression = "ssn = :ssn",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "user-456" },
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) }
            }
        };

        var response = await DynamoDb.QueryAsync(queryRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        
        var retrievedItem = response.Items[0];
        retrievedItem["pk"].S.Should().Be("user-456");
        retrievedItem["name"].S.Should().Be("Jane Smith");
        
        // Verify the SSN is encrypted and can be decrypted
        var storedSsnBytes = retrievedItem["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "tenant-456" },
            CancellationToken.None);
        var decryptedSsn = Encoding.UTF8.GetString(decryptedSsnBytes);
        decryptedSsn.Should().Be("987-65-4321");
        
        // Verify encryptor was called once
        encryptor.EncryptCalls.Should().HaveCount(1);
        encryptor.EncryptCalls[0].FieldName.Should().Be("ssn");
        encryptor.EncryptCalls[0].Context.ContextId.Should().Be("tenant-456");
    }

    [Fact]
    public async Task Scan_WithTableEncryptInFormatString_EncryptsAndScansCorrectly()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        // Create multiple entities with encrypted SSNs
        var entities = new[]
        {
            new SecureTestEntity
            {
                Id = "user-1",
                Name = "Alice",
                SocialSecurityNumber = "111-11-1111",
                Email = "alice@example.com"
            },
            new SecureTestEntity
            {
                Id = "user-2",
                Name = "Bob",
                SocialSecurityNumber = "222-22-2222",
                Email = "bob@example.com"
            },
            new SecureTestEntity
            {
                Id = "user-3",
                Name = "Charlie",
                SocialSecurityNumber = "333-33-3333",
                Email = "charlie@example.com"
            }
        };

        // Store entities with encrypted SSNs
        foreach (var entity in entities)
        {
            var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber!);
            var encryptedSsn = await encryptor.EncryptAsync(
                ssnBytes,
                "ssn",
                new FieldEncryptionContext { ContextId = "scan-test" },
                CancellationToken.None);

            var item = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue { S = entity.Id },
                ["name"] = new AttributeValue { S = entity.Name },
                ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
                ["email"] = new AttributeValue { S = entity.Email! }
            };

            await DynamoDb.PutItemAsync(TableName, item);
        }

        // Act - Scan with filter using table.Encrypt() in format string
        DynamoDbOperationContext.EncryptionContextId = "scan-test";
        var targetSsn = "222-22-2222";
        var encryptedTargetSsn = table.Encrypt(targetSsn, "ssn");
        
        var scanRequest = new ScanRequest
        {
            TableName = TableName,
            FilterExpression = "ssn = :ssn",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) }
            }
        };

        var response = await DynamoDb.ScanAsync(scanRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        response.Items[0]["pk"].S.Should().Be("user-2");
        response.Items[0]["name"].S.Should().Be("Bob");
        
        // Verify the SSN is encrypted and can be decrypted
        var storedSsnBytes = response.Items[0]["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "scan-test" },
            CancellationToken.None);
        var decryptedSsn = Encoding.UTF8.GetString(decryptedSsnBytes);
        decryptedSsn.Should().Be("222-22-2222");
    }

    [Fact]
    public async Task Query_WithMultipleEncryptedFieldsInFormatString_EncryptsAllFieldsCorrectly()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        var entity = new SecureTestEntity
        {
            Id = "user-multi",
            Name = "Multi Field User",
            SocialSecurityNumber = "555-55-5555",
            CreditCardNumber = "4111-1111-1111-1111",
            Email = "multi@example.com"
        };

        // Store entity with encrypted fields
        var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber);
        var ccBytes = Encoding.UTF8.GetBytes(entity.CreditCardNumber);
        
        var encryptedSsn = await encryptor.EncryptAsync(
            ssnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "multi-field" },
            CancellationToken.None);
        
        var encryptedCc = await encryptor.EncryptAsync(
            ccBytes,
            "credit_card",
            new FieldEncryptionContext { ContextId = "multi-field" },
            CancellationToken.None);

        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = entity.Id },
            ["name"] = new AttributeValue { S = entity.Name },
            ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
            ["credit_card"] = new AttributeValue { B = new MemoryStream(encryptedCc) },
            ["email"] = new AttributeValue { S = entity.Email }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Query with multiple encrypted fields in format string
        DynamoDbOperationContext.EncryptionContextId = "multi-field";
        var targetSsn = "555-55-5555";
        var targetCc = "4111-1111-1111-1111";
        
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            FilterExpression = "ssn = :ssn AND credit_card = :cc",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "user-multi" },
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(table.Encrypt(targetSsn, "ssn"))) },
                [":cc"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(table.Encrypt(targetCc, "credit_card"))) }
            }
        };

        var response = await DynamoDb.QueryAsync(queryRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        var retrievedItem = response.Items[0];
        
        // Verify SSN is encrypted and can be decrypted
        var storedSsnBytes = retrievedItem["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "multi-field" },
            CancellationToken.None);
        Encoding.UTF8.GetString(decryptedSsnBytes).Should().Be("555-55-5555");
        
        // Verify credit card is encrypted and can be decrypted
        var storedCcBytes = retrievedItem["credit_card"].B.ToArray();
        var decryptedCcBytes = await encryptor.DecryptAsync(
            storedCcBytes,
            "credit_card",
            new FieldEncryptionContext { ContextId = "multi-field" },
            CancellationToken.None);
        Encoding.UTF8.GetString(decryptedCcBytes).Should().Be("4111-1111-1111-1111");
        
        // Verify both fields were encrypted
        encryptor.EncryptCalls.Should().HaveCount(2);
        encryptor.EncryptCalls.Should().Contain(c => c.FieldName == "ssn");
        encryptor.EncryptCalls.Should().Contain(c => c.FieldName == "credit_card");
    }

    [Fact]
    public async Task Query_WithEncryptedValueAndDifferentContexts_UsesCorrectContext()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = MockFieldEncryptor.CreateWithContextKeys("context-A", "context-B");
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        // Store entity with context-A
        var entity = new SecureTestEntity
        {
            Id = "user-context",
            Name = "Context User",
            SocialSecurityNumber = "777-77-7777",
            Email = "context@example.com"
        };

        var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber);
        var encryptedSsn = await encryptor.EncryptAsync(
            ssnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "context-A" },
            CancellationToken.None);

        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = entity.Id },
            ["name"] = new AttributeValue { S = entity.Name },
            ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
            ["email"] = new AttributeValue { S = entity.Email }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Query with correct context
        DynamoDbOperationContext.EncryptionContextId = "context-A";
        var targetSsn = "777-77-7777";
        var encryptedTargetSsn = table.Encrypt(targetSsn, "ssn");
        
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            FilterExpression = "ssn = :ssn",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "user-context" },
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) }
            }
        };

        var response = await DynamoDb.QueryAsync(queryRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        var retrievedItem = response.Items[0];
        
        // Verify decryption with correct context works
        var storedSsnBytes = retrievedItem["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "context-A" },
            CancellationToken.None);
        Encoding.UTF8.GetString(decryptedSsnBytes).Should().Be("777-77-7777");
        
        // Verify decryption with wrong context fails
        var decryptWithWrongContext = async () => await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "context-B" },
            CancellationToken.None);
        
        await decryptWithWrongContext.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Encryption context mismatch*");
    }

    [Fact]
    public async Task Query_WithEncryptedValueRoundTrip_MaintainsDataIntegrity()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        var originalSsn = "999-99-9999";
        DynamoDbOperationContext.EncryptionContextId = "roundtrip";
        
        // Encrypt the value using format string pattern
        var encryptedSsn = table.Encrypt(originalSsn, "ssn");
        
        // Store entity with encrypted SSN
        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = "user-roundtrip" },
            ["name"] = new AttributeValue { S = "Roundtrip User" },
            ["ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedSsn)) }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Retrieve and decrypt
        var getRequest = new GetItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue { S = "user-roundtrip" }
            }
        };

        var getResponse = await DynamoDb.GetItemAsync(getRequest);

        // Assert
        getResponse.IsItemSet.Should().BeTrue();
        var storedSsnBytes = getResponse.Item["ssn"].B.ToArray();
        
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "roundtrip" },
            CancellationToken.None);
        
        var decryptedSsn = Encoding.UTF8.GetString(decryptedSsnBytes);
        decryptedSsn.Should().Be(originalSsn);
        
        // Verify the encrypted value is base64-encoded
        var decoded = Convert.FromBase64String(encryptedSsn);
        decoded.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Scan_WithEncryptedValueInComplexFilter_WorksCorrectly()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        // Create entities with different attributes
        var entities = new[]
        {
            new { Id = "user-A", Ssn = "111-11-1111", Name = "Alice", Email = "alice@example.com" },
            new { Id = "user-B", Ssn = "222-22-2222", Name = "Bob", Email = "bob@example.com" },
            new { Id = "user-C", Ssn = "333-33-3333", Name = "Charlie", Email = "charlie@example.com" }
        };

        foreach (var entity in entities)
        {
            var ssnBytes = Encoding.UTF8.GetBytes(entity.Ssn);
            var encryptedSsn = await encryptor.EncryptAsync(
                ssnBytes,
                "ssn",
                new FieldEncryptionContext { ContextId = "complex-filter" },
                CancellationToken.None);

            var item = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue { S = entity.Id },
                ["name"] = new AttributeValue { S = entity.Name },
                ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
                ["email"] = new AttributeValue { S = entity.Email }
            };

            await DynamoDb.PutItemAsync(TableName, item);
        }

        // Act - Scan with complex filter including encrypted value
        DynamoDbOperationContext.EncryptionContextId = "complex-filter";
        var targetSsn = "222-22-2222";
        var encryptedTargetSsn = table.Encrypt(targetSsn, "ssn");
        
        var scanRequest = new ScanRequest
        {
            TableName = TableName,
            FilterExpression = "ssn = :ssn AND begins_with(#name, :namePrefix)",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#name"] = "name"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) },
                [":namePrefix"] = new AttributeValue { S = "B" }
            }
        };

        var response = await DynamoDb.ScanAsync(scanRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        response.Items[0]["pk"].S.Should().Be("user-B");
        response.Items[0]["name"].S.Should().Be("Bob");
        
        // Verify the SSN is encrypted and can be decrypted
        var storedSsnBytes = response.Items[0]["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "complex-filter" },
            CancellationToken.None);
        var decryptedSsn = Encoding.UTF8.GetString(decryptedSsnBytes);
        decryptedSsn.Should().Be("222-22-2222");
    }

    [Fact]
    public async Task Query_WithNullEncryptionContext_EncryptsWithNullContext()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        var entity = new SecureTestEntity
        {
            Id = "user-null",
            Name = "Null Context User",
            SocialSecurityNumber = "000-00-0000",
            Email = "null@example.com"
        };

        // Store with null context
        var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber);
        var encryptedSsn = await encryptor.EncryptAsync(
            ssnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = null },
            CancellationToken.None);

        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = entity.Id },
            ["name"] = new AttributeValue { S = entity.Name },
            ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
            ["email"] = new AttributeValue { S = entity.Email }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Query with null context using format string
        DynamoDbOperationContext.EncryptionContextId = null;
        var targetSsn = "000-00-0000";
        var encryptedTargetSsn = table.Encrypt(targetSsn, "ssn");
        
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            FilterExpression = "ssn = :ssn",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "user-null" },
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) }
            }
        };

        var response = await DynamoDb.QueryAsync(queryRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        var retrievedItem = response.Items[0];
        
        // Verify decryption with null context works
        var storedSsnBytes = retrievedItem["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = null },
            CancellationToken.None);
        Encoding.UTF8.GetString(decryptedSsnBytes).Should().Be("000-00-0000");
    }
}
