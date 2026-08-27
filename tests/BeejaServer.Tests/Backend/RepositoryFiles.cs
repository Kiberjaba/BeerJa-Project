namespace BeejaServer.Tests.Backend;

internal static class RepositoryFiles
{
    public static string Root()
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
