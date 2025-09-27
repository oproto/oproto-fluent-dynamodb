namespace Oproto.FluentDynamoDb.Pagination;

/// <summary>
/// Default implementation of IPaginationRequest for configuring pagination parameters.
/// </summary>
public class PaginationRequest : IPaginationRequest
{
    /// <summary>
    /// Initializes a new instance of the PaginationRequest.
    /// </summary>
    /// <param name="pageSize">The maximum number of items to return per page.</param>
    /// <param name="paginationToken">The pagination token from the previous page response, or empty string for the first page.</param>
    public PaginationRequest(int pageSize, string paginationToken)
    {
        PageSize = pageSize;
        PaginationToken = paginationToken;
    }
    
    /// <summary>
    /// Gets the maximum number of items to return per page.
    /// </summary>
    public int PageSize { get; }
    
    /// <summary>
    /// Gets the pagination token from the previous page response.
    /// </summary>
    public string PaginationToken { get; }
}