using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;

namespace GoalFlow.Tests.Integration;

public class AuthAndGoalsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthAndGoalsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    record UserDto(string Email, string Password);
    record RefreshDto(string UserId, string RefreshToken);
    record LoginRes(string accessToken, string refreshToken);

    record CreateGoalBody(string Title, string Specific, string Measurable, string Achievable, string Relevant,
        DateTimeOffset TimeBound, string? Description, string Priority);

    [Fact]
    public async Task EndToEnd_Auth_CreateAndListGoals()
    {
        // Register
        var reg = await _client.PostAsJsonAsync("/api/auth/register", new UserDto("t1@goalflow.local", "P@ssw0rd!"));
        reg.EnsureSuccessStatusCode();

        // Login
        var login = await _client.PostAsJsonAsync("/api/auth/login", new UserDto("t1@goalflow.local", "P@ssw0rd!"));
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<LoginRes>();
        tokens.Should().NotBeNull();
        var access = tokens!.accessToken;

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access);

        // Create Goal
        var body = new CreateGoalBody(
            "Test Goal", "S", "M", "A", "R",
            DateTimeOffset.UtcNow.AddDays(30), "integration", "High");

        var post = await _client.PostAsJsonAsync("/api/goals", body);
        post.EnsureSuccessStatusCode();

        // List Goals
        var get = await _client.GetAsync("/api/goals?page=1&pageSize=10");
        get.EnsureSuccessStatusCode();

        var payload = await get.Content.ReadAsStringAsync();
        payload.Should().Contain("Test Goal");
    }
}
