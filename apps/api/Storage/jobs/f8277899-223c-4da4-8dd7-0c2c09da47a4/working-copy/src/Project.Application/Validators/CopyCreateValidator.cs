using Project.Application.Common.Dtos;
namespace Project.Application.Validators;

public class CopyCreateValidator
{
    public int BookId { get; set; }

    public string Barcode { get; set; } = string.Empty;
}
