namespace BeejaServer.Tests;

public class StaticFrontendTests
{
    private static readonly string[] RequiredAppFiles =
    [
        "index.html",
        "app.js",
        "backend-bridge.js",
        "data.js",
        "state-machine.js",
        "tour.js",
        "screens-player.js",
        "screens-organizer.js",
        "screens-organizer-editor.js",
        "screens-host.js",
        "screens-public.js",
        "styles.css",
        "forms-and-overlays.css",
        "colors_and_type.css",
        "bootstrap.js",
        "product-app.js",
        "product-data.js",
        "product-contracts.js",
        "product-ui.js",
        "router.js",
        "app-shell.js",
        "screens-public-site.js",
        "screens-auth.js",
        "screens-user-account.js",
        "screens-host-account.js",
        "screens-mechanics.js",
        "screens-order.js",
        "screens-analytics.js",
        "product.css"
    ];

    [Fact]
    public void LiveSignalApp_HasAllRuntimeFiles()
    {
        var appRoot = Path.Combine(RepositoryRoot(), "BeejaServer", "wwwroot", "app");

        foreach (var file in RequiredAppFiles)
        {
            Assert.True(File.Exists(Path.Combine(appRoot, file)), $"Missing frontend file: {file}");
        }
    }

    [Fact]
    public void RootPage_OpensBeerJaProduct()
    {
        var rootIndex = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "BeejaServer", "wwwroot", "index.html"));

        Assert.Contains("url=/app/", rootIndex);
        Assert.DoesNotContain("url=/app/?tour=1", rootIndex);
    }

    [Fact]
    public void App_UsesBackendBridgeAndLocalAssetPaths()
    {
        var repositoryRoot = RepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "BeejaServer", "wwwroot", "app");
        var appSource = File.ReadAllText(Path.Combine(appRoot, "app.js"));
        var dataSource = File.ReadAllText(Path.Combine(appRoot, "data.js"));

        Assert.Contains("./backend-bridge.js", appSource);
        Assert.DoesNotContain("../../../assets", dataSource);
        Assert.Contains("../assets/generated/", dataSource);
        Assert.True(File.Exists(Path.Combine(
            repositoryRoot,
            "BeejaServer",
            "wwwroot",
            "assets",
            "generated",
            "quiz-entry-qr-v1.svg")));
    }

    [Fact]
    public void ProductFrontend_HasApiBoundaryAndPreservesTour()
    {
        var appRoot = Path.Combine(RepositoryRoot(), "BeejaServer", "wwwroot", "app");
        var bootstrap = File.ReadAllText(Path.Combine(appRoot, "bootstrap.js"));
        var contracts = File.ReadAllText(Path.Combine(appRoot, "product-contracts.js"));
        var bridge = File.ReadAllText(Path.Combine(appRoot, "backend-bridge.js"));

        Assert.Contains("import(\"./app.js\")", bootstrap);
        Assert.Contains("import(\"./product-app.js\")", bootstrap);
        Assert.Contains("integrationMode === \"api\"", contracts);
        Assert.Contains("createOrder", bridge);
        Assert.Contains("getHostAnalytics", bridge);
    }

    private static string RepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Beerja.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate BeerJa repository root.");
    }
}
