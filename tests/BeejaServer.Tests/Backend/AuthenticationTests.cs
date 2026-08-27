using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BeejaServer.DTOs;

namespace BeejaServer.Tests.Backend;

[Trait("Category", "Regression")]
public class AuthenticationTests
{
    [Fact]
    public async Task Registration_NormalizesDataAndNeverStoresPlaintextPassword()
    {
        using var factory = new BeejaApplicationFactory();
        using var client = factory.CreateClient();
        const string password = "Correct-Horse-42";

        var response = await client.PostAsJsonAsync("/api/v1/User/register", new RegisterDto
        {
            Username = "  new-user  ",
            Email = "  New.User@Example.Test  ",
            Password = password
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var user = await factory.GetUserByEmailAsync("new.user@example.test");
        Assert.Equal("new-user", user.Username);
        Assert.NotEqual(password, user.PasswordHash);
        Assert.DoesNotContain(password, user.PasswordHash, StringComparison.Ordinal);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(password, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_WithCorrectCredentialsReturnsJwtAcceptedByProtectedEndpoint()
    {
        using var factory = new BeejaApplicationFactory();
        await factory.AddUserAsync(BeejaApplicationFactory.CreateUser());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/User/login", new LoginDto
        {
            LoginOrEmail = " EXISTING@EXAMPLE.TEST ",
            Password = "Correct-Horse-42"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(payload);
        Assert.True(new JwtSecurityTokenHandler().CanReadToken(payload.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Token);
        var profileResponse = await client.GetAsync("/api/v1/User/me");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPasswordNeverReturnsToken()
    {
        using var factory = new BeejaApplicationFactory();
        await factory.AddUserAsync(BeejaApplicationFactory.CreateUser());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/User/login", new LoginDto
        {
            LoginOrEmail = "existing@example.test",
            Password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Registration_DuplicateNormalizedEmailIsRejected()
    {
        using var factory = new BeejaApplicationFactory();
        await factory.AddUserAsync(BeejaApplicationFactory.CreateUser());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/User/register", new RegisterDto
        {
            Username = "another-user",
            Email = " EXISTING@EXAMPLE.TEST ",
            Password = "Correct-Horse-43"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(await factory.GetUsersAsync());
    }
}
