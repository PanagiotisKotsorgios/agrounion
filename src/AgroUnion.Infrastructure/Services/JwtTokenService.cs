using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AgroUnion.Infrastructure.Services;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public JwtTokenResult Create(string userId, string email, string role)
    {
        var expires = DateTime.UtcNow.AddHours(configuration.GetValue("Jwt:Hours", 8));
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Δεν έχει οριστεί Jwt:Key.");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims, expires: expires, signingCredentials: credentials);
        return new JwtTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires, role);
    }
}
