namespace LibraryManagement.API.DTOs.Common;

/// <summary>
/// Represents a paginated response.
/// </summary>
/// <typeparam name="T">The type of items in the current page.</typeparam>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}