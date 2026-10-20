namespace LibraryManagement.API.DTOs.Auth;

/// <summary>
/// Represents the credentials required to authenticate a user.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Gets or sets the email address used for authentication.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}