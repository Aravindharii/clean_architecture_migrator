using System.Text.Json.Serialization;

namespace LibraryManagement.Api.DTOs;

public class RegisterResponse
{
    public string Token { get; set; } = string.Empty;

    public UserDto User { get; set; } = null!;

    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTime RefreshTokenExpiry { get; set; }
}
