namespace Oproto.FluentDynamoDb.Pagination;

/// <summary>
/// Interface for pagination request parameters.
/// Used to configure pagination behavior for Query operations.
/// </summary>
public interface IPaginationRequest
{
    /// <summary>
    /// Gets the maximum number of items to return per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the pagination token from the previous page response.
    /// Use an empty string or null for the first page.
    /// </summary>
    public string PaginationToken { get; }
}