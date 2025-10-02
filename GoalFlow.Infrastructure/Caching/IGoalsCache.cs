using System;

namespace GoalFlow.Infrastructure.Caching;

public interface IGoalsCache
{
    /// <summary>
    /// Builds a stable cache key for a paged "goals list" query.
    /// Inputs are normalized (trim/lower/whitespace collapse) to maximize cache hits.
    /// </summary>
    string BuildPagedKey(Guid userId, int page, int pageSize, string? search, string? status, string? priority);

    /// <summary>
    /// Try read a cached value (returns null if not found or on cache errors).
    /// </summary>
    Task<T?> TryGet<T>(string cacheKey, CancellationToken ct = default);

    /// <summary>
    /// Cache a paged result for a given user and tag it for bulk invalidation.
    /// </summary>
    Task CachePaged(Guid userId, string cacheKey, object data, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// Invalidate all cached goal pages for a given user (tag-based).
    /// </summary>
    Task InvalidateUser(Guid userId);
}
