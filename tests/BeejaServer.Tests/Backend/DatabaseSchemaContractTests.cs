using System.Text.RegularExpressions;

namespace BeejaServer.Tests.Backend;

[Trait("Category", "DatabaseContract")]
public class DatabaseSchemaContractTests
{
    [Fact]
    public void SqlBootstrap_UsesSameDatabaseNameAsApplication()
    {
        var root = RepositoryFiles.Root();
        var sql = File.ReadAllText(Path.Combine(root, "beeja.sql"));
        var settings = File.ReadAllText(Path.Combine(root, "BeejaServer", "appsettings.json"));

        Assert.Contains("Database=beeja;", settings, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(new Regex(@"Database=beeja;", RegexOptions.IgnoreCase), settings);
        Assert.Contains("База данных: `beeja`", sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("users", "user_name")]
    [InlineData("users", "email")]
    [InlineData("users", "password_hash")]
    [InlineData("users", "is_email_confirmed")]
    [InlineData("users", "total_points")]
    [InlineData("users", "level")]
    [InlineData("users", "created_at")]
    [InlineData("events", "event_id")]
    [InlineData("event_checkins", "checkin_id")]
    public void SqlBootstrap_ContainsColumnsRequiredByEfModel(string table, string column)
    {
        var sql = File.ReadAllText(Path.Combine(RepositoryFiles.Root(), "beeja.sql"));
        var tableStart = sql.IndexOf($"CREATE TABLE `{table}`", StringComparison.OrdinalIgnoreCase);

        Assert.True(tableStart >= 0, $"Missing table {table}.");
        var tableEnd = sql.IndexOf(";", tableStart, StringComparison.Ordinal);
        Assert.True(tableEnd > tableStart, $"Incomplete table {table} definition.");
        var definition = sql[tableStart..tableEnd];
        Assert.Contains($"`{column}`", definition, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migrations_CanCreateRequiredTablesFromEmptyDatabase()
    {
        var migrationsRoot = Path.Combine(RepositoryFiles.Root(), "BeejaServer", "Migrations");
        var migrations = Directory.EnumerateFiles(migrationsRoot, "*.cs")
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText)
            .ToArray();

        Assert.Contains(migrations, source => source.Contains("CreateTable(", StringComparison.Ordinal));
        Assert.Contains(migrations, source => source.Contains("name: \"users\"", StringComparison.Ordinal));
        Assert.Contains(migrations, source => source.Contains("name: \"events\"", StringComparison.Ordinal));
        Assert.Contains(migrations, source => source.Contains("name: \"event_checkins\"", StringComparison.Ordinal));
    }

    [Fact]
    public void MigrationSnapshot_DoesNotContainRemovedDomainModels()
    {
        var snapshot = File.ReadAllText(Path.Combine(
            RepositoryFiles.Root(),
            "BeejaServer",
            "Migrations",
            "AppDbContextModelSnapshot.cs"));

        Assert.DoesNotContain("BeejaServer.Models.Order", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("BeejaServer.Models.Product", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("BeejaServer.Models.PriceHistory", snapshot, StringComparison.Ordinal);
    }
}
