using Project.Application.Authentication;
using Project.Application.Common.Exceptions;
using Project.Application.Validators;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Project.Application.Common.Dtos;

namespace Project.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        JwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest dto)
    {
        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Member,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        var fullName = $"{user.FirstName} {user.LastName}";
        var token = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString(), fullName);

        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshExpiry;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User registered: {Email}", user.Email);

        return new RegisterResponse
        {
            Token = token,
            User = MapToDto(user),
            RefreshToken = refreshToken,
            RefreshTokenExpiry = refreshExpiry
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email.Trim().ToLower());

        if (user == null)
            throw new BadRequestException("Invalid email or password.");

        if (!user.IsActive)
            throw new BadRequestException("This account has been deactivated. Please contact an administrator.");

        if (user.IsDeleted)
            throw new BadRequestException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new BadRequestException("Invalid email or password.");

        var fullName = $"{user.FirstName} {user.LastName}";
        var token = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString(), fullName);

        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshExpiry;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return new LoginResponse
        {
            Token = token,
            User = MapToDto(user),
            RefreshToken = refreshToken,
            RefreshTokenExpiry = refreshExpiry
        };
    }

    public async Task<RefreshResponse> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken)
            ?? throw new BadRequestException("Invalid session. Please log in again.");

        if (user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            throw new BadRequestException("Session expired. Please log in again.");

        if (user.RefreshToken != refreshToken)
            throw new BadRequestException("Invalid session. Please log in again.");

        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = refreshExpiry;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var fullName = $"{user.FirstName} {user.LastName}";
        var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString(), fullName);

        return new RefreshResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiry = refreshExpiry
        };
    }

    public async Task LogoutAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }

        _logger.LogInformation("User logged out: {UserId}", userId);
    }

    public async Task<UserDto> GetMeAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        return MapToDto(user);
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Name = $"{user.FirstName} {user.LastName}",
        Email = user.Email,
        Role = user.Role.ToString().ToLower(),
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}
