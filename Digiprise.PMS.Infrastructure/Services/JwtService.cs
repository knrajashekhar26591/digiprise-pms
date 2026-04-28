using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Digiprise.PMS.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Digiprise.PMS.Infrastructure.Services;

/// <summary>
/// Manual JWT implementation using only System.Security.Cryptography.
/// No external NuGet dependency — works with the Microsoft.AspNetCore.App framework reference.
/// </summary>
public class JwtService : IJwtService
{
    private readonly byte[] _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtService(IConfiguration config)
    {
        var secret = config["Jwt:Secret"] ?? "digiprise-pms-super-secret-key-change-in-production-256bit!!";
        _secretKey = Encoding.UTF8.GetBytes(secret);
        _issuer = config["Jwt:Issuer"] ?? "Digiprise.PMS";
        _audience = config["Jwt:Audience"] ?? "Digiprise.PMS.Client";
    }

    public string GenerateAccessToken(int userId, int tenantId, string email, string[] roles)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new
        {
            sub = userId.ToString(),
            email,
            tenantId,
            roles,
            iss = _issuer,
            aud = _audience,
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds(),
            jti = Guid.NewGuid().ToString("N")
        };

        var headerB64 = Base64UrlEncode(JsonSerializer.Serialize(header));
        var payloadB64 = Base64UrlEncode(JsonSerializer.Serialize(payload));
        var signingInput = $"{headerB64}.{payloadB64}";
        var signature = ComputeHmacSha256(signingInput, _secretKey);
        return $"{signingInput}.{signature}";
    }

    public string GenerateRefreshToken()
        => Base64UrlEncode(RandomNumberGenerator.GetBytes(64));

    public (int userId, int tenantId) ValidateAccessToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) throw new SecurityTokenException("Invalid token format.");

        var signingInput = $"{parts[0]}.{parts[1]}";
        var expectedSig = ComputeHmacSha256(signingInput, _secretKey);
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSig),
            Encoding.UTF8.GetBytes(parts[2])))
            throw new SecurityTokenException("Token signature invalid.");

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);

        var exp = payload.GetProperty("exp").GetInt64();
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
            throw new SecurityTokenException("Token has expired.");

        var userId = int.Parse(payload.GetProperty("sub").GetString()!);
        var tenantId = payload.GetProperty("tenantId").GetInt32();
        return (userId, tenantId);
    }

    private static string ComputeHmacSha256(string data, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Base64UrlEncode(string json)
        => Base64UrlEncode(Encoding.UTF8.GetBytes(json));

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
    }
}

/// <summary>Manual token validation exception</summary>
public class SecurityTokenException : Exception
{
    public SecurityTokenException(string message) : base(message) { }
}

/// <summary>Current user context populated from JWT claims</summary>
public class CurrentUserContext : ICurrentUserContext
{
    public int UserId { get; set; }
    public int TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public bool IsAdmin => Roles.Contains("Admin");

    public static CurrentUserContext FromClaims(ClaimsPrincipal principal)
    {
        return new CurrentUserContext
        {
            UserId = int.TryParse(principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : 0,
            TenantId = int.TryParse(principal.FindFirstValue("tenantId"), out var tid) ? tid : 0,
            Email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email) ?? "",
            Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value)
                .Concat(principal.FindAll("roles").Select(c => c.Value)).ToArray()
        };
    }
}
