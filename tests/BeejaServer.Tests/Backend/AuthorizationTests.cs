using System.Net;
using System.Net.Http.Json;
using BeejaServer.Controllers;

namespace BeejaServer.Tests.Backend;

public class AuthorizationTests
{
    [Theory]
    [Trait("Category", "Regression")]
    [InlineData("GET", "/api/v1/User/me")]
    [InlineData("GET", "/api/v1/User/profile-data")]
    [InlineData("POST", "/api/v1/User/add-points")]
    [InlineData("PUT", "/api/v1/User/update-username")]
    [InlineData("POST", "/api/v1/User/upload-avatar")]
    public async Task ProtectedEndpoints_RejectAnonymousRequests(string method, string path)
    {
        using var factory = new BeejaApplicationFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (path.EndsWith("/upload-avatar", StringComparison.Ordinal))
        {
            var multipart = new MultipartFormDataContent();
            multipart.Add(new ByteArrayContent([1]), "file", "avatar.jpg");
            request.Content = multipart;
        }
        else if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { points = 1, username = "updated-user" });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "SecurityContract")]
    public async Task AddPointsByUsername_RejectsAnonymousCaller()
    {
        using var factory = new BeejaApplicationFactory();
        await factory.AddUserAsync(BeejaApplicationFactory.CreateUser());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/User/add-points-by-username", new AddPointsByUsernameDto
        {
            Username = "existing-user",
            Points = 1000
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var user = await factory.GetUserByEmailAsync("existing@example.test");
        Assert.Equal(0, user.TotalPoints);
    }

    [Fact]
    [Trait("Category", "SecurityContract")]
    public async Task ConfirmEmail_RejectsRequestWithoutSingleUseConfirmationToken()
    {
        using var factory = new BeejaApplicationFactory();
        var user = BeejaApplicationFactory.CreateUser();
        user.IsEmailConfirmed = false;
        await factory.AddUserAsync(user);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/User/confirm-email?email=existing%40example.test");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var storedUser = await factory.GetUserByEmailAsync("existing@example.test");
        Assert.False(storedUser.IsEmailConfirmed);
    }

    [Fact]
    [Trait("Category", "SecurityContract")]
    public async Task OrdinaryUser_CannotAwardPointsToSelf()
    {
        using var factory = new BeejaApplicationFactory();
        await factory.AddUserAsync(BeejaApplicationFactory.CreateUser());
        using var client = factory.CreateClient();
        await BeejaApplicationFactory.AuthenticateAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/User/add-points", new AddPointsDto { Points = 1000 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var user = await factory.GetUserByEmailAsync("existing@example.test");
        Assert.Equal(0, user.TotalPoints);
    }
}
