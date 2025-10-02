using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GoalFlow.Api.Auth;

/// <summary>
/// Provides extension methods for working with <see cref="ClaimsPrincipal"/> 
/// in the GoalFlow authentication layer.
/// </summary>
public static class ClaimsExtensions
{
    /// <summary>
    /// Retrieves the current user's unique identifier (uid) from claims.  
    /// Falls back to <see cref="ClaimTypes.NameIdentifier"/> or <see cref="JwtRegisteredClaimNames.Sub"/> 
    /// if "uid" is not present.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the current user.</param>
    /// <returns>The user's <see cref="Guid"/> identifier.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the claim is missing or cannot be parsed into a valid <see cref="Guid"/>.
    /// </exception>
    public static Guid GetUserIdOrThrow(this ClaimsPrincipal user)
    {
        // Try primary claim: "uid"
        var uid = user.FindFirstValue("uid")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier) // fallback option
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub); // fallback option

        if (!Guid.TryParse(uid, out var id))
            throw new UnauthorizedAccessException("Invalid or missing uid claim.");

        return id;
    }
}
