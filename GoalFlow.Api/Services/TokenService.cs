using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using GoalFlow.Domain.Entities;
using GoalFlow.Infrastructure.Persistence;

namespace GoalFlow.Api.Services;

/// <summary>
/// Issues and validates JWT access tokens and hashed refresh tokens for GoalFlow.
/// </summary>
/// <remarks>
/// - Access token: short-lived JWT (signed with HMAC SHA-256).
/// - Refresh token: random 64 bytes, returned raw to the client; only the SHA-256 hash is stored in the database.
/// - Rotation: on successful refresh, the old refresh token is revoked and a new pair is issued.
/// </remarks>
public sealed class TokenService
{
    private readonly IConfiguration _cfg;
    private readonly GoalFlowDbContext _db;

    /// <summary>
    /// Creates a new instance of <see cref="TokenService"/>.
    /// </summary>
    /// <param name="cfg">Application configuration (expects a "Jwt" section with Issuer, Audience, Key).</param>
    /// <param name="db">Database context for persisting refresh tokens.</param>
    public TokenService(IConfiguration cfg, GoalFlowDbContext db)
    {
        _cfg = cfg;
        _db = db;
    }

    /// <summary>
    /// Issues a new access token and a new refresh token for the specified user.
    /// </summary>
    /// <param name="user">The authenticated Identity user.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description><c>access</c>: the signed JWT string.</description></item>
    /// <item><description><c>refresh</c>: the raw refresh token (base64). Only the hash is stored server-side.</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// - Access token expires in 15 minutes.  
    /// - Refresh token expires in 7 days and is stored as a SHA-256 hash.
    /// </remarks>
    public (string access, string refresh) IssueTokens(IdentityUser user)
    {
        var jwt = _cfg.GetSection("Jwt");

        // Build signing credentials from symmetric key
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Minimal claims; include a stable user identifier and email when available
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim("uid", user.Id)
        };

        // Create short-lived access token (15 minutes)
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        string access = new JwtSecurityTokenHandler().WriteToken(token);

        // Create refresh token: random 64 bytes, return raw; persist only SHA-256 hash
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        using var sha = SHA256.Create();
        var hash = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw)));

        var rt = new RefreshToken(Guid.Parse(user.Id), hash, DateTimeOffset.UtcNow.AddDays(7));
        _db.RefreshTokens.Add(rt);
        _db.SaveChanges();

        // Return access token and the raw refresh token (client stores this securely)
        return (access, raw);
    }

    /// <summary>
    /// Validates an incoming refresh token, revokes the old record, and issues a new access/refresh pair.
    /// </summary>
    /// <param name="refreshRaw">The raw refresh token presented by the client.</param>
    /// <param name="user">The requesting Identity user.</param>
    /// <param name="newAccess">Outputs a newly issued access token on success.</param>
    /// <param name="newRefresh">Outputs a newly issued raw refresh token on success.</param>
    /// <returns><c>true</c> if validation and rotation succeed; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// - Compares the SHA-256 hash of the provided raw token with the stored hash.  
    /// - Ensures the refresh token is active, then revokes it and issues a fresh pair (rotation).
    /// </remarks>
    public bool ValidateAndRotate(string refreshRaw, IdentityUser user, out string newAccess, out string newRefresh)
    {
        using var sha = SHA256.Create();
        var hash = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(refreshRaw)));

        var existing = _db.RefreshTokens
            .FirstOrDefault(x => x.UserId.ToString() == user.Id && x.TokenHash == hash);

        newAccess = string.Empty;
        newRefresh = string.Empty;

        if (existing is null || !existing.IsActive)
            return false;

        // Invalidate the current refresh token
        existing.Revoke();

        // Issue a fresh access/refresh pair and persist the new refresh token
        var (acc, r) = IssueTokens(user);
        newAccess = acc;
        newRefresh = r;

        _db.SaveChanges();
        return true;
    }
}
