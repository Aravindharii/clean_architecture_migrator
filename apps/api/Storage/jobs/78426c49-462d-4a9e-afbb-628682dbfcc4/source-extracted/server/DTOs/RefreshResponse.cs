using System.Text.Json.Serialization;

namespace LibraryManagement.Api.DTOs;

public class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;

    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTime RefreshTokenExpiry { get; set; }
}
