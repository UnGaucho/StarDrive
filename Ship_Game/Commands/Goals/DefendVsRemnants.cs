﻿using Ship_Game.AI;
using System;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    class DefendVsRemnants : Goal
    {
        public const string ID = "DefendVsRemnants";
        public override string UID => ID;
        private Planet TargetPlanet;

        public DefendVsRemnants() : base(GoalType.BuildOrbital)
        {
            Steps = new Func<GoalStep>[]
            {
                WaitForFleet,
            };
        }

        public DefendVsRemnants(Planet targetPlanet, Empire owner, Fleet fleet) : this()
        {
            empire             = owner;
            ColonizationTarget = targetPlanet;
            Fleet              = fleet;
            TargetEmpire       = EmpireManager.Remnants;
            PostInit();
        }

        public sealed override void PostInit()
        {
            TargetPlanet = ColonizationTarget;
        }

        bool RemnantTaskExists()
        {
            var tasks = empire.GetEmpireAI().GetRemnantEngagementTasksFor(TargetPlanet);
            return tasks.Length != 0;
        }

        bool TryChangeTargetPlanet()
        {
            var remnantFleets = TargetEmpire.GetFleetsDict().Values.ToArray();
            if (!remnantFleets.Any(f => f.FleetTask?.TargetPlanet?.Owner == empire))
                return false;

            var defenseTasks = empire.GetEmpireAI().GetDefendVsRemnantTasks();
            foreach (Fleet remnantFleet in remnantFleets.Filter(f => f.FleetTask?.TargetPlanet?.Owner == empire))
            {
                // Check if we have other defense task vs. this remnant fleet target planet
                foreach (MilitaryTask task in defenseTasks)
                {
                    if (task.TargetPlanet == remnantFleet.FleetTask.TargetPlanet)
                        continue;

                    TargetPlanet = remnantFleet.FleetTask.TargetPlanet;
                    Fleet.TaskStep = 0;
                    Fleet.FleetTask.ChangeTargetPlanet(TargetPlanet);
                    return true;
                }
            }

            return false;
        }

        GoalStep WaitForFleet()
        {
            if (Fleet == null)
                return GoalStep.GoalFailed;

            if (TargetPlanet.Owner != empire && !TryChangeTargetPlanet())
                return GoalStep.GoalComplete;

            return RemnantTaskExists() ? GoalStep.TryAgain : GoalStep.GoalComplete;
        }
    }
}