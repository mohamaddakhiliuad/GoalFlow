using GoalFlow.Application.Goals;
using Microsoft.AspNetCore.SignalR;

namespace GoalFlow.Api.Hubs;

/// <summary>
/// Defines the contract for client methods that can be invoked by the ProgressHub.
/// </summary>
public interface IProgressClient
{
    /// <summary>
    /// Notifies the client that a progress entry has been logged for a goal.
    /// </summary>
    /// <param name="payload">The details of the progress log entry.</param>
    Task ProgressLogged(ProgressLogDto payload);

    /// <summary>
    /// Confirms that the client has successfully joined the SignalR group for the specified goal.
    /// </summary>
    /// <param name="goalId">The unique identifier of the goal joined.</param>
    Task JoinedGoal(Guid goalId);
}

/// <summary>
/// SignalR hub that manages real-time communication related to goal progress updates.
/// </summary>
public class ProgressHub : Hub<IProgressClient>
{
    /// <summary>
    /// Adds the current client connection to the SignalR group for the specified goal.
    /// </summary>
    /// <param name="goalId">The unique identifier of the goal to join.</param>
    /// <remarks>
    /// Groups in SignalR are used to broadcast updates only to clients
    /// who are subscribed to the same goal (e.g., team members or collaborators).
    /// </remarks>
    public async Task JoinGoal(Guid goalId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"goal:{goalId}");
        await Clients.Caller.JoinedGoal(goalId);
    }
}
