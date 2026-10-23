using UnityEngine;

namespace TypeDrive.Core
{
    /// <summary>Settings and visual identity for each selectable campaign stage.</summary>
    public sealed class GameLevelDefinition
    {
        public readonly int Index;
        public readonly string DisplayName;
        public readonly string Objective;
        public readonly int WordGoal;
        public readonly WordBank.Difficulty Difficulty;
        public readonly float StartingSpeed;
        public readonly float GateSeconds;
        public readonly float HazardSpacing;
        public readonly Color FogColor;
        public readonly Color SkyTint;
        public readonly Color AmbientColor;
        public readonly Color RoadGlow;
        public readonly Color LaneMarking;
        public readonly Color SkylineColor;
        public readonly Color WindowGlow;

        public GameLevelDefinition(int index, string displayName, string objective, int wordGoal,
            WordBank.Difficulty difficulty, float startingSpeed, float gateSeconds, float hazardSpacing,
            Color fogColor, Color skyTint, Color ambientColor, Color roadGlow, Color laneMarking,
            Color skylineColor, Color windowGlow)
        {
            Index = index;
            DisplayName = displayName;
            Objective = objective;
            WordGoal = wordGoal;
            Difficulty = difficulty;
            StartingSpeed = startingSpeed;
            GateSeconds = gateSeconds;
            HazardSpacing = hazardSpacing;
            FogColor = fogColor;
            SkyTint = skyTint;
            AmbientColor = ambientColor;
            RoadGlow = roadGlow;
            LaneMarking = laneMarking;
            SkylineColor = skylineColor;
            WindowGlow = windowGlow;
        }
    }

    public static class GameLevelCatalog
    {
        static readonly GameLevelDefinition[] levels =
        {
            new GameLevelDefinition(0, "CITY SPRINT", "Learn the road and build your typing streak",
                10, WordBank.Difficulty.Easy, 8f, 4.3f, 52f,
                new Color(0.055f, 0.075f, 0.17f), new Color(0.44f, 0.42f, 0.7f),
                new Color(0.3f, 0.37f, 0.53f), new Color(0.12f, 0.91f, 1f),
                new Color(0.3f, 0.38f, 0.55f), new Color(0.065f, 0.09f, 0.2f),
                new Color(0.14f, 0.44f, 0.75f)),
            new GameLevelDefinition(1, "MIDNIGHT RUSH", "Longer words, tighter traffic, faster start",
                14, WordBank.Difficulty.Medium, 10f, 3.8f, 46f,
                new Color(0.10f, 0.035f, 0.15f), new Color(0.62f, 0.27f, 0.55f),
                new Color(0.25f, 0.20f, 0.42f), new Color(1f, 0.32f, 0.72f),
                new Color(0.45f, 0.27f, 0.55f), new Color(0.10f, 0.045f, 0.16f),
                new Color(1f, 0.46f, 0.72f)),
            new GameLevelDefinition(2, "NEON GRAND PRIX", "Hard vocabulary and the fastest campaign pace",
                18, WordBank.Difficulty.Hard, 12f, 3.4f, 41f,
                new Color(0.025f, 0.045f, 0.16f), new Color(0.20f, 0.34f, 0.78f),
                new Color(0.20f, 0.29f, 0.53f), new Color(0.48f, 0.34f, 1f),
                new Color(0.20f, 0.45f, 0.8f), new Color(0.045f, 0.07f, 0.19f),
                new Color(0.24f, 0.88f, 1f)),
            new GameLevelDefinition(3, "STORM CIRCUIT", "Master hard words through a storm-lit downtown",
                22, WordBank.Difficulty.Hard, 13f, 3.25f, 38f,
                new Color(0.018f, 0.065f, 0.12f), new Color(0.16f, 0.48f, 0.57f),
                new Color(0.14f, 0.30f, 0.38f), new Color(0.1f, 0.94f, 0.87f),
                new Color(0.18f, 0.57f, 0.64f), new Color(0.025f, 0.105f, 0.15f),
                new Color(0.25f, 1f, 0.79f)),
            new GameLevelDefinition(4, "NOVA SHOWDOWN", "The ultimate speed test and final storm gate",
                26, WordBank.Difficulty.Hard, 14f, 3.1f, 36f,
                new Color(0.09f, 0.025f, 0.12f), new Color(0.61f, 0.23f, 0.46f),
                new Color(0.31f, 0.16f, 0.36f), new Color(1f, 0.30f, 0.51f),
                new Color(0.54f, 0.22f, 0.49f), new Color(0.13f, 0.035f, 0.13f),
                new Color(1f, 0.58f, 0.72f))
        };

        public static int Count => levels.Length;

        public static GameLevelDefinition Get(int index)
        {
            return levels[Mathf.Clamp(index, 0, levels.Length - 1)];
        }
    }
}
