using Microsoft.Xna.Framework;
using Ship_Game.Debug;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.AI.Tasks
{
    public partial class MilitaryTask
    {
        float GetEnemyShipStrengthInAO()
        {
            // RedFox: I removed ClampMin(minimumStrength) because this was causing infinite
            //         Create-Destroy-Create loop of ClearAreaOfEnemies MilitaryTasks
            //         Lets just report what the actual strength is.
            return Owner.GetEmpireAI().ThreatMatrix.PingNetRadarStr(AO, AORadius * 2, Owner);
        }

        private int GetTargetPlanetGroundStrength(int minimumStrength)
        {
            return (int)TargetPlanet.GetGroundStrengthOther(Owner).LowerBound(minimumStrength);
        }

        Array<Troop> GetTroopsOnPlanets(Vector2 rallyPoint, float strengthNeeded, out float totalStrength,
                                        bool troopPriorityHigh)
        {
            var potentialTroops = new Array<Troop>();
            totalStrength = 0;
            if (strengthNeeded <= 0) return potentialTroops;

            var defenseDict     = Owner.GetEmpireAI().DefensiveCoordinator.DefenseDict;
            var troopSystems    = Owner.GetOwnedSystems().Sorted(dist => dist.Position.SqDist(rallyPoint));
            
            for (int x = 0; x < troopSystems.Length; x++)
            {
                SolarSystem system     = troopSystems[x];
                SystemCommander sysCom = defenseDict[system];
                if (!sysCom.IsEnoughTroopStrength) 
                    continue;
                
                for (int i = 0; i < system.PlanetList.Count; i++)
                {
                    Planet planet = system.PlanetList[i];

                    if (planet.Owner != Owner) continue;
                    if (planet.RecentCombat)   continue;

                    float planetMinStr                 = sysCom.PlanetTroopMin(planet);
                    float planetDefendingTroopStrength = planet.GetDefendingTroopCount();
                    float maxCanTake                   = troopPriorityHigh 
                                    ? 5 
                                    : (planetDefendingTroopStrength - (planetMinStr - 3)).LowerBound(0);

                    if (maxCanTake > 0)
                    {
                        potentialTroops.AddRange(planet.GetEmpireTroops(Owner, (int)maxCanTake));

                        totalStrength += potentialTroops.Sum(t => t.ActualStrengthMax);

                        if (strengthNeeded <= totalStrength)
                        {
                            break;
                        }
                    }
                }

                if (potentialTroops.Count > 50)
                {
                    break;
                }
            }

            return potentialTroops;
        }

        private void CreateFleet(Array<Ship> ships, string Name)
        {
            var newFleet = new Fleet
            {
                Name = Name,
                Owner = Owner
            };

            int fleetNum = FindUnusedFleetNumber();
            Owner.GetFleetsDict()[fleetNum] = newFleet;
            Owner.GetEmpireAI().UsedFleets.Add(fleetNum);
            WhichFleet = fleetNum;
            newFleet.FleetTask = this;
            foreach (Ship ship in ships)
            {
                ship.AI.ClearOrders();
                Owner.Pool.RemoveShipFromFleetAndPools(ship);
                newFleet.AddShip(ship);
            }

            newFleet.AutoArrange();
        }

        private bool AreThereEnoughTroopsToInvade(float invasionTroopStrength, out Array<Troop> troopsOnPlanetNeeded,
                                                  Vector2 rallyPoint, bool troopPriorityHigh = false)
        {
            troopsOnPlanetNeeded = new Array<Troop>();
            if (NeededTroopStrength <= 0)
                return true;

            if (invasionTroopStrength < NeededTroopStrength)
            {
                troopsOnPlanetNeeded = GetTroopsOnPlanets(rallyPoint, NeededTroopStrength, 
                                              out float planetsTroopStrength, troopPriorityHigh);

                if (invasionTroopStrength + planetsTroopStrength >= NeededTroopStrength)
                    return true;
            }
            return true;
        }

        /// <summary>
        /// The concept here is to calculate how much bomb power is needed.
        /// this set into minutes of bombing. 
        /// </summary>
        /// <returns></returns>
        private int BombTimeNeeded()
        {
            //ground landing spots. if we dont have a significant space to land troops. create them. 
            int bombTime =  TargetPlanet.TotalDefensiveStrength / 20  ;

            //shields are a real pain. this may need a lot more code to deal with. 
            bombTime    += (int)TargetPlanet.ShieldStrengthMax / 50;
            return bombTime;
        }

        AO FindClosestAO(float strWanted = 100)
        {
            var aos = Owner.GetEmpireAI().AreasOfOperations;
            if (aos.Count == 0)
            {
                Log.Info($"{Owner.Name} has no areas of operation");
                return null;
            }

            AO closestAO = aos.FindMaxFiltered(ao => ao.GetPoolStrength() > strWanted,
                               ao => -ao.Center.SqDist(AO))
                           ?? aos.FindMin(ao => ao.Center.SqDist(AO));
            return closestAO;
        }

        Fleet FindClosestCoreFleet(float strWanted = 100)
        {
            Array<AO> aos = Owner.GetEmpireAI().AreasOfOperations;
            if (aos.Count == 0)
            {
                Log.Error($"{Owner.Name} has no areas of operation");
                return null;
            }

            AO closestAo = aos.FindMinFiltered(ao => ao.GetCoreFleet().GetStrength() > strWanted,
                                               ao => ao.Center.SqDist(AO));
            return closestAo?.GetCoreFleet();
        }

        void SendSofteningFleet(float enemyStrength)
        {
            Fleet coreFleet = FindClosestCoreFleet(MinimumTaskForceStrength);
            if (coreFleet == null || coreFleet.FleetTask != null)
                return;

            // don't send the fleet if it definitely cannot take the fight
            if (!coreFleet.CanTakeThisFight(enemyStrength))
                return;

            var clearArea = new MilitaryTask(coreFleet.Owner)
            {
                AO = TargetPlanet.Center,
                AORadius = 75000f,
                type = TaskType.ClearAreaOfEnemies,
                TargetPlanet = TargetPlanet,
                TargetPlanetGuid = TargetPlanet.guid
            };

            coreFleet.Owner.GetEmpireAI().AddPendingTask(clearArea);
            clearArea.WhichFleet = Owner.GetFleetsDict().FindFirstKeyForValue(coreFleet);
            coreFleet.FleetTask = clearArea;
            clearArea.IsCoreFleetTask  = true;
            coreFleet.TaskStep  = 1;
            clearArea.Step = 1;
        }

        void RequisitionCoreFleet()
        {
            AO[] sorted = Owner.GetEmpireAI().AreasOfOperations
                .OrderByDescending(ao => ao.OffensiveForcePoolStrength >= MinimumTaskForceStrength)
                .ThenBy(ao => AO.Distance(ao.Center)).ToArray();

            if (sorted.Length == 0)
                return;

            AO closestAO = sorted[0];
            EnemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingRadarStr(AO, 10000, Owner);
            if (EnemyStrength < 1f)
                return;

            MinimumTaskForceStrength = EnemyStrength;
            if (closestAO.GetCoreFleet().FleetTask == null &&
                closestAO.GetCoreFleet().GetStrength() > MinimumTaskForceStrength)
            {
                WhichFleet = closestAO.WhichFleet;
                closestAO.GetCoreFleet().FleetTask = this;
                closestAO.GetCoreFleet().TaskStep = 1;
                IsCoreFleetTask = true;
                Step = 1;
            }
        }

        void RequisitionDefenseForce()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            AO = TargetPlanet?.Center ?? AO;

            InitFleetRequirements(minFleetStrength: 100, minTroopStrength: 0, minBombMinutes: 0);

            float battleFleetSize = Owner.DifficultyModifiers.FleetCompletenessMin;

            if (CreateTaskFleet("Defensive Fleet", battleFleetSize) == RequisitionStatus.Complete)
            {
                Step = 1;
            }
        }

        void RequisitionClaimForce()
        {
            if (AO.AlmostZero())
                throw new Exception();

            AO closestAO = FindClosestAO();
            if (closestAO == null || closestAO.GetNumOffensiveForcePoolShips() < 1)
                return;

            int requiredTroopStrength = 0;
            if (TargetPlanet != null)
            {
                AO = TargetPlanet.Center;
                requiredTroopStrength = (int)TargetPlanet.GetGroundStrengthOther(Owner) - (int)TargetPlanet.GetGroundStrength(Owner);
            }
            
            InitFleetRequirements(minFleetStrength: 100, minTroopStrength: requiredTroopStrength.LowerBound(40), minBombMinutes: 0);
            float battleFleetSize = Owner.DifficultyModifiers.FleetCompletenessMin;

            if (CreateTaskFleet("Scout Fleet", battleFleetSize, true) == RequisitionStatus.Complete)
                Step = 1;
        }

        void RequisitionAssaultPirateBase()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            if (TargetShip == null || !TargetShip.Active)
            {
                EndTask();
                return;
            }

            AO closestAO  = FindClosestAO(EnemyStrength);
            if (closestAO == null || closestAO.GetNumOffensiveForcePoolShips() < 1)
                return;


            InitFleetRequirements(minFleetStrength: (int)EnemyStrength, minTroopStrength: 0, minBombMinutes: 0);
            EnemyStrength = GetEnemyShipStrengthInAO().LowerBound(TargetShip.BaseStrength);
            if (CreateTaskFleet("Assault Fleet", 1) == RequisitionStatus.Complete)
                Step = 1;
        }

        void RequisitionExplorationForce()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            if (TargetPlanet.Owner != null && !Owner.IsEmpireAttackable(TargetPlanet.Owner))
            {
                EndTask();
                return;
            }

            AO = TargetPlanet.Center;
            float minStrength = Owner.GetEmpireAI().ThreatMatrix.PingRadarStr(TargetPlanet.Center
                                , TargetPlanet.ParentSystem.Radius
                                , Owner
                                , true).LowerBound(100);
            InitFleetRequirements((int)minStrength, minTroopStrength: 40, minBombMinutes: 0);

            float battleFleetSize = Owner.DifficultyModifiers.FleetCompletenessMin * 0.5f;

            if (CreateTaskFleet("Exploration Force", battleFleetSize, true) 
                                == RequisitionStatus.Complete)
            {
                Step = 1;
            }
        }

        void RequisitionAssaultForces()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            if (TargetPlanet.Owner == null || TargetPlanet.Owner == Owner ||
                Owner.GetRelations(TargetPlanet.Owner).Treaty_Peace)
            {
                EndTask();
                return;
            }
            EnemyStrength = GetEnemyShipStrengthInAO();
            AO = TargetPlanet.Center;
            InitFleetRequirements(minFleetStrength: 100, minTroopStrength: 100 ,minBombMinutes: 1);

            float battleFleetSize = Owner.DifficultyModifiers.FleetCompletenessMin;

            if (CreateTaskFleet("Invasion Fleet", battleFleetSize) == RequisitionStatus.Complete)
            {
                Step = 1;
            }
        }

        void RequisitionGlassForce()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            if (TargetPlanet.Owner == null || TargetPlanet.Owner == Owner ||
                Owner.GetRelations(TargetPlanet.Owner).Treaty_Peace)
            {
                EndTask();
                return;
            }
            EnemyStrength = TargetPlanet.ParentSystem.ShipList.Sum(s => s.loyalty == TargetPlanet.Owner ? s.BaseStrength : 0);
            AO = TargetPlanet.Center;
            InitFleetRequirements(minFleetStrength: 1000, minTroopStrength: 0, minBombMinutes: 2);

            float battleFleetSize = Owner.DifficultyModifiers.FleetCompletenessMin;

            if (CreateTaskFleet("Doom Fleet", battleFleetSize) == RequisitionStatus.Complete)
            {
                Step = 1;
            }
        }

        /// <summary>
        /// this creates a task fleet from task parameters.
        /// AO, EnemyStrength, NeededTroopStrength, TaskBombTimeNeeded
        /// </summary>
        /// <param name="fleetName">The name displayed for the fleet</param>
        /// <param name="battleFleetSize">The ratio of a full fleet required. Best to keep this low. 
        /// a full fleet is each role count in the fleet ratios being fulfilled.
        /// if a full fleet is not found but the needed strength is found it will create a fleet
        /// slightly larger than needed.
        /// technically the enemy strength requirement could be 0 and fleetSize set 1 and it would bring
        /// as close to a main battle fleet as it can.
        /// </param>
        /// <returns>The return is either complete for success or the type of failure in fleet creation.</returns>
        RequisitionStatus CreateTaskFleet(string fleetName, float battleFleetSize, bool highTroopPriority = false)
        {
            // this determines what core fleet to send if the enemy is strong. 
            // its also an easy out for an empire in a bad state. 
            AO closestAO = FindClosestAO(MinimumTaskForceStrength);

            if (closestAO == null)
            {
                return RequisitionStatus.NoEmpireAreasOfOperation;
            }

            // where the fleet will gather after requisition before moving to target AO.
            Planet rallyPoint = Owner.FindNearestRallyPoint(AO);
            if (rallyPoint == null)
                return RequisitionStatus.NoRallyPoint;


            FleetShips fleetShips = Owner.AllFleetsReady(rallyPoint.Center);
            fleetShips.WantedFleetCompletePercentage = battleFleetSize;

            // if we cant build bombers then convert bombtime to troops. 
            // This assume a standard troop strength of 10 
            if (fleetShips.BombSecsAvailable < TaskBombTimeNeeded)
                NeededTroopStrength += (TaskBombTimeNeeded - fleetShips.BombSecsAvailable).LowerBound(0) * 10;

            if (fleetShips.AccumulatedStrength < EnemyStrength)
            {
                // send a core fleet and wait.
                SendSofteningFleet(EnemyStrength);
                return  RequisitionStatus.NotEnoughShipStrength;
            }

            // See if we need to gather troops from planets. Bail if not enough
            if (!AreThereEnoughTroopsToInvade(fleetShips.InvasionTroopStrength, out Array<Troop> troopsOnPlanets, rallyPoint.Center, highTroopPriority))
                return RequisitionStatus.NotEnoughTroopStrength;

            int wantedNumberOfFleets = 1;
            if (TargetPlanet?.Owner != null)
            {
                wantedNumberOfFleets = TargetPlanet.ParentSystem.PlanetList.Max(p =>
                {
                    if (p.Owner == TargetPlanet.Owner)
                    {
                        var extraFleets = TargetPlanet.Level;
                        extraFleets    += TargetPlanet.HasWinBuilding ? 2 : 0;
                        extraFleets    += TargetPlanet.BuildingList.Any(b => b.IsCapital) ? 1 : 0;
                        return extraFleets;
                    }
                    return 0;
                });
            }

            // All's Good... Make a fleet
            TaskForce = fleetShips.ExtractShipSet(MinimumTaskForceStrength, TaskBombTimeNeeded
                , NeededTroopStrength, troopsOnPlanets, wantedNumberOfFleets);

            if (TaskForce.IsEmpty)
                return RequisitionStatus.FailedToCreateAFleet;

            CreateFleet(TaskForce, fleetName);
            return RequisitionStatus.Complete;
        }

        void InitFleetRequirements(int minFleetStrength, int minTroopStrength, int minBombMinutes)
        {
            if (minTroopStrength > 0 || minBombMinutes > 0)
            {
                if (TargetPlanet == null)
                {
                    Log.Error($"Sending troops with no planet to assault");
                }
                else
                {
                    if (minTroopStrength > 0)
                        NeededTroopStrength = (int)(GetTargetPlanetGroundStrength(minTroopStrength)
                                            * Owner.DifficultyModifiers.EnemyTroopStrength);

                    if (minBombMinutes > 0)
                        TaskBombTimeNeeded = BombTimeNeeded().LowerBound(minBombMinutes);
                }
            }

            EnemyStrength = GetEnemyShipStrengthInAO();
            MinimumTaskForceStrength = Math.Max(minFleetStrength, EnemyStrength);
            if (!Owner.isPlayer)
                MinimumTaskForceStrength *= Owner.DifficultyModifiers.TaskForceStrength;
        }

        enum RequisitionStatus
        {
            NoRallyPoint,
            NoEmpireAreasOfOperation,
            NotEnoughShipStrength,
            NotEnoughTroopStrength,
            NotEnoughBomberStrength,
            FailedToCreateAFleet,
            Complete
        }
    }
}