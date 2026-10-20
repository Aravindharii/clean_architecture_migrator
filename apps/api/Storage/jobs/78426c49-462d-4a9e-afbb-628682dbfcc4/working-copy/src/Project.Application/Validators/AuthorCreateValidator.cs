using Project.Application.Common.Dtos;
namespace Project.Application.Validators;

public class AuthorCreateValidator
{
    public string Name { get; set; } = string.Empty;
}
