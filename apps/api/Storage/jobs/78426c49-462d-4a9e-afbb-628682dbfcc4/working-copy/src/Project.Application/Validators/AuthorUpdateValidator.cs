using Project.Application.Common.Dtos;
namespace Project.Application.Validators;

public class AuthorUpdateValidator
{
    public string Name { get; set; } = string.Empty;
}
