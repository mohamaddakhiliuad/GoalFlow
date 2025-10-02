using GoalFlow.Application.Goals;
using GoalFlow.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GoalFlow.Infrastructure.Goals;

/// <summary>
/// Handles query to fetch a single Goal by Id (with ownership enforced).
/// 
/// 🔑 Teaching Point:
/// - This is a CQRS *Query Handler* (read-only).
/// - Uses EF Core projection (Select) into a DTO to avoid loading full entity.
/// - Enforces ownership: only returns a goal if it belongs to the requesting user.
/// - Uses AsNoTracking for performance (read-only).
/// </summary>
public sealed class GetGoalByIdHandler : IRequestHandler<GetGoalByIdQuery, GoalDetailDto?>
{
    private readonly GoalFlowDbContext _db;

    public GetGoalByIdHandler(GoalFlowDbContext db) => _db = db;

    /// <summary>
    /// Handle GetGoalByIdQuery:
    /// 1) Query DbContext with filters (Id + UserId).
    /// 2) Project entity → GoalDetailDto.
    /// 3) Use AsNoTracking (read-only query).
    /// 4) Return DTO or null if not found.
    /// </summary>
    public async Task<GoalDetailDto?> Handle(GetGoalByIdQuery q, CancellationToken ct)
    {
        var g = await _db.Goals
            .AsNoTracking() // Optimization: no change tracking needed for reads
            .Where(x => x.Id == q.Id && x.UserId == q.UserId) // Ownership enforced
            .Select(x => new GoalDetailDto(
                x.Id,
                x.UserId,
                x.Title,
                x.Description,
                x.Specific,
                x.Measurable,
                x.Achievable,
                x.Relevant,
                x.Priority.ToString(),
                x.Status.ToString(),
                x.TimeBound,
                x.CreatedAt
            ))
            .FirstOrDefaultAsync(ct);

        return g; // Null if not found
    }
}
