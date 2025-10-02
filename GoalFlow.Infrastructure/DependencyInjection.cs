using GoalFlow.Infrastructure.Persistence;
using GoalFlow.Infrastructure.Reminders;
using GoalFlow.Infrastructure.Sql;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace GoalFlow.Infrastructure;

/// <summary>
/// Service registration entry point for the Infrastructure layer.
/// </summary>
/// <remarks>
/// This extension method wires up all technology-facing components:
/// - EF Core (DbContext)
/// - MediatR handlers hosted in this assembly
/// - Dapper connection factory
/// - Redis multiplexing and goal-list caching
/// - Hangfire background processing (disabled in Test)
/// - ASP.NET Core Identity (stores backed by EF)
/// 
/// Keep this layer technology-focused and replaceable; no business logic here.
/// </remarks>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure services into the DI container.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="config">Application configuration (connection strings, options).</param>
    /// <param name="env">Host environment (to conditionally enable services like Hangfire).</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        // --------------------------------------------------------------------
        // 1) Persistence (EF Core DbContext)
        // --------------------------------------------------------------------
        services.AddDbContext<GoalFlowDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString("Sql")));

        // --------------------------------------------------------------------
        // 2) MediatR (handlers in this Infrastructure assembly)
        //    NOTE: Keep Application-layer requests/contracts independent.
        // --------------------------------------------------------------------
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // --------------------------------------------------------------------
        // 3) Dapper connection factory (raw SQL access where needed)
        // --------------------------------------------------------------------
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

        // --------------------------------------------------------------------
        // 4) Redis (connection multiplexer) + Goals cache abstraction
        //    - AbortOnConnectFail=false ensures app keeps running if Redis is down.
        //    - Single IConnectionMultiplexer per app domain (recommended).
        // --------------------------------------------------------------------
        var redisCs = config.GetConnectionString("Redis") ?? "localhost:6379";
        var redisOptions = ConfigurationOptions.Parse(redisCs, allowAdmin: true);
        redisOptions.AbortOnConnectFail = false;
        redisOptions.ConnectRetry = 5;
        redisOptions.ConnectTimeout = 5000;

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
        services.AddSingleton<GoalFlow.Infrastructure.Caching.IGoalsCache, GoalFlow.Infrastructure.Caching.GoalsCache>();

        // --------------------------------------------------------------------
        // 5) Hangfire (background processing)
        //    - Disabled in Test environment to keep tests fast/isolated.
        //    - Uses SQL Server storage with safe defaults for small-to-mid loads.
        // --------------------------------------------------------------------
        if (!env.IsEnvironment("Test"))
        {
            var sqlCs = config.GetConnectionString("Sql")!;
            services.AddHangfire(h => h
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(sqlCs, new SqlServerStorageOptions
                {
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.FromSeconds(15)
                }));

            services.AddHangfireServer();
        }

        // --------------------------------------------------------------------
        // 6) Background processors (DI-scoped services invoked by jobs/hosted services)
        // --------------------------------------------------------------------
        services.AddScoped<ReminderProcessor>();

        // --------------------------------------------------------------------
        // 7) Identity (Core) with EF stores
        //    - Unique emails; short but non-trivial password policy for MVP.
        // --------------------------------------------------------------------
        services.AddIdentityCore<IdentityUser>(o =>
        {
            o.User.RequireUniqueEmail = true;
            o.Password.RequiredLength = 6;
        })
            .AddEntityFrameworkStores<GoalFlowDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return services;
    }
}
