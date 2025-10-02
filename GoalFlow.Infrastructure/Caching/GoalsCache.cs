using System.Text.Json;
using System.Text.RegularExpressions;
using StackExchange.Redis;

namespace GoalFlow.Infrastructure.Caching;

public sealed class GoalsCache : IGoalsCache
{
    private readonly IConnectionMultiplexer _cx;
    private readonly JsonSerializerOptions _json;
    private const string TagPrefix = "tag:goals:u=";

    public GoalsCache(IConnectionMultiplexer cx)
    {
        _cx = cx;
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    // --- Helpers -------------------------------------------------------------

    private static string Norm(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "-";
        var t = s.Trim().ToLowerInvariant();
        return Regex.Replace(t, @"\s+", " "); // collapse spaces
    }

    public string BuildPagedKey(Guid userId, int page, int pageSize, string? search, string? status, string? priority)
    {
        var p = page <= 0 ? 1 : page;
        var ps = pageSize <= 0 ? 10 : pageSize;

        var sSearch = Norm(search);
        var sStatus = Norm(status);
        var sPriority = Norm(priority);

        return $"goals:u={userId}:p={p}:ps={ps}:s={sSearch}:st={sStatus}:pr={sPriority}";
    }

    // --- Reads ---------------------------------------------------------------

    public async Task<T?> TryGet<T>(string cacheKey, CancellationToken ct = default)
    {
        try
        {
            var db = _cx.GetDatabase();
            var val = await db.StringGetAsync(cacheKey);
            if (!val.HasValue) return default;
            return JsonSerializer.Deserialize<T>(val!, _json);
        }
        catch
        {
            // Cache optional — swallow errors
            return default;
        }
    }

    // --- Writes --------------------------------------------------------------

    public async Task CachePaged(Guid userId, string cacheKey, object data, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            var db = _cx.GetDatabase();
            var tagKey = $"{TagPrefix}{userId}";
            var tx = db.CreateTransaction();

            _ = tx.StringSetAsync(cacheKey, JsonSerializer.Serialize(data, _json), ttl);
            _ = tx.SetAddAsync(tagKey, cacheKey);
            _ = tx.KeyExpireAsync(tagKey, ttl + TimeSpan.FromMinutes(5));

            await tx.ExecuteAsync();
        }
        catch
        {
            // Cache optional — ignore failures
        }
    }

    public async Task InvalidateUser(Guid userId)
    {
        try
        {
            var db = _cx.GetDatabase();
            var tagKey = $"{TagPrefix}{userId}";
            var members = await db.SetMembersAsync(tagKey);

            if (members.Length > 0)
            {
                // حذف کلیدهای عضو تگ (فعال‌سازی در صورت نیاز)
                // var keys = members.Select(m => (RedisKey)m).ToArray();
                // await db.KeyDeleteAsync(keys);
            }

            await db.KeyDeleteAsync(tagKey);
        }
        catch
        {
            // Cache optional — ignore failures
        }
    }
}
