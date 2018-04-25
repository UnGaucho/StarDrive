﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class BuildScout : Goal
    {
        public const string ID = "Build Scout";
        public override string UID => ID;

        public BuildScout() : base(GoalType.BuildScout)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitMainGoalCompletion,  
                ReportGoalCompleteToEmpire,
            };
        }
        public BuildScout(Empire empire) : this()
        {
            this.empire = empire;
        }

        private Planet FindScoutProductionPlanet()
        {
            Planet bestPlanet = null;
            int num1 = 9999999;
            foreach (Planet planet2 in empire.BestShipYards)
            {
                int num2 = 0;
                foreach (QueueItem queueItem in planet2.ConstructionQueue)
                    num2 += (int)((queueItem.Cost - queueItem.productionTowards) / planet2.NetProductionPerTurn);
                if (num2 < num1)
                {
                    num1 = num2;
                    bestPlanet = planet2;
                }
            }
            return bestPlanet;
        }

        private GoalStep FindPlanetToBuildAt()
        {
            Planet planet = FindScoutProductionPlanet();
            if (planet == null)
                return GoalStep.TryAgain;
            if (EmpireManager.Player == empire
                && ResourceManager.ShipsDict.TryGetValue(EmpireManager.Player.data.CurrentAutoScout, out Ship autoScout))
            {
                planet.ConstructionQueue.Add(new QueueItem()
                {
                    isShip = true,
                    QueueNumber = planet.ConstructionQueue.Count,
                    sData = autoScout.GetShipData(),
                    Goal = this,
                    Cost = autoScout.GetCost(empire),
                    NotifyOnEmpty = false
                });
                return GoalStep.GoToNextStep;
            }

            var scoutShipsWeCanBuild = new Array<Ship>();
            foreach (string shipUid in empire.ShipsWeCanBuild)
            {
                Ship ship = ResourceManager.ShipsDict[shipUid];
                if (ship.shipData.Role == ShipData.RoleName.scout)
                    scoutShipsWeCanBuild.Add(ship);
            }
            if (scoutShipsWeCanBuild.IsEmpty)
                return GoalStep.TryAgain;

            Ship mostPowerEfficientScout = scoutShipsWeCanBuild.FindMax(s => s.PowerFlowMax - s.ModulePowerDraw);
            planet.ConstructionQueue.Add(new QueueItem()
            {
                isShip = true,
                QueueNumber = planet.ConstructionQueue.Count,
                sData = mostPowerEfficientScout.GetShipData(),
                Goal = this,
                Cost = mostPowerEfficientScout.GetCost(empire)
            });
            return GoalStep.GoToNextStep;
        }
       
        private GoalStep OrderExploreForLastFoundScoutInEmpire()
        {
            bool foundFreighter = false;
            foreach (Ship ship in empire.GetShips())
            {
                if ((ship.shipData.Role == ShipData.RoleName.scout
                     || ship.Name == EmpireManager.Player.data.CurrentAutoScout) && !ship.PlayerShip)
                {
                    freighter = ship;
                    foundFreighter = true;
                }
            }
            if (!foundFreighter)
                return GoalStep.TryAgain;
            freighter.AI.OrderExplore();
            return GoalStep.GoToNextStep;
        }

        private GoalStep ReportGoalCompleteToEmpire()
        {
            empire.ReportGoalComplete(this);
            return GoalStep.GoalComplete;
        }
    }
}
