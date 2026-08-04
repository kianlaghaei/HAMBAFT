using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hambaft.Contracts;
using Microsoft.IdentityModel.Tokens;

namespace Hambaft.Api.Security;

public sealed class JwtConfiguration
{
    private JwtConfiguration(string issuer,string audience,SymmetricSecurityKey signingKey,TimeSpan tokenLifetime)
    {
        Issuer=issuer;
        Audience=audience;
        SigningKey=signingKey;
        TokenLifetime=tokenLifetime;
    }

    public string Issuer { get; }
    public string Audience { get; }
    public SymmetricSecurityKey SigningKey { get; }
    public TimeSpan TokenLifetime { get; }

    public static JwtConfiguration From(IConfiguration configuration)
    {
        var configuredKey=configuration["Jwt:SigningKey"];
        var keyBytes=string.IsNullOrWhiteSpace(configuredKey)
            ? RandomNumberGenerator.GetBytes(32)
            : Encoding.UTF8.GetBytes(configuredKey);

        if(keyBytes.Length<32)
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 UTF-8 bytes.");

        var lifetimeMinutes=configuration.GetValue("Jwt:TeamTokenLifetimeMinutes",60);
        if(lifetimeMinutes is < 1 or > 1440)
            throw new InvalidOperationException("Jwt:TeamTokenLifetimeMinutes must be between 1 and 1440.");

        return new(
            configuration["Jwt:Issuer"]??"Hambaft",
            configuration["Jwt:Audience"]??"Hambaft.Clients",
            new SymmetricSecurityKey(keyBytes),
            TimeSpan.FromMinutes(lifetimeMinutes));
    }
}

public sealed class JwtTokenIssuer(JwtConfiguration configuration)
{
    public TokenResponse IssueTeam(Guid sessionId,Guid teamId)
    {
        var now=DateTimeOffset.UtcNow;
        var expires=now.Add(configuration.TokenLifetime);
        var claims=new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,$"team:{teamId:D}"),
            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString("D")),
            new Claim("client_role","Team"),
            new Claim("session_id",sessionId.ToString("D")),
            new Claim("team_id",teamId.ToString("D"))
        };
        var token=new JwtSecurityToken(
            issuer:configuration.Issuer,
            audience:configuration.Audience,
            claims:claims,
            notBefore:now.UtcDateTime,
            expires:expires.UtcDateTime,
            signingCredentials:new SigningCredentials(configuration.SigningKey,SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token),"Bearer",expires);
    }
}
