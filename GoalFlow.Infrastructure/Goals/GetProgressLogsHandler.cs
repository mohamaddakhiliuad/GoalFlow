using GoalFlow.Application.Goals;
using GoalFlow.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GoalFlow.Infrastructure.Goals;

/// <summary>
/// Handles a query to retrieve paginated progress logs for a given goal.
/// </summary>
/// <remarks>
/// This query handler reads directly from the EF Core DbContext in a read-only manner
/// (<see cref="AsNoTracking"/>) to optimize performance. Results are ordered by 
/// creation date (most recent first) and projected into lightweight DTOs 
/// (<see cref="ProgressLogDto"/>).
/// </remarks>
public class GetProgressLogsHandler : IRequestHandler<GetProgressLogsQuery, IReadOnlyList<ProgressLogDto>>
{
    private readonly GoalFlowDbContext _db;

    /// <summary>
    /// Creates a new instance of <see cref="GetProgressLogsHandler"/>.
    /// </summary>
    /// <param name="db">The database context for GoalFlow persistence.</param>
    public GetProgressLogsHandler(GoalFlowDbContext db) => _db = db;

    /// <summary>
    /// Handles the <see cref="GetProgressLogsQuery"/> request.
    /// </summary>
    /// <param name="q">The query parameters, including GoalId, page, and page size.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> for cooperative cancellation.</param>
    /// <returns>
    /// A read-only list of <see cref="ProgressLogDto"/> items representing the logs for the specified goal.
    /// Returns an empty list if no logs are found.
    /// </returns>
    public async Task<IReadOnlyList<ProgressLogDto>> Handle(GetProgressLogsQuery q, CancellationToken ct)
    {
        // Step 1: Calculate pagination offset
        int skip = (q.Page <= 1 ? 0 : (q.Page - 1) * q.PageSize);

        // Step 2: Query ProgressLogs (read-only, ordered, projected to DTOs)
        return await _db.ProgressLogs
            .AsNoTracking() // read-only optimization
            .Where(x => x.GoalId == q.GoalId) // filter by goal
            .OrderByDescending(x => x.CreatedAt) // newest first
            .Skip(skip)
            .Take(q.PageSize)
            .Select(x => new ProgressLogDto(
                x.Id,
                x.GoalId,
                x.Delta,
                x.Note,
                x.CreatedAt
            ))
            .ToListAsync(ct);
    }
}
