using System.Text.Json.Serialization;
using Project.Application.Validators;

namespace Project.Application.Common.Dtos;

public class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;

    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTime RefreshTokenExpiry { get; set; }
}
