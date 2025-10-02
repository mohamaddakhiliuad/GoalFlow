namespace GoalFlow.Api.Contracts
{
    /// <summary>
    /// Data transfer object for user login or registration requests.
    /// </summary>
    /// <param name="Email">The email address of the user.</param>
    /// <param name="Password">The plain-text password of the user (to be hashed and validated by the system).</param>
    public record UserDto(string Email, string Password);

    /// <summary>
    /// Data transfer object for refreshing an access token using a refresh token.
    /// </summary>
    /// <param name="UserId">The unique identifier of the user requesting the refresh.</param>
    /// <param name="RefreshToken">The refresh token previously issued to the user.</param>
    public record RefreshDto(string UserId, string RefreshToken);
}
