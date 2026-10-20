namespace Project.Application.Common.Dtos;

/// <summary>
/// Represents the refresh token required to obtain a new access token.
/// </summary>
public class RefreshTokenDto
{
    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}