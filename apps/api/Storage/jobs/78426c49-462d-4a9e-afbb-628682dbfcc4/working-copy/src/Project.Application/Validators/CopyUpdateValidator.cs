using Project.Application.Common.Dtos;
namespace Project.Application.Validators;

public class CopyUpdateValidator
{
    public string Barcode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
