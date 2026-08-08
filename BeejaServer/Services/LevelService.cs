namespace BeejaServer.Services
{
    public static class LevelService
    {
        private const int BasePoints = 100; //порог 2 уровня
        private const double Multiplier = 1.5; //множитель прогресс

        public static int GetRequiredPointsForLevel(int level)
        {
            if (level <= 1) return 0;

            double totalRequired = 0;
            double currentLevelCost = BasePoints;

            for (int i = 1; i < level; i++)
            {
                totalRequired += currentLevelCost;
                currentLevelCost *= Multiplier;
            }

            return (int)Math.Floor(totalRequired);
        }

        public static int CalculateLevel(int totalPoints)
        {
            if (totalPoints <= 0) return 1;

            int level = 1;
            while (totalPoints >= GetRequiredPointsForLevel(level + 1))
            {
                level++;
            }

            return level;
        }
        public static double CalculateProgressPercentage(int totalPoints)
        {
            int currentLevel = CalculateLevel(totalPoints);
            int currentLevelPoints = GetRequiredPointsForLevel(currentLevel);
            int nextLevelPoints = GetRequiredPointsForLevel(currentLevel + 1);

            int pointsInCurrentLevel = totalPoints - currentLevelPoints;
            int pointsNeededForNextLevel = nextLevelPoints - currentLevelPoints;

            if (pointsNeededForNextLevel <= 0) return 100.0;

            double percentage = (double)pointsInCurrentLevel / pointsNeededForNextLevel * 100;
            return Math.Round(percentage, 2);
        }
    }
}