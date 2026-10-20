namespace LibraryManagement.Api.DTOs;

public class CopyCreateValidator
{
    public int BookId { get; set; }

    public string Barcode { get; set; } = string.Empty;
}
