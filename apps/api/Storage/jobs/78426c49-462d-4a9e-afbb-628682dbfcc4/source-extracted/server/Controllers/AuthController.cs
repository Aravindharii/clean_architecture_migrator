using System.Security.Claims;
using LibraryManagement.Api.Common.Responses;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    private const string RefreshTokenCookieName = "RefreshToken";

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiry);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<RegisterResponse>
        {
            Success = true,
            Message = "Registration successful.",
            Data = result
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiry);

        return Ok(new ApiResponse<LoginResponse>
        {
            Success = true,
            Message = "Login successful.",
            Data = result
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "No valid session found.",
                Data = null
            });
        }

        var result = await _authService.RefreshTokenAsync(refreshToken);

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiry);

        return Ok(new ApiResponse<RefreshResponse>
        {
            Success = true,
            Message = "Token refreshed successfully.",
            Data = result
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = GetUserIdFromClaims();
        if (userId != null)
        {
            await _authService.LogoutAsync(userId.Value);
        }

        ExpireRefreshTokenCookie();

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Logout successful.",
            Data = null
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = GetUserIdFromClaims()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var result = await _authService.GetMeAsync(userId);

        return Ok(new ApiResponse<UserDto>
        {
            Success = true,
            Message = "User retrieved successfully.",
            Data = result
        });
    }

    private int? GetUserIdFromClaims()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;
        return null;
    }

    private void SetRefreshTokenCookie(string token, DateTime expires)
    {
        Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = new DateTimeOffset(expires),
            Path = "/"
        });
    }

    private void ExpireRefreshTokenCookie()
    {
        Response.Cookies.Append(RefreshTokenCookieName, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            Path = "/"
        });
    }
}
