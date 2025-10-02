using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using GoalFlow.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace GoalFlow.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql;
    private readonly RedisContainer _redis;

    public string SqlConnectionString => _sql.GetConnectionString();
    public string RedisConnectionString { get; private set; } = default!;

    public ApiFactory()
    {
        _sql = new MsSqlBuilder()
            .WithPassword("Passw0rd!")
            .Build();

        _redis = new RedisBuilder().Build();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration(cfg =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sql"] = SqlConnectionString,
                ["ConnectionStrings:Redis"] = RedisConnectionString
            };
            cfg.AddInMemoryCollection(dict!);
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(RedisConnectionString));
        });

        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GoalFlowDbContext>();
        db.Database.Migrate();

        return host;
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        await _redis.StartAsync();

        var ep = _redis.GetConnectionString(); // مثل "localhost:XXXXX"
        RedisConnectionString = $"{ep},abortConnect=false,connectRetry=5,connectTimeout=5000";
    }

    public new async Task DisposeAsync()
    {
        await _sql.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
