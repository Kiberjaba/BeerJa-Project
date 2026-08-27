using System.Text.Json;

namespace BeejaServer.Tests.Backend;

[Trait("Category", "SecurityContract")]
public class ConfigurationSecurityTests
{
    [Fact]
    public void AppSettings_DoesNotContainJwtSigningKey()
    {
        using var settings = ReadAppSettings();
        var jwt = settings.RootElement.GetProperty("Jwt");

        Assert.False(jwt.TryGetProperty("Key", out _));
    }

    [Fact]
    public void AppSettings_DoesNotContainYandexClientSecret()
    {
        using var settings = ReadAppSettings();
        var yandex = settings.RootElement.GetProperty("Yandex");

        Assert.False(yandex.TryGetProperty("ClientSecret", out _));
    }

    [Fact]
    public void DefaultDatabaseConnection_DoesNotUseRootOrBlankPassword()
    {
        using var settings = ReadAppSettings();
        var connection = settings.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString();

        Assert.NotNull(connection);
        Assert.DoesNotContain("User=root", connection, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=;", connection, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionCors_DoesNotAllowEveryOrigin()
    {
        var program = File.ReadAllText(Path.Combine(RepositoryFiles.Root(), "BeejaServer", "Program.cs"));

        Assert.DoesNotContain("AllowAnyOrigin()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void OAuthCallback_DoesNotPutAccessTokenIntoUrlQuery()
    {
        var controller = File.ReadAllText(Path.Combine(
            RepositoryFiles.Root(),
            "BeejaServer",
            "Controllers",
            "UserController.cs"));

        Assert.DoesNotContain("?token=", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiResponses_DoNotExposeExceptionMessages()
    {
        var controller = File.ReadAllText(Path.Combine(
            RepositoryFiles.Root(),
            "BeejaServer",
            "Controllers",
            "UserController.cs"));

        Assert.DoesNotContain("error = ex.Message", controller, StringComparison.Ordinal);
    }

    private static JsonDocument ReadAppSettings()
    {
        var path = Path.Combine(RepositoryFiles.Root(), "BeejaServer", "appsettings.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}
