using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibraryManagement.API.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace LibraryManagement.API.Services;

/// <summary>
/// Provides JWT token generation functionality.
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;


    /// <summary>
    /// Initializes a new instance of the <see cref="JwtService"/> class.
    /// </summary>
    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    /// <summary>
    /// Generates an access token for a user.
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var key = jwtSettings["Key"]
            ?? throw new InvalidOperationException("JWT key is missing.");

        var issuer = jwtSettings["Issuer"];

        var audience = jwtSettings["Audience"];

        var expiryMinutes = int.Parse(
            jwtSettings["ExpiryInMinutes"] ?? "15");


        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email),

            new Claim(
                ClaimTypes.Role,
                user.Role.ToString())
        };


        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));


        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);


        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);


        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
public string GenerateRefreshToken()
{
    var randomBytes = new byte[64];

    using var rng = RandomNumberGenerator.Create();

    rng.GetBytes(randomBytes);

    return Convert.ToBase64String(randomBytes);
}
}