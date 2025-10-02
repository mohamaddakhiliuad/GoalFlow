using GoalFlow.Api.Hubs;
using GoalFlow.Application.Goals;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace GoalFlow.Api.Notifications;

/// <summary>
/// Notification handler that reacts when a new progress log is created
/// and broadcasts it in real-time via SignalR.
/// </summary>
public sealed class ProgressLogCreatedHandler : INotificationHandler<ProgressLogCreated>
{
    private readonly IHubContext<ProgressHub, IProgressClient> _hub;

    /// <summary>
    /// Creates a new instance of <see cref="ProgressLogCreatedHandler"/>.
    /// </summary>
    /// <param name="hub">
    /// The SignalR hub context used to send progress updates to connected clients.
    /// </param>
    public ProgressLogCreatedHandler(IHubContext<ProgressHub, IProgressClient> hub) => _hub = hub;

    /// <summary>
    /// Handles the <see cref="ProgressLogCreated"/> domain event by broadcasting
    /// the progress log to all clients subscribed to the goal group.
    /// </summary>
    /// <param name="n">The notification containing progress log details.</param>
    /// <param name="ct">Cancellation token for the async operation.</param>
    public async Task Handle(ProgressLogCreated n, CancellationToken ct)
    {
        var dto = new ProgressLogDto(n.Id, n.GoalId, n.Delta, n.Note, n.CreatedAt);

        // Broadcast to all clients connected to the group for the specific goal
        await _hub.Clients.Group($"goal:{n.GoalId}").ProgressLogged(dto);
    }
}
