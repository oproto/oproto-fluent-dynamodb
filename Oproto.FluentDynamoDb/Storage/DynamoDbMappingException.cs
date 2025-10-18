namespace Oproto.FluentDynamoDb.Storage;

/// <summary>
/// Exception thrown when entity mapping operations fail.
/// </summary>
public class DynamoDbMappingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DynamoDbMappingException class.
    /// </summary>
    public DynamoDbMappingException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the DynamoDbMappingException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DynamoDbMappingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DynamoDbMappingException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DynamoDbMappingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}