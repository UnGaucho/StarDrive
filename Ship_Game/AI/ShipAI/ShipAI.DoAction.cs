using Microsoft.Xna.Framework;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Linq;
using System.Text;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        void DoBoardShip(float elapsedTime)
        {
            HasPriorityTarget = true;
            State = AIState.Boarding;
            if ((!EscortTarget?.Active ?? true)
                || EscortTarget.loyalty == Owner.loyalty)
            {
                ClearOrders(State);
                if (Owner.Mothership != null)
                {
                    if (Owner.Mothership.TroopsOut)
                        Owner.DoEscort(Owner.Mothership);
                    else
                        OrderReturnToHangar();
                }
                return;
            }
            ThrustOrWarpToPosCorrected(EscortTarget.Center, elapsedTime);
            float distance = Owner.Center.Distance(EscortTarget.Center);
            if (distance < EscortTarget.Radius + 300f)
            {
                if (Owner.TroopList.Count > 0)
                    Owner.TroopList[0].LandOnShip(EscortTarget);
            }
            else if (distance > 10000f && Owner.Mothership?.AI.CombatState == CombatState.AssaultShip)
                OrderReturnToHangar();
        }

        void DoCombat(float elapsedTime)
        {
            if (Target?.Active != true || Target.engineState != Ship.MoveState.Sublight
                                       || !Owner.loyalty.IsEmpireAttackable(Target.GetLoyalty(), Target))
            {
                Target = PotentialTargets.FirstOrDefault(t => t.Active && t.engineState != Ship.MoveState.Warp &&
                                                              t.Center.InRadius(Owner.Center, Owner.SensorRange));
                if (Target == null)
                {
                    DequeueCurrentOrder();
                    State = DefaultAIState;
                    return;
                }
            }

            AwaitClosest = null;
            State = AIState.Combat;
            Owner.InCombat = true;
            Owner.InCombatTimer = 15f;

            if (!HasPriorityOrder && !HasPriorityTarget && Owner.Weapons.Count == 0 && !Owner.Carrier.HasActiveHangars)
                CombatState = CombatState.Evade;

            if (Owner.System != null && Owner.Carrier.HasTroopBays)
                CombatState = CombatState.AssaultShip;

            // in range:
            if (Target.Center.InRadius(Owner.Center, 7500f))
            {
                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();
                if (Owner.Carrier.HasHangars && !Owner.ManualHangarOverride)
                    Owner.Carrier.ScrambleFighters();
            }
            else
            {
                // need to move this into fleet.
                if (FleetNode != null && Owner.fleet != null)
                { if (Target == null)
                        Log.Error("doCombat: Target was null? : https://sentry.io/blackboxmod/blackbox/issues/628107403/");
                    Vector2 nodePos = Owner.fleet.AveragePosition() + FleetNode.FleetOffset;
                    if (Target.Center.OutsideRadius(nodePos, FleetNode.OrdersRadius))
                    {
                        if (Owner.Center.OutsideRadius(nodePos, 1000f))
                        {
                            ThrustOrWarpToPosCorrected(nodePos, elapsedTime);
                        }
                        else
                        {
                            DoHoldPositionCombat(elapsedTime);
                        }
                        return;
                    }
                }
                if (CombatState != CombatState.HoldPosition && CombatState != CombatState.Evade)
                {
                    if (Owner.FastestWeapon.ProjectedImpactPointNoError(Target, out Vector2 prediction) == false)
                        prediction = Target.Center;
                    ThrustOrWarpToPosCorrected(prediction, elapsedTime);
                    return;
                }
            }

            if (Intercepting && CombatState != CombatState.HoldPosition && CombatState != CombatState.Evade
                && Owner.Center.OutsideRadius(Target.Center, Owner.maxWeaponsRange*3f))
            {
                ThrustOrWarpToPosCorrected(Target.Center, elapsedTime);
                return;
            }

            CombatAI.ExecuteCombatTactic(elapsedTime);

            // Target was modified by one of the CombatStates (?)
            Owner.InCombat = Target != null;
        }

        void DoDeploy(ShipGoal g)
        {
            if (g.Goal == null)
                return;

            Planet target = g.TargetPlanet;
            if (target == null && g.Goal.TetherTarget != Guid.Empty)
            {
                Empire.Universe.PlanetsDict.TryGetValue(g.Goal.TetherTarget, out target);
            }

            if (target != null && (target.Center + g.Goal.TetherOffset).Distance(Owner.Center) > 200f)
            {
                g.Goal.BuildPosition = target.Center + g.Goal.TetherOffset;
                OrderDeepSpaceBuild(g.Goal);
                return;
            }

            Ship platform = Ship.CreateShipAtPoint(g.Goal.ToBuildUID, Owner.loyalty, g.Goal.BuildPosition);
            if (platform == null)
                return;

            AddStructureToRoadsList(g, platform);

            if (g.Goal.TetherTarget != Guid.Empty)
            {
                platform.TetherToPlanet(Empire.Universe.PlanetsDict[g.Goal.TetherTarget]);
                platform.TetherOffset = g.Goal.TetherOffset;
            }
            Owner.QueueTotalRemoval();
        }

        void AddStructureToRoadsList(ShipGoal g, Ship platform)
        {
            foreach (SpaceRoad road in Owner.loyalty.SpaceRoadsList)
            {
                foreach (RoadNode node in road.RoadNodesList)
                {
                    if (node.Position == g.Goal.BuildPosition)
                    {
                        node.Platform = platform;
                        StatTracker.StatAddRoad(node, Owner.loyalty);
                        return;
                    }
                }
            }
        }

        public void DoExplore(float elapsedTime)
        {
            HasPriorityOrder = true;
            IgnoreCombat = true;
            if (ExplorationTarget == null)
            {
                ExplorationTarget = Owner.loyalty.GetEmpireAI().AssignExplorationTarget(Owner);
                if (ExplorationTarget == null)
                {
                    ClearOrders();
                }
            }
            else if (DoExploreSystem(elapsedTime)) //@Notification
            {
                if (Owner.loyalty.isPlayer)
                {
                    //added by gremlin  add shamatts notification here
                    SolarSystem system = ExplorationTarget;
                    var message = new StringBuilder(system.Name); //@todo create global string builder
                    message.Append(" system explored.");

                    var planetsTypesNumber = new Map<string, int>();
                    if (system.PlanetList.Count > 0)
                    {
                        foreach (Planet planet in system.PlanetList)
                        {
                            planetsTypesNumber.AddToValue(planet.CategoryName, 1);
                        }

                        foreach (var pair in planetsTypesNumber)
                            message.Append('\n').Append(pair.Value).Append(' ').Append(pair.Key);
                    }

                    foreach (Planet planet in system.PlanetList)
                    {
                        Building tile = planet.BuildingList.Find(t => t.IsCommodity);
                        if (tile != null)
                            message.Append('\n').Append(tile.Name).Append(" on ").Append(planet.Name);
                    }

                    if (system.HostileForcesPresent(Owner.loyalty))
                        message.Append("\nCombat in system!!!");

                    if (system.OwnerList.Count > 0 && !system.OwnerList.Contains(Owner.loyalty))
                        message.Append("\nContested system!!!");

                    Empire.Universe.NotificationManager.AddNotification(new Notification
                    {
                        Pause = false,
                        Message = message.ToString(),
                        ReferencedItem1 = system,
                        Icon = system.Sun.Icon,
                        Action = "SnapToExpandSystem"
                    }, "sd_ui_notification_warning");
                }
                ExplorationTarget = null;
            }
        }

        bool DoExploreSystem(float elapsedTime)
        {
            SystemToPatrol = ExplorationTarget;
            if (PatrolRoute.Count == 0)
            {
                foreach (Planet p in SystemToPatrol.PlanetList)
                    PatrolRoute.Add(p);

                if (SystemToPatrol.PlanetList.Count == 0)
                    return ExploreEmptySystem(elapsedTime, SystemToPatrol);
            }
            else
            {
                PatrolTarget = PatrolRoute[StopNumber];
                if (PatrolTarget.IsExploredBy(Owner.loyalty))
                {
                    StopNumber += 1;
                    if (StopNumber == PatrolRoute.Count)
                    {
                        StopNumber = 0;
                        PatrolRoute.Clear();
                        return true;
                    }
                }
                else
                {
                    MovePosition = PatrolTarget.Center;
                    float distanceToTarget = Owner.Center.Distance(MovePosition);
                    if (distanceToTarget < 75000f)
                        PatrolTarget.ParentSystem.SetExploredBy(Owner.loyalty);

                    if (distanceToTarget >= 5500f)
                    {
                        float speedLimit = Owner.Speed.Clamped(distanceToTarget, Owner.velocityMaximum);
                        ThrustOrWarpToPosCorrected(MovePosition, elapsedTime, speedLimit);
                    }
                    else
                    {
                        ThrustOrWarpToPosCorrected(MovePosition, elapsedTime);
                        if (distanceToTarget < 500f)
                        {
                            PatrolTarget.SetExploredBy(Owner.loyalty);
                            StopNumber += 1;
                            if (StopNumber == PatrolRoute.Count)
                            {
                                StopNumber = 0;
                                PatrolRoute.Clear();
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        void DoHoldPositionCombat(float elapsedTime)
        {
            if (Owner.Velocity.Length() > 0f)
            {
                ReverseThrustUntilStopped(elapsedTime);
                Vector2 interceptPoint = Owner.PredictImpact(Target);
                RotateTowardsPosition(interceptPoint, elapsedTime, 0.2f);
            }
            else
            {
                RotateTowardsPosition(Target.Center, elapsedTime, 0.2f);
            }
        }

        void DoLandTroop(float elapsedTime, ShipGoal goal)
        {
            if (Owner.shipData.Role != ShipData.RoleName.troop || Owner.TroopList.Count == 0)
                DoOrbit.Orbit(goal.TargetPlanet, elapsedTime); //added by gremlin.

            float radius = goal.TargetPlanet.ObjectRadius + Owner.Radius * 2;
            float distCenter = goal.TargetPlanet.Center.Distance(Owner.Center);

            if (Owner.shipData.Role == ShipData.RoleName.troop && Owner.TroopList.Count > 0)
            {
                if (Owner.engineState == Ship.MoveState.Warp && distCenter < 7500f)
                    Owner.HyperspaceReturn();
                if (distCenter < radius)
                    ThrustOrWarpToPosCorrected(goal.TargetPlanet.Center, elapsedTime,
                        Owner.Speed > 200 ? Owner.Speed * 0.90f : Owner.velocityMaximum);
                else
                    ThrustOrWarpToPosCorrected(goal.TargetPlanet.Center, elapsedTime);

                if (distCenter < goal.TargetPlanet.ObjectRadius)
                    Owner.TroopList[0].TryLandTroop(goal.TargetPlanet);

                return;
            }
            // FB @todo - change to FreeTiles
            if (Owner.loyalty == goal.TargetPlanet.Owner || goal.TargetPlanet.GetGroundLandingSpots() == 0
                || Owner.Carrier.NumTroopsInShipAndInSpace <= 0)
            {
                if (Owner.loyalty.isPlayer)
                    HadPO = true;

                ClearOrders(DefaultAIState);
                Log.Info($"Do Land Troop: Troop Assault Canceled with {Owner.TroopList.Count} troops and {goal.TargetPlanet.GetGroundLandingSpots()} Landing Spots ");
            }
            else if (!Owner.Carrier.HasTransporters) // FB: use regular invade
            {
                if (distCenter > 7500f)
                    return;
                Owner.Carrier.AssaultPlanet(goal.TargetPlanet);
                Owner.DoOrbit(goal.TargetPlanet);
            }
            else if (distCenter < radius)  // STSA with transpoters - this should be checked wit STSA active
                Owner.Carrier.AssaultPlanetWithTransporters(goal.TargetPlanet);
        }

        void DoRebase(ShipGoal goal)
        {
            if (Owner.TroopList.Count == 0)
                Owner.QueueTotalRemoval(); // troops not found, vanish the ship
            else if (!Owner.TroopList[0].TryLandTroop(goal.TargetPlanet))
                ClearOrders();
        }

        void DoRefit(ShipGoal goal)
        {
            if (goal.Goal == null) // empire goal was removed or planet was compromised
                ClearOrders();

            ClearOrders(AIState.HoldPosition);
        }

        void DoRepairDroneLogic(Weapon w)
        {
            using (FriendliesNearby.AcquireReadLock())
            {
                Ship repairMe = FriendliesNearby.FindMinFiltered(
                    filter: ship => ShipNeedsRepair(ship, ShipResupply.RepairDroneRange),
                    selector: ship => ship.InternalSlotsHealthPercent);

                if (repairMe == null) return;
                Vector2 target = w.Center.DirectionToTarget(repairMe.Center);
                target.Y = target.Y * -1f;
                w.FireDrone(target);
            }
        }

        void DoRepairBeamLogic(Weapon w)
        {
            Ship repairMe = FriendliesNearby.FindMinFiltered(
                    filter: ship => ShipNeedsRepair(ship, w.Range + 500f, Owner),
                    selector: ship => ship.InternalSlotsHealthPercent);

            if (repairMe != null) w.FireTargetedBeam(repairMe);
        }

        bool ShipNeedsRepair(Ship target, float maxDistance, Ship doNotHealSelf = null)
        {
            return target.Active && target != doNotHealSelf
                    && target.HealthPercent < ShipResupply.RepairDroneThreshold
                    && Owner.Center.Distance(target.Center) <= maxDistance;
        }

        void DoOrdinanceTransporterLogic(ShipModule module)
        {
            Ship repairMe = module.GetParent()
                    .loyalty.GetShips()
                    .FindMinFiltered(
                        filter: ship => Owner.Center.Distance(ship.Center) <= module.TransporterRange + 500f
                                        && ship.Ordinance < ship.OrdinanceMax && !ship.Carrier.HasOrdnanceTransporters,
                        selector: ship => ship.Ordinance);
            if (repairMe == null)
                return;

            module.TransporterTimer = module.TransporterTimerConstant;

            float transferAmount    = module.TransporterOrdnance > module.GetParent().Ordinance
                ? module.GetParent().Ordinance : module.TransporterOrdnance;
            float ordnanceLeft = repairMe.ChangeOrdnance(transferAmount);
            module.GetParent().ChangeOrdnance(ordnanceLeft - transferAmount);
            module.GetParent().AddPower(module.TransporterPower * ((ordnanceLeft - transferAmount) / module.TransporterOrdnance));

            if (Owner.InFrustum)
                GameAudio.PlaySfxAsync("transporter", module.GetParent().SoundEmitter);
        }

        void DoAssaultTransporterLogic(ShipModule module)
        {
            ShipWeight ship = NearByShips.Where(
                    s => s.Ship.loyalty != null && s.Ship.loyalty != Owner.loyalty && s.Ship.shield_power <= 0
                         && Owner.Center.Distance(s.Ship.Center) <= module.TransporterRange + 500f)
                .OrderBy(sw => Owner.Center.SqDist(sw.Ship.Center)).First();
            if (ship.Ship == null) return;

            int troopCount = 0;
            bool transported = false;
            for (int i = 0; i < Owner.TroopList.Count; ++i)
            {
                if (Owner.TroopList[i] == null)
                    continue;
                if (Owner.TroopList[i].Loyalty == Owner.loyalty)
                {
                    ship.Ship.TroopList.Add(Owner.TroopList[i]);
                    Owner.TroopList.Remove(Owner.TroopList[i]);
                    troopCount++;
                    transported = true;
                }
                if (troopCount == module.TransporterTroopAssault)
                    break;
            }
            if (transported) //@todo audio should not be here
            {
                module.TransporterTimer = module.TransporterTimerConstant;
                if (Owner.InFrustum)
                    GameAudio.PlaySfxAsync("transporter");
            }
        }

        void DoReturnToHangar(float elapsedTime)
        {
            if (Owner.Mothership == null || !Owner.Mothership.Active)
            {
                ClearOrders(State);
                if (Owner.shipData.Role == ShipData.RoleName.supply)
                    OrderScrapShip();
                else
                    GoOrbitNearestPlanetAndResupply(true);
                return;
            }
            ThrustOrWarpToPosCorrected(Owner.Mothership.Center, elapsedTime);
            //this looks to need refactor. some of these formulas are... weird
            if (Owner.Center.InRadius(Owner.Mothership.Center, Owner.Mothership.Radius + 300f))
            {
                if (Owner.TroopList.Count == 1)
                    Owner.Mothership.TroopList.Add(Owner.TroopList[0]);
                if (Owner.shipData.Role == ShipData.RoleName.supply) //fbedard: Supply ship return with Ordinance
                    Owner.Mothership.ChangeOrdnance(Owner.Ordinance);
                Owner.Mothership.ChangeOrdnance(Owner.ShipRetrievalOrd);

                Owner.QueueTotalRemoval();
                foreach (ShipModule hangar in Owner.Mothership.Carrier.AllActiveHangars)
                {
                    if (hangar.GetHangarShip() != Owner)
                        continue;
                    //added by gremlin: prevent fighters from relaunching immediately after landing.
                    float ammoReloadTime = Owner.OrdinanceMax * .1f;
                    float shieldRechargeTime = Owner.shield_max * .1f;
                    float powerRechargeTime = Owner.PowerStoreMax * .1f;
                    float rearmTime = Owner.Health;
                    rearmTime += Owner.Ordinance * .1f;
                    rearmTime += Owner.PowerCurrent * .1f;
                    rearmTime += Owner.shield_power * .1f;
                    rearmTime /= Owner.HealthMax + ammoReloadTime + shieldRechargeTime + powerRechargeTime;
                    //this was broken now im not sure.
                    float rearmModifier = hangar.hangarTimerConstant *
                                           (1.01f - (Owner.Level + hangar.GetParent().Level) /10f);
                    // fbedard: rearm time from 50% to 150%
                    rearmTime = (1.01f - rearmTime) * rearmModifier;
                    if (rearmTime < 0)
                        rearmTime = 1;
                    /*CG: if the fighter is fully functional reduce rearm time to very little.
                    The default 5 minute hangar timer is way too high. It cripples fighter usage.
                    at 50% that is still 2.5 minutes if the fighter simply launches and returns.
                    with lag that can easily be 10 or 20 minutes.
                    at 1.01 that should be 3 seconds for the default hangar.
                    */
                    hangar.SetHangarShip(null);
                    hangar.hangarTimer = rearmTime;
                    hangar.HangarShipGuid = Guid.Empty;
                }
            }
        }

        void DoReturnHome(float elapsedTime)
        {
            if (Owner.HomePlanet.Owner != Owner.loyalty)
            {
                // find another friendly planet to land at
                Owner.UpdateHomePlanet(Owner.loyalty.RallyShipYardNearestTo(Owner.Center));
                if (Owner.HomePlanet == null)
                {
                    // Nowhere to land, bye bye.
                    Owner.ScuttleTimer = 1;
                    return;
                }
            }
            ThrustOrWarpToPosCorrected(Owner.HomePlanet.Center, elapsedTime);
            if (Owner.Center.InRadius(Owner.HomePlanet.Center, Owner.HomePlanet.ObjectRadius + 150f))
            {
                Owner.HomePlanet.LandDefenseShip(Owner.DesignRole, Owner.GetCost(Owner.loyalty), Owner.HealthPercent);
                Owner.QueueTotalRemoval();
            }
            if (Owner.InCombat)
            {
                ClearOrders();
            }
        }

        void DoRebaseToShip(float elapsedTime)
        {
            if (EscortTarget == null || !EscortTarget.Active
                                     || EscortTarget.AI.State == AIState.Scrap
                                     || EscortTarget.AI.State == AIState.Refit)
            {
                OrderRebaseToNearest();
                return;
            }

            ThrustOrWarpToPosCorrected(EscortTarget.Center, elapsedTime);
            if (Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))
            {
                if (EscortTarget.TroopCapacity == EscortTarget.TroopList.Count)
                {
                    OrderRebaseToNearest();
                    return;
                }

                Owner.TroopList[0].LandOnShip(EscortTarget);
            }
        }

        void DoSupplyShip(float elapsedTime)
        {
            if (EscortTarget == null || !EscortTarget.Active
                                     || EscortTarget.AI.State == AIState.Resupply
                                     || EscortTarget.AI.State == AIState.Scrap
                                     || EscortTarget.AI.State == AIState.Refit
                                     || EscortTarget.OrdnancePercent >= 0.99f)
            {
                OrderReturnToHangar();
                return;
            }

            ThrustOrWarpToPosCorrected(EscortTarget.Center, elapsedTime);
            if (Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))
            {
                Owner.ChangeOrdnance(EscortTarget.ChangeOrdnance(Owner.Ordinance) - Owner.Ordinance);
                EscortTarget.AI.TerminateResupplyIfDone();
                OrderReturnToHangar();
            }
        }

        void DoResupplyEscort(float elapsedTime, ShipGoal goal)
        {
            if (EscortTarget == null || !EscortTarget.Active
                                     || EscortTarget.AI.State == AIState.Resupply
                                     || EscortTarget.AI.State == AIState.Scrap
                                     || EscortTarget.AI.State == AIState.Refit
                                     || !EscortTarget.SupplyShipCanSupply)
            {
                State = AIState.AwaitingOrders;
                IgnoreCombat = false;
                return;
            }

            var escortVector = EscortTarget.FindStrafeVectorFromTarget(goal.VariableNumber, goal.Direction);
            float distanceToEscortSpot = Owner.Center.Distance(escortVector);
            float supplyShipVelocity   = EscortTarget.Velocity.Length();
            float escortVelocity       = Owner.velocityMaximum;
            if (distanceToEscortSpot < 2000) // ease up thrust on approach to escort spot
                escortVelocity = distanceToEscortSpot / 2000 * Owner.velocityMaximum + supplyShipVelocity + 25;

            if (distanceToEscortSpot > 50)
                ThrustOrWarpToPosCorrected(escortVector, elapsedTime, escortVelocity);
            else
                Owner.Velocity = Vector2.Zero;

            switch (goal.VariableString)
            {
                default:       TerminateResupplyIfDone();                  break;
                case "Rearm":  TerminateResupplyIfDone(SupplyType.Rearm);  break;
                case "Repair": TerminateResupplyIfDone(SupplyType.Repair); break;
                case "Troops": TerminateResupplyIfDone(SupplyType.Troops); break;
            }
        }

        void DoSystemDefense(float elapsedTime)
        {
            SystemToDefend = SystemToDefend ?? Owner.System;
            if (SystemToDefend == null || AwaitClosest?.Owner == Owner.loyalty)
                AwaitOrders(elapsedTime);
            else
                OrderSystemDefense(SystemToDefend);
        }

        void DoTroopToShip(float elapsedTime, ShipGoal goal)
        {
            if (EscortTarget == null || !EscortTarget.Active)
            {
                ClearOrders();
                return;
            }
            SubLightMoveTowardsPosition(EscortTarget.Center, elapsedTime);
            if (Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))
            {
                if (EscortTarget.TroopCapacity > EscortTarget.TroopList.Count)
                {
                    EscortTarget.TroopList.Add(Owner.TroopList[0]);
                    Owner.QueueTotalRemoval();
                    return;
                }
                DoOrbit.Orbit(EscortTarget, elapsedTime);
            }
        }
    }
}