namespace GoalFlow.Domain.Entities;

/// <summary>
/// Represents a refresh token issued to a user for authentication.
/// Refresh tokens are long-lived and used to obtain new access tokens
/// after the short-lived access token expires.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>
    /// Unique identifier for this refresh token entry.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// The identifier of the user to whom this token belongs.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The hashed value of the refresh token.
    /// The raw token is never persisted for security reasons.
    /// </summary>
    public string TokenHash { get; private set; } = default!;

    /// <summary>
    /// Expiry date and time (UTC) after which the token is invalid.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Date and time (UTC) when the token was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Date and time (UTC) when the token was revoked, if applicable.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Optional reference to the new token that replaced this one.
    /// Used during token rotation.
    /// </summary>
    public string? ReplacedByToken { get; private set; }

    /// <summary>
    /// Required for EF Core materialization.
    /// Prevents direct construction without parameters.
    /// </summary>
    private RefreshToken() { }

    /// <summary>
    /// Creates a new refresh token for the specified user.
    /// </summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="tokenHash">The hashed value of the refresh token.</param>
    /// <param name="expiresAt">The expiry date and time (UTC).</param>
    public RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Indicates whether the refresh token is still valid (not expired and not revoked).
    /// </summary>
    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;

    /// <summary>
    /// Revokes the token immediately.
    /// Optionally links it to the new token that replaced it.
    /// </summary>
    /// <param name="replacedBy">The identifier or hash of the replacement token.</param>
    public void Revoke(string? replacedBy = null)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        ReplacedByToken = replacedBy;
    }
}
