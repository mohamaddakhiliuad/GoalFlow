// ---------------------------------------------------------------------------------------------------------------------
// GoalFlow API bootstrap (Minimal APIs / Program.cs)
// - Configures logging (Serilog), authentication (JWT), authorization (policies), CORS, rate limiting, ProblemDetails,
//   Swagger, SignalR, MediatR, Hangfire, and infrastructure services.
// - Exposes endpoints for Auth, Goals, Progress Logs, Reminders, Health, and a SignalR hub.
// - Style: concise, production-leaning defaults (short-lived access tokens, fixed-window rate limits, CORS policy).
// ---------------------------------------------------------------------------------------------------------------------

using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using GoalFlow.Api.Auth;
using GoalFlow.Api.Hubs;
using GoalFlow.Api.Notifications;
using GoalFlow.Api.Services;
using GoalFlow.Application.Common;
using GoalFlow.Application.Goals;
using GoalFlow.Infrastructure;
using GoalFlow.Infrastructure.Reminders;
using Hangfire;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authorization; // for policy registration
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using GoalFlow.Api.Contracts;
using Serilog;
using Serilog.Enrichers.Span;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------
// Logging (Serilog) — console sink with span IDs
// ---------------------------------------------
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithSpan() // Adds TraceId/SpanId if OTel is added later
    .WriteTo.Console() // Consider JSON formatting in production
    .CreateLogger();

builder.Host.UseSerilog();

// ---------------------------------------------
// JWT configuration
// ---------------------------------------------
var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

// ---------------------------------------------
// Core services
// ---------------------------------------------
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(1) // keep tokens tight
        };
    });

// Authorization + policy (OwnerGoal) for per-resource ownership checks
builder.Services.AddAuthorization(options =>
{
    // Registers a policy used on routes like /api/goals/{id} to ensure the caller owns the goal
    options.AddPolicy("OwnerGoal", policy => policy.Requirements.Add(new MustOwnGoal()));
});

// Register the handler that enforces MustOwnGoal
builder.Services.AddSingleton<IAuthorizationHandler, MustOwnGoalHandler>();

// ---------------------------------------------
// CORS — allow local dev clients and Swagger UI
// ---------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("client", b => b
        .WithOrigins("http://localhost:5500", "http://127.0.0.1:5500", "http://localhost:5274")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ---------------------------------------------
// Rate limiting — general vs. login-specific
// ---------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------------------------------------------
// ProblemDetails — consistent API error responses
// ---------------------------------------------
Hellang.Middleware.ProblemDetails.ProblemDetailsExtensions.AddProblemDetails(builder.Services, options =>
{
    options.Map<FluentValidation.ValidationException>(ex =>
        new StatusCodeProblemDetails(StatusCodes.Status400BadRequest)
        {
            Title = "Validation failed",
            Detail = string.Join(" | ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))
        });

    options.Map<KeyNotFoundException>(ex =>
        new StatusCodeProblemDetails(StatusCodes.Status404NotFound)
        {
            Title = "Not found",
            Detail = ex.Message
        });

    options.MapToStatusCode<UnauthorizedAccessException>(StatusCodes.Status401Unauthorized);
    options.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
});

// ---------------------------------------------
// Swagger — with global Bearer auth requirement
// ---------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GoalFlow API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste only the JWT (no 'Bearer ' prefix)."
    };

    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ---------------------------------------------
// App infrastructure (EF Core, Redis, etc.)
// ---------------------------------------------
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// MediatR — scan API assembly for notification handlers (SignalR dispatch, etc.)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProgressLogCreatedHandler).Assembly));

// Token service — issues/rotates access & refresh tokens
builder.Services.AddScoped<TokenService>();

var app = builder.Build();

// ---------------------------------------------
// HTTP pipeline
// ---------------------------------------------
app.UseProblemDetails();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("client");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard (protect in production)
app.UseHangfireDashboard("/hangfire");

// Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ---------------------------------------------
// Real-time hub
// ---------------------------------------------
app.MapHub<ProgressHub>("/hubs/progress").RequireCors("client");

// ---------------------------------------------
// Health probes
// ---------------------------------------------
app.MapGet("/health/live", () => Results.Ok(new { status = "live" })).WithTags("Health");
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" })).WithTags("Health");

// ---------------------------------------------
// AUTH endpoints
// ---------------------------------------------
app.MapPost("/api/auth/register", async (UserDto dto, UserManager<IdentityUser> users, TokenService tokens) =>
{
    var u = new IdentityUser { UserName = dto.Email, Email = dto.Email };
    var res = await users.CreateAsync(u, dto.Password);
    if (!res.Succeeded) return Results.BadRequest(res.Errors);

    var (access, refresh) = tokens.IssueTokens(u);
    return Results.Ok(new { accessToken = access, refreshToken = refresh });
})
.WithTags("Auth");

app.MapPost("/api/auth/login", async (UserDto dto, UserManager<IdentityUser> users, TokenService tokens) =>
{
    var u = await users.FindByEmailAsync(dto.Email);
    if (u is null) return Results.Unauthorized();

    var ok = await users.CheckPasswordAsync(u, dto.Password);
    if (!ok) return Results.Unauthorized();

    var (access, refresh) = tokens.IssueTokens(u);
    return Results.Ok(new { accessToken = access, refreshToken = refresh });
})
.RequireRateLimiting("login")
.WithTags("Auth");

app.MapPost("/api/auth/refresh", async (RefreshDto dto, UserManager<IdentityUser> users, TokenService tokens) =>
{
    var u = await users.FindByIdAsync(dto.UserId);
    if (u is null) return Results.Unauthorized();

    return tokens.ValidateAndRotate(dto.RefreshToken, u, out var access, out var refresh)
        ? Results.Ok(new { accessToken = access, refreshToken = refresh })
        : Results.Unauthorized();
})
.WithTags("Auth");

// Returns the caller's subject and email based on validated JWT claims
app.MapGet("/api/me",
    [Microsoft.AspNetCore.Authorization.Authorize]
(ClaimsPrincipal me) => Results.Ok(new
{
    userId = me.FindFirstValue("uid"),
    email = me.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)
}))
.WithTags("Auth");

// ---------------------------------------------
// GOALS endpoints
// ---------------------------------------------

// Get goal by id (ownership enforced by OwnerGoal policy)
app.MapGet("/api/goals/{id:guid}",
    [Microsoft.AspNetCore.Authorization.Authorize]
async (Guid id, MediatR.IMediator mediator, ClaimsPrincipal me) =>
    {
        var userId = me.GetUserIdOrThrow();
        var dto = await mediator.Send(new GetGoalByIdQuery(id, userId));
        return dto is not null ? Results.Ok(dto) : Results.NotFound();
    })
.RequireAuthorization("OwnerGoal")
.WithTags("Goals")
.WithOpenApi();

// List goals (paged with optional filters)
app.MapGet("/api/goals",
    [Microsoft.AspNetCore.Authorization.Authorize]
async (int page, int pageSize, string? search, string? status, string? priority, MediatR.IMediator mediator, ClaimsPrincipal me) =>
    {
        var userId = me.GetUserIdOrThrow();
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is <= 0 or > 100 ? 20 : pageSize;

        var result = await mediator.Send(new GetGoalsQuery(userId, page, pageSize, search, status, priority));
        return Results.Ok(result);
    })
.RequireRateLimiting("fixed")
.WithTags("Goals");

// Update goal (ownership enforced)
app.MapPut("/api/goals/{id:guid}",
    [Microsoft.AspNetCore.Authorization.Authorize]
async (Guid id, UpdateGoalBody body, MediatR.IMediator mediator, ClaimsPrincipal me) =>
    {
        var userId = me.GetUserIdOrThrow();
        var cmd = new UpdateGoalCommand(
            id, userId, body.Title, body.Specific, body.Measurable, body.Achievable, body.Relevant,
            body.TimeBound, body.Description, body.Priority, body.Status);

        var res = await mediator.Send(cmd);
        return res.IsSuccess ? Results.NoContent() : Results.BadRequest(res.Error);
    })
.RequireAuthorization("OwnerGoal")
.WithTags("Goals")
.WithOpenApi();

// Delete goal (ownership enforced)
app.MapDelete("/api/goals/{id:guid}",
    [Microsoft.AspNetCore.Authorization.Authorize]
async (Guid id, MediatR.IMediator mediator, ClaimsPrincipal me) =>
    {
        var userId = me.GetUserIdOrThrow();
        var res = await mediator.Send(new DeleteGoalCommand(id, userId));
        return res.IsSuccess ? Results.NoContent() : Results.BadRequest(res.Error);
    })
.RequireAuthorization("OwnerGoal")
.WithTags("Goals")
.WithOpenApi();

// ---------------------------------------------
// PROGRESS LOGS endpoints
// ---------------------------------------------

// Create a progress log entry for a goal
app.MapPost("/api/goals/{id:guid}/progress-logs",
    [Microsoft.AspNetCore.Authorization.Authorize]
async (Guid id, CreateProgressBody body, MediatR.IMediator mediator, ClaimsPrincipal me) =>
    {
        var userId = me.GetUserIdOrThrow();
        var cmd = new CreateProgressLogCommand(id, userId, body.Delta, body.Note);
        var res = await mediator.Send(cmd);

        return res.IsSuccess
            ? Results.Created($"/api/goals/{id}/progress-logs/{res.Value}", new { id = res.Value })
            : Results.BadRequest(res.Error);
    })
.RequireRateLimiting("fixed")
.WithTags("Goals");

// Get paged progress logs for a goal
app.MapGet("/api/goals/{id:guid}/progress-logs",
    [Microsoft.AspNetCore.Authorization.Authorize]
async (Guid id, int page, int pageSize, MediatR.IMediator mediator, ClaimsPrincipal me) =>
    {
        // Optional: enforce ownership for read as well, if required by business rules
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is <= 0 or > 200 ? 50 : pageSize;

        var list = await mediator.Send(new GetProgressLogsQuery(id, page, pageSize));
        return Results.Ok(list);
    })
.RequireRateLimiting("fixed")
.WithTags("Goals");

// Create a new goal
app.MapPost("/api/goals",
    [Microsoft.AspNetCore.Authorization.Authorize]
async (CreateGoalBody body, MediatR.IMediator mediator, ClaimsPrincipal me) =>
    {
        var userId = me.GetUserIdOrThrow();

        if (!Enum.TryParse<GoalFlow.Domain.Entities.GoalPriority>(body.Priority, true, out var priority))
            return Results.BadRequest(new { error = "Invalid priority. Use Low|Medium|High." });

        var cmd = new CreateGoalCommand(
            userId,
            body.Title, body.Specific, body.Measurable, body.Achievable, body.Relevant,
            body.TimeBound, body.Description, priority);

        var result = await mediator.Send(cmd);

        return result.IsSuccess
            ? Results.Created($"/api/goals/{result.Value}", new { id = result.Value })
            : Results.BadRequest(result.Error);
    })
.WithTags("Goals")
.RequireAuthorization() // explicit
.RequireRateLimiting("fixed")
.Accepts<CreateGoalBody>("application/json")
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status429TooManyRequests)
.WithOpenApi();

// ---------------------------------------------
// REMINDERS endpoints
// ---------------------------------------------

// Create a reminder (processed by Hangfire recurring job)
app.MapPost("/api/reminders",
    [Microsoft.AspNetCore.Authorization.Authorize]
async (CreateReminderCommand cmd, MediatR.IMediator mediator, ClaimsPrincipal me) =>
    {
        // Optional: verify goal ownership inside the command handler as needed
        var res = await mediator.Send(cmd);
        return res.IsSuccess
            ? Results.Created($"/api/reminders/{res.Value}", new { id = res.Value })
            : Results.BadRequest(res.Error);
    })
.RequireRateLimiting("fixed")
.WithTags("Reminders");

// Debug claims endpoint (remove/secure in production)
app.MapGet("/_debug/claims",
    [Microsoft.AspNetCore.Authorization.Authorize]
(ClaimsPrincipal me) => Results.Ok(me.Claims.Select(c => new { c.Type, c.Value })))
.WithTags("Auth");

// ---------------------------------------------
// Background jobs (Hangfire)
// ---------------------------------------------
RecurringJob.AddOrUpdate<ReminderProcessor>("reminders-processor", x => x.ProcessDueReminders(), "*/1 * * * *");

app.Run();

// ---------------------------------------------
// Local DTOs (kept here for sample completeness)
// ---------------------------------------------
public record UserDto(string Email, string Password);
public record RefreshDto(string UserId, string RefreshToken);
public record UpdateGoalBody(
    string Title, string Specific, string Measurable, string Achievable, string Relevant,
    DateTimeOffset TimeBound, string? Description, string Priority, string Status);
