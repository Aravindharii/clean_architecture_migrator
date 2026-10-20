using Project.Application.Common.Dtos;
using Project.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Project.Application.Common.Interfaces;
using Project.Application.Authentication;

namespace Project.API.Controllers;

/// <summary>
/// Provides authentication related endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;


    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }


    /// <summary>
    /// Registers a new user.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        await _authService.RegisterAsync(registerDto);

        return Ok(new
        {
            message = "User registered successfully."
        });
    }
/// <summary>
/// Authenticates a user and returns JWT tokens.
/// </summary>
/// <param name="loginDto">User login credentials.</param>
/// <returns>Authentication response containing tokens.</returns>
[HttpPost("login")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> Login(LoginDto loginDto)
{
    var response = await _authService.LoginAsync(loginDto);

    return Ok(response);
}

/// <summary>
/// Generates new authentication tokens using a valid refresh token.
/// </summary>
/// <param name="refreshTokenDto">
/// Refresh token used to obtain new authentication tokens.
/// </param>
/// <returns>
/// Authentication response containing new access and refresh tokens.
/// </returns>
[HttpPost("refresh-token")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> RefreshToken(
    RefreshTokenDto refreshTokenDto)
{
    var response =
        await _authService.RefreshTokenAsync(refreshTokenDto);

    return Ok(response);
}
/// <summary>
/// Logs out the currently authenticated user by invalidating
/// the stored refresh token.
/// </summary>
/// <returns>
/// No content when logout is successful.
/// </returns>
[HttpPost("logout")]
[Authorize]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> Logout()
{
    var userIdClaim = User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier);

    if (userIdClaim == null ||
        !int.TryParse(userIdClaim.Value, out var userId))
    {
        return Unauthorized();
    }

    await _authService.LogoutAsync(userId);

    return NoContent();
}
}
