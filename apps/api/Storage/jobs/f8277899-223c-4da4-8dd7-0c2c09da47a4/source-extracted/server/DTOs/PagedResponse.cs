namespace LibraryManagement.Api.DTOs;

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];

    public int Page { get; set; } = 1;

    public int Size { get; set; } = 10;

    public int TotalItems { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalItems / Size);
}
