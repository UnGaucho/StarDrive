﻿using System;
using System.Linq;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class BuildTroop : Goal
    {
        public const string ID = "Build Troop";
        public override string UID => ID;

        public BuildTroop() : base(GoalType.BuildTroop)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitMainGoalCompletion
            };
        }

        public BuildTroop(Troop toCopy, Empire owner) : this()
        {
            ToBuildUID = toCopy.Name;
            empire = owner;
            if (ToBuildUID.IsEmpty())
                Log.Error($"Missing Troop {ToBuildUID}");
        }

        GoalStep FindPlanetToBuildAt()
        {
            float troopRatio = empire.GetEmpireAI().DefensiveCoordinator.TroopsToTroopsWantedRatio;
            if (troopRatio.GreaterOrEqual(1))
                return GoalStep.GoalFailed;

            // find a planet
            Troop troopTemplate = ResourceManager.GetTroopTemplate(ToBuildUID);
            if (empire.FindPlanetToBuildAt(empire.MilitaryOutposts, troopTemplate, out Planet planet))
            {
                if (planet.ConstructionQueue.Any(q => q.isTroop))
                    return GoalStep.TryAgain;

                // submit troop into queue
                planet.Construction.Enqueue(troopTemplate, this);
                if (RandomMath.RollDice(100 - troopRatio * 100))
                    planet.Construction.PrioritizeTroop();

                PlanetBuildingAt = planet;
                return GoalStep.GoToNextStep;
            }
            return GoalStep.GoalFailed;
        }
    }
}
