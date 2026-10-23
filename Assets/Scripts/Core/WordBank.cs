using UnityEngine;

namespace TypeDrive.Core
{
    /// <summary>
    /// Static word lists grouped by difficulty. GateSpawner asks this for a
    /// random word matching the GameManager's current difficulty tier.
    /// Replace or extend these arrays freely - nothing else needs to change.
    /// </summary>
    public static class WordBank
    {
        public enum Difficulty { Easy, Medium, Hard }

        static readonly string[] easy =
        {
            "car","road","fast","turn","gas","stop","go","left","right","drive",
            "race","lane","fuel","surge","dash","curve","brake","speed","shift","wheel"
        };

        static readonly string[] medium =
        {
            "engine","highway","traffic","overtake","throttle","gearbox","ignition",
            "junction","headlight","accelerate","mirror","exhaust","bumper","chassis",
            "odometer","collision","downhill","uphill","sideways","daylight"
        };

        static readonly string[] hard =
        {
            "turbocharger","windscreen","acceleration","suspension","transmission",
            "intersection","speedometer","aerodynamics","deceleration","differential",
            "carburetor","trajectory","overclocking","navigation","synchronization"
        };

        public static string GetRandomWord(Difficulty difficulty)
        {
            string[] source = difficulty switch
            {
                Difficulty.Hard => hard,
                Difficulty.Medium => medium,
                _ => easy
            };
            return source[Random.Range(0, source.Length)];
        }
    }
}
