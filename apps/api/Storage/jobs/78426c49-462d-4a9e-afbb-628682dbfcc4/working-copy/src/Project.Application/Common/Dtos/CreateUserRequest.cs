using Project.Application.Validators;
namespace Project.Application.Common.Dtos;

public class CreateUserRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
