using BeejaServer.Services;

namespace BeejaServer.Tests.Backend;

[Trait("Category", "SecurityContract")]
public class LevelSecurityContractTests
{
    [Fact]
    public void Progress_NeverBecomesNegative()
    {
        Assert.Equal(0, LevelService.CalculateProgressPercentage(-1));
    }
}
