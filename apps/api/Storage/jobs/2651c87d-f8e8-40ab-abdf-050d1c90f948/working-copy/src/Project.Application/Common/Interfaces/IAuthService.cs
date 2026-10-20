using Project.Application.Validators;
using Project.Application.Common.Dtos;

namespace Project.Application.Common.Interfaces;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest dto);

    Task<LoginResponse> LoginAsync(LoginRequest dto);

    Task<RefreshResponse> RefreshTokenAsync(string refreshToken);

    Task LogoutAsync(int userId);

    Task<UserDto> GetMeAsync(int userId);
}
