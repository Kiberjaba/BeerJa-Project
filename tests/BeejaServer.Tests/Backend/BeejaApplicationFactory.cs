using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeejaServer.Data;
using BeejaServer.DTOs;
using BeejaServer.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BeejaServer.Tests.Backend;

internal sealed class BeejaApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"beerja-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestOnlyKey_MustBeAtLeast32BytesLong_1234567890",
                ["Jwt:Issuer"] = "BeejaServer.Tests",
                ["Jwt:Audience"] = "BeejaServer.Tests.Client"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task AddUserAsync(User user)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    public async Task<List<User>> GetUsersAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Users.AsNoTracking().OrderBy(user => user.UserId).ToListAsync();
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Users.AsNoTracking().SingleAsync(user => user.Email == email);
    }

    public static User CreateUser(
        string username = "existing-user",
        string email = "existing@example.test",
        string password = "Correct-Horse-42",
        int totalPoints = 0,
        bool oauthOnly = false)
    {
        return new User
        {
            Username = username,
            Email = email,
            PasswordHash = oauthOnly ? string.Empty : BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
            IsEmailConfirmed = true,
            TotalPoints = totalPoints,
            Level = 1,
            CreatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    public static async Task AuthenticateAsync(
        HttpClient client,
        string login = "existing@example.test",
        string password = "Correct-Horse-42")
    {
        var response = await client.PostAsJsonAsync("/api/v1/User/login", new LoginDto
        {
            LoginOrEmail = login,
            Password = password
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
    }
}
