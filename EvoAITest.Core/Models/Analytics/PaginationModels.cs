namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Pagination request parameters
/// </summary>
public sealed class PaginationRequest
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size (items per page)
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Maximum page size allowed
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Gets validated page number (minimum 1)
    /// </summary>
    public int ValidatedPageNumber => Math.Max(1, PageNumber);

    /// <summary>
    /// Gets validated page size (between 1 and MaxPageSize)
    /// </summary>
    public int ValidatedPageSize => Math.Min(Math.Max(1, PageSize), MaxPageSize);

    /// <summary>
    /// Calculates skip count for database query
    /// </summary>
    public int Skip => (ValidatedPageNumber - 1) * ValidatedPageSize;

    /// <summary>
    /// Gets take count for database query
    /// </summary>
    public int Take => ValidatedPageSize;
}

/// <summary>
/// Paginated response wrapper
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public sealed class PaginatedResponse<T>
{
    /// <summary>
    /// Current page items
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total items count
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total pages count
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Has previous page
    /// </summary>
    public bool HasPrevious => PageNumber > 1;

    /// <summary>
    /// Has next page
    /// </summary>
    public bool HasNext => PageNumber < TotalPages;
}

/// <summary>
/// Query optimization options
/// </summary>
public sealed class QueryOptimizationOptions
{
    /// <summary>
    /// Use read-only tracking for queries
    /// </summary>
    public bool AsNoTracking { get; set; } = true;

    /// <summary>
    /// Maximum results to return
    /// </summary>
    public int? MaxResults { get; set; } = 10000;

    /// <summary>
    /// Query timeout in seconds
    /// </summary>
    public int? TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Use compiled queries if available
    /// </summary>
    public bool UseCompiledQueries { get; set; } = true;
}
