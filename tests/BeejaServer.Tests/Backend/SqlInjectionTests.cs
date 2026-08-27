using System.Net;
using System.Net.Http.Json;
using BeejaServer.Data;
using BeejaServer.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BeejaServer.Tests.Backend;

[Trait("Category", "Regression")]
public class SqlInjectionTests
{
    private static readonly string[] InjectionPayloads =
    [
        "' OR 1=1 --",
        "\" OR \"1\"=\"1",
        "admin@example.test' --",
        "x'; DROP TABLE users; --",
        "' UNION SELECT 1,2,3 --"
    ];

    [Fact]
    public async Task LoginPayloads_NeverAuthenticateOrChangeUsers()
    {
        using var factory = new BeejaApplicationFactory();
        await factory.AddUserAsync(BeejaApplicationFactory.CreateUser());
        using var client = factory.CreateClient();

        foreach (var payload in InjectionPayloads)
        {
            var response = await client.PostAsJsonAsync("/api/v1/User/login", new LoginDto
            {
                LoginOrEmail = payload,
                Password = payload
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.DoesNotContain("token", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        var passwordResponse = await client.PostAsJsonAsync("/api/v1/User/login", new LoginDto
        {
            LoginOrEmail = "existing@example.test",
            Password = "' OR 1=1 --"
        });

        Assert.Equal(HttpStatusCode.BadRequest, passwordResponse.StatusCode);
        Assert.Single(await factory.GetUsersAsync());
    }

    [Fact]
    public async Task RegistrationPayload_IsStoredAsDataAndCannotAlterExistingRows()
    {
        using var factory = new BeejaApplicationFactory();
        await factory.AddUserAsync(BeejaApplicationFactory.CreateUser());
        using var client = factory.CreateClient();
        const string payload = "x'; DROP TABLE users; --";

        var response = await client.PostAsJsonAsync("/api/v1/User/register", new RegisterDto
        {
            Username = payload,
            Email = "injection@example.test",
            Password = "Correct-Horse-43"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var users = await factory.GetUsersAsync();
        Assert.Equal(2, users.Count);
        Assert.Contains(users, user => user.Email == "existing@example.test");
        Assert.Contains(users, user => user.Username == payload);
    }

    [Fact]
    public void LoginQuery_EscapesQuoteThatWouldTerminateSqlLiteral()
    {
        var payload = InjectionPayloads[0];
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                "Server=localhost;Database=unused;User=test;Password=test;",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;
        using var context = new AppDbContext(options);

        var sql = BuildLoginQuery(context, payload).ToQueryString();

        var selectIndex = sql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
        Assert.True(selectIndex >= 0);
        var commandText = sql[selectIndex..];
        Assert.Contains("= ''' OR 1=1 --'", commandText, StringComparison.Ordinal);
        Assert.DoesNotContain("= '' OR 1=1 --", commandText, StringComparison.Ordinal);
    }

    private static IQueryable<BeejaServer.Models.User> BuildLoginQuery(AppDbContext context, string input)
    {
        return context.Users.Where(user =>
            user.Email.ToLower() == input || user.Username.ToLower() == input);
    }

    [Fact]
    public void BackendSource_DoesNotUseRawSqlExecutionApis()
    {
        var backendRoot = Path.Combine(RepositoryFiles.Root(), "BeejaServer");
        var forbiddenApis = new[]
        {
            "FromSqlRaw(",
            "ExecuteSqlRaw(",
            "SqlQueryRaw(",
            "ExecuteSqlCommand("
        };

        foreach (var file in Directory.EnumerateFiles(backendRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            foreach (var api in forbiddenApis)
            {
                Assert.DoesNotContain(api, source, StringComparison.Ordinal);
            }
        }
    }
}
