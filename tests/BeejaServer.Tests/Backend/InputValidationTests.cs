using System.Net;
using System.Net.Http.Json;
using System.Text;
using BeejaServer.Controllers;
using BeejaServer.DTOs;

namespace BeejaServer.Tests.Backend;

public class InputValidationTests
{
    [Theory]
    [Trait("Category", "Regression")]
    [InlineData("ab", "valid@example.test", "long-enough")]
    [InlineData("valid-user", "not-an-email", "long-enough")]
    [InlineData("valid-user", "valid@example.test", "12345")]
    public async Task Registration_InvalidModelIsRejected(string username, string email, string password)
    {
        using var factory = new BeejaApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/User/register", new RegisterDto
        {
            Username = username,
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await factory.GetUsersAsync());
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task MalformedJsonIsRejectedWithoutChangingDatabase()
    {
        using var factory = new BeejaApplicationFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent("{\"username\":", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/User/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await factory.GetUsersAsync());
    }

    [Fact]
    [Trait("Category", "SecurityContract")]
    public async Task UpdateUsername_RejectsValueLongerThanDatabaseContract()
    {
        using var factory = new BeejaApplicationFactory();
        await factory.AddUserAsync(BeejaApplicationFactory.CreateUser());
        using var client = factory.CreateClient();
        await BeejaApplicationFactory.AuthenticateAsync(client);

        var response = await client.PutAsJsonAsync("/api/v1/User/update-username", new UpdateUsernameDto
        {
            Username = new string('a', 51)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "SecurityContract")]
    public async Task UploadAvatar_RejectsNonImageWithoutLeakingExceptionDetails()
    {
        using var factory = new BeejaApplicationFactory();
        await factory.AddUserAsync(BeejaApplicationFactory.CreateUser());
        using var client = factory.CreateClient();
        await BeejaApplicationFactory.AuthenticateAsync(client);
        using var multipart = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Encoding.UTF8.GetBytes("this is not an image"));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        multipart.Add(file, "file", "avatar.txt");

        var response = await client.PostAsync("/api/v1/User/upload-avatar", multipart);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType });
        Assert.DoesNotContain("\"error\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "SecurityContract")]
    public async Task AddPoints_RejectsIntegerOverflow()
    {
        using var factory = new BeejaApplicationFactory();
        await factory.AddUserAsync(BeejaApplicationFactory.CreateUser(totalPoints: int.MaxValue - 5));
        using var client = factory.CreateClient();
        await BeejaApplicationFactory.AuthenticateAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/User/add-points", new AddPointsDto { Points = 10 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var user = await factory.GetUserByEmailAsync("existing@example.test");
        Assert.Equal(int.MaxValue - 5, user.TotalPoints);
    }
}
