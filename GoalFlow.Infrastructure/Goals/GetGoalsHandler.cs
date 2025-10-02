using System.Data;
using Dapper;
using GoalFlow.Application.Goals;
using MediatR;
using GoalFlow.Infrastructure.Sql;
using GoalFlow.Infrastructure.Caching;

namespace GoalFlow.Infrastructure.Goals;

public sealed class GetGoalsHandler : IRequestHandler<GetGoalsQuery, PagedResult<GoalSummaryDto>>
{
    private readonly ISqlConnectionFactory _factory;
    private readonly IGoalsCache _cache;

    public GetGoalsHandler(ISqlConnectionFactory factory, IGoalsCache cache)
    {
        _factory = factory;
        _cache = cache;
    }

    public async Task<PagedResult<GoalSummaryDto>> Handle(GetGoalsQuery q, CancellationToken ct)
    {
        // 1) Stable cache key via abstraction
        var cacheKey = _cache.BuildPagedKey(q.UserId, q.Page, q.PageSize, q.Search, q.Status, q.Priority);

        // 2) Try cache (early return on hit)
        var cached = await _cache.TryGet<PagedResult<GoalSummaryDto>>(cacheKey, ct);
        if (cached is not null) return cached;

        // 3) Build WHERE safely
        var where = new List<string> { "UserId = @userId" };
        var p = new DynamicParameters(new { userId = q.UserId });

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(Title LIKE @s OR Description LIKE @s)");
            p.Add("s", $"%{q.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            where.Add("Status = @status");
            p.Add("status", q.Status);
        }
        if (!string.IsNullOrWhiteSpace(q.Priority))
        {
            where.Add("Priority = @priority");
            p.Add("priority", q.Priority);
        }

        var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;

        // 4) Query DB with cancellation support
        using var conn = _factory.Create();

        var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            $"SELECT COUNT(1) FROM dbo.Goals {whereSql};",
            p, cancellationToken: ct));

        var page = q.Page <= 0 ? 1 : q.Page;
        var pageSize = q.PageSize <= 0 ? 10 : q.PageSize;
        int skip = (page - 1) * pageSize;
        p.Add("skip", skip);
        p.Add("take", pageSize);

        var items = (await conn.QueryAsync<GoalSummaryDto>(new CommandDefinition($@"
SELECT Id, Title, Description, Priority, Status, TimeBound, CreatedAt
FROM dbo.Goals
{whereSql}
ORDER BY CreatedAt DESC
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;",
            p, cancellationToken: ct))).AsList();

        var result = new PagedResult<GoalSummaryDto>(items, page, pageSize, total);

        // 5) Cache via abstraction (tagged under user)
        await _cache.CachePaged(q.UserId, cacheKey, result, TimeSpan.FromSeconds(30), ct);

        return result;
    }
}
