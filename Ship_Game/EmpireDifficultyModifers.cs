﻿namespace Ship_Game
{
    public struct DifficultyModifiers
    {
        public readonly int SysComModifier;
        public readonly int DiploWeightVsPlayer;
        public readonly float BaseColonyGoals;
        public readonly float ShipBuildStrMin;
        public readonly float ShipBuildStrMax;
        public readonly int ColonyRankModifier;
        public readonly float TaskForceStrength;
        public readonly bool DataVisibleToPlayer;
        public readonly float Anger;
        public readonly int RemnantStory;
        public readonly int ShipLevel;
        public readonly bool HideTacticalData;
        public readonly float MaxDesiredPlanets;
        public readonly float CreditsMultiplier;
        public readonly float FleetCompletenessMin;
        public readonly float EnemyTroopStrength;
        public readonly int MineralDecayDivider;
        public readonly float PiratePayModifier;

        /// <summary>
        /// Higher number will decrease colonization pace
        /// </summary>
        public readonly float ExpansionModifier; 

        public DifficultyModifiers(Empire empire, UniverseData.GameDifficulty difficulty)
        {
            DataVisibleToPlayer = false;
            switch (difficulty)
            {
                case UniverseData.GameDifficulty.Easy:
                    ShipBuildStrMin      = 0.3f;
                    ShipBuildStrMax      = 0.8f;
                    ColonyRankModifier   = -2;
                    TaskForceStrength    = 0.8f;
                    DataVisibleToPlayer  = true;
                    ShipLevel            = 0;
                    HideTacticalData     = false;
                    MaxDesiredPlanets    = 0.25f;
                    FleetCompletenessMin = 0.25f;
                    CreditsMultiplier    = empire.isPlayer ? 0.1f : 0.25f;
                    EnemyTroopStrength   = 1f;
                    MineralDecayDivider  = 100;
                    PiratePayModifier    = 0.5f;
                    ExpansionModifier    = 0.2f;
                    break;
                default:
                case UniverseData.GameDifficulty.Normal:
                    ShipBuildStrMin      = 0.7f;
                    ShipBuildStrMax      = 1;
                    ColonyRankModifier   = 0;
                    TaskForceStrength    = 1.2f;
                    ShipLevel            = 0;
                    HideTacticalData     = false;
                    MaxDesiredPlanets    = 0.5f;
                    FleetCompletenessMin = 0.25f;
                    CreditsMultiplier    = 0.2f;
                    EnemyTroopStrength   = 1.1f;
                    MineralDecayDivider  = 75;
                    PiratePayModifier    = 0.75f;
                    ExpansionModifier    = 0.1f;
                    break;
                case UniverseData.GameDifficulty.Hard:
                    ShipBuildStrMin      = 0.8f;
                    ShipBuildStrMax      = 1f;
                    ColonyRankModifier   = 1;
                    TaskForceStrength    = 1.5f;
                    ShipLevel            = 2;
                    HideTacticalData     = true;
                    MaxDesiredPlanets    = 0.75f;
                    FleetCompletenessMin = 0.5f;
                    CreditsMultiplier    = empire.isPlayer ? 0.3f : 0.1f;
                    EnemyTroopStrength   = 1.25f;
                    MineralDecayDivider  = 50;
                    PiratePayModifier    = 1f;
                    ExpansionModifier    = 0.05f;
                    break;
                case UniverseData.GameDifficulty.Brutal:
                    ShipBuildStrMin      = 0.9f;
                    ShipBuildStrMax      = 1f;
                    ColonyRankModifier   = 2;
                    TaskForceStrength    = 1.75f;
                    ShipLevel            = 3;
                    HideTacticalData     = true;
                    MaxDesiredPlanets    = 1f;
                    FleetCompletenessMin = 1f;
                    CreditsMultiplier    = empire.isPlayer ? 0.5f : 0.05f;
                    EnemyTroopStrength   = 1.5f;
                    MineralDecayDivider  = 35;
                    PiratePayModifier    = 1.5f;
                    ExpansionModifier    = 0f;
                    break;
            }

            if (empire.isPlayer)
            {
                BaseColonyGoals = 3;
            }
            else
            {
                EconomicResearchStrategy strategy = ResourceManager.GetEconomicStrategy(empire.data.EconomicPersonality.Name);
                BaseColonyGoals                   = (float)difficulty * 2.5f * strategy.ExpansionRatio;
            }

            SysComModifier      = (int)(((int)difficulty + 1) * 0.75f).LowerBound(1);
            DiploWeightVsPlayer = (int)difficulty + 1;
            Anger               = 1 + ((int)difficulty + 1) * 0.2f;
            RemnantStory        = (int)difficulty * 3;

            if (empire.isPlayer)
            {
                ShipBuildStrMin    = 0.9f;
                ShipBuildStrMax    = 1f;
                ColonyRankModifier = 0;
                TaskForceStrength  = 1f;
                if (GlobalStats.FixedPlayerCreditCharge && difficulty > UniverseData.GameDifficulty.Easy)
                    CreditsMultiplier = 0.2f;
            }
        }
    }
}
