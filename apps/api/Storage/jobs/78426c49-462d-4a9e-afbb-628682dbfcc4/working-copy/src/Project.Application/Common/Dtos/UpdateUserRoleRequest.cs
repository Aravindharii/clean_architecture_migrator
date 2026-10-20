using Project.Application.Validators;
namespace Project.Application.Common.Dtos;

public class UpdateUserRoleRequest
{
    public string Role { get; set; } = string.Empty;
}
