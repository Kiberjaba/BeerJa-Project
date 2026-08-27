using BeejaServer.Services;

namespace BeejaServer.Tests.Backend;

[Trait("Category", "Regression")]
public class LevelServiceTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 100)]
    [InlineData(3, 250)]
    [InlineData(4, 475)]
    [InlineData(5, 812)]
    [InlineData(6, 1318)]
    public void RequiredPoints_AreCumulativeThresholds(int level, int expected)
    {
        Assert.Equal(expected, LevelService.GetRequiredPointsForLevel(level));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(249, 2)]
    [InlineData(250, 3)]
    [InlineData(811, 4)]
    [InlineData(812, 5)]
    public void CalculateLevel_ChangesOnlyAtThreshold(int totalPoints, int expected)
    {
        Assert.Equal(expected, LevelService.CalculateLevel(totalPoints));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 50)]
    [InlineData(100, 0)]
    [InlineData(175, 50)]
    [InlineData(249, 99.33)]
    public void Progress_IsRelativeToCurrentLevel(int totalPoints, double expected)
    {
        Assert.Equal(expected, LevelService.CalculateProgressPercentage(totalPoints));
    }

    [Fact]
    public void RequiredPoints_AreStrictlyIncreasingAcrossSupportedRange()
    {
        var previous = LevelService.GetRequiredPointsForLevel(1);

        for (var level = 2; level <= 20; level++)
        {
            var current = LevelService.GetRequiredPointsForLevel(level);
            Assert.True(current > previous, $"Threshold for level {level} must be greater than level {level - 1}.");
            previous = current;
        }
    }
}
