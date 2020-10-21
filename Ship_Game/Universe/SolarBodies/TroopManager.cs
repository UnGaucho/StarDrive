﻿using Ship_Game.Gameplay;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Audio;
using Ship_Game.GameScreens.DiplomacyScreen;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class TroopManager // Refactored by Fat Bastard. Feb 6, 2019
    {
        private readonly Planet Ground;

        private Empire Owner           => Ground.Owner;
        public bool RecentCombat       => InCombatTimer > 0.0f;
        private bool NoTroopsOnPlanet  => Ground.TroopsHere.Count <= 0;
        private bool TroopsAreOnPlanet => Ground.TroopsHere.Count > 0;

        private Array<PlanetGridSquare> TilesList => Ground.TilesList;
        private Array<Building> BuildingList      => Ground.BuildingList;
        private SolarSystem ParentSystem          => Ground.ParentSystem;
        public int NumEmpireTroops(Empire us)     => TroopList.Count(t => t.Loyalty == us);
        public int NumDefendingTroopCount         => NumEmpireTroops(Owner);
        public bool WeHaveTroopsHere(Empire us)   => TroopList.Any(t => t.Loyalty == us);
        public bool WeAreInvadingHere(Empire us)  => WeHaveTroopsHere(us) && Ground.Owner != us;
        public bool ForeignTroopHere(Empire us)   => TroopList.Any(t => t.Loyalty != null && t.Loyalty != us);
        public bool MightBeAWarZone(Empire us)    => RecentCombat || Ground.SpaceCombatNearPlanet || ForeignTroopHere(us);
        public bool MightBeAWarZone()             => MightBeAWarZone(Ground.Owner);
        public float OwnerTroopStrength           => TroopList.Sum(troop => troop.Loyalty == Owner ? troop.Strength : 0);

        private BatchRemovalCollection<Troop> TroopList      => Ground.TroopsHere;
        private BatchRemovalCollection<Combat> ActiveCombats => Ground.ActiveCombats;

        private float DecisionTimer = 0.5f;        
        private float InCombatTimer;
        private int NumInvadersLast;

        public void SetInCombat(float timer = 10f)
        {
            InCombatTimer = timer;
        }

        // ReSharper disable once UnusedParameter.Local Habital concept here is to not use this class if the planet cant have
        // ground combat. but that will be a future project. 
        public TroopManager(Planet planet)      
        {
            Ground = planet;
        }

        public void Update(FixedSimTime timeStep)
        {
            if (Empire.Universe.Paused) 
                return;

            DecisionTimer -= timeStep.FixedTime;
            InCombatTimer -= timeStep.FixedTime;
            if (RecentCombat || TroopsAreOnPlanet)
            {
                ResolvePlanetaryBattle(timeStep);
                if (DecisionTimer <= 0)
                {
                    MakeCombatDecisions();
                    DecisionTimer = 0.5f;
                }
                DoBuildingTimers(timeStep);
                DoTroopTimers(timeStep);
            }
        }

        private void MakeCombatDecisions()
        {
            if (!ForeignTroopHere(Owner) && !Ground.EventsOnBuildings())
                return;

            for (int i = 0; i < TilesList.Count; ++i)
            {
                PlanetGridSquare tile = TilesList[i];
                if (tile.TroopsAreOnTile)
                    PerformTroopsGroundActions(tile);

                else if (tile.BuildingPerformsAutoCombat(Ground))
                    PerformGroundActions(tile.building, tile);
            }
        }

        private void PerformTroopsGroundActions(PlanetGridSquare tile)
        {
            using (tile.TroopsHere.AcquireWriteLock())
            {
                for (int i = 0; i < tile.TroopsHere.Count; ++i)
                {
                    Troop troop = tile.TroopsHere[i];
                    PerformGroundActions(troop, tile);
                }
            }
        }

        private void PerformGroundActions(Building b, PlanetGridSquare ourTile)
        {
            if (!b.CanAttack || Ground.Owner == null)
                return;

            // scan for enemies
            if (!SpotClosestHostile(ourTile, Owner, out PlanetGridSquare nearestTargetTile))
                return; // no targets on planet

            // find range
            if (!nearestTargetTile.InRangeOf(ourTile, 1) || nearestTargetTile.EventOnTile) // right now all buildings have range 1
                return;

            // lock on enemy troop target
            if (nearestTargetTile.LockOnEnemyTroop(Owner, out Troop enemy))
            {
                // start combat
                b.UpdateAttackActions(-1);
                CombatScreen.StartCombat(b, enemy, nearestTargetTile, Ground);
            }
        }

        private void PerformGroundActions(Troop t, PlanetGridSquare ourTile)
        {
            if (!t.CanMove && !t.CanAttack)
                return;

            // find target to attack
            if (t.CanAttack && t.AcquireTarget(ourTile, Ground, out PlanetGridSquare targetTile))
            {
                // start combat
                t.UpdateAttackActions(-1);

                t.FaceEnemy(targetTile, ourTile);
                if (targetTile.CombatBuildingOnTile)
                {
                    t.UpdateMoveActions(-1);
                    CombatScreen.StartCombat(t, targetTile.building, targetTile, Ground);
                }
                else if (targetTile.LockOnEnemyTroop(t.Loyalty, out Troop enemy))
                {
                    if (enemy.Strength.LessOrEqual(0))
                    {
                        // Workaround for negative troop health
                        Log.Warning($"{enemy.Name} health is less or 0, on planet {Ground.Name}");
                        enemy.DamageTroop(1, Ground, targetTile, out _);
                    }
                    CombatScreen.StartCombat(t, enemy, targetTile, Ground);
                    if (t.ActualRange == 1)
                        MoveTowardsTarget(t, ourTile, targetTile); // enter the same tile 
                    else
                        t.UpdateMoveActions(-1);
                }
            }
            else // Move to target
            {
                // scan for closest enemy
                if (!SpotClosestHostile(ourTile, t.Loyalty, out PlanetGridSquare nearestTargetTile))
                    return; // No targets on planet. No need to move

                MoveTowardsTarget(t, ourTile, nearestTargetTile);

                // resolve possible events 
                ResolveEvents(ourTile, nearestTargetTile, t.Loyalty);
            }
        }

        private void ResolveEvents(PlanetGridSquare troopTile, PlanetGridSquare possibleEventTile, Empire empire)
        {
            if (troopTile == possibleEventTile)
                possibleEventTile.CheckAndTriggerEvent(Ground, empire);
        }

        private void MoveTowardsTarget(Troop t, PlanetGridSquare ourTile, PlanetGridSquare targetTile)
        {
            if (!t.CanMove || ourTile == targetTile)
                return;

            TileDirection direction     = ourTile.GetDirectionTo(targetTile);
            PlanetGridSquare moveToTile = PickTileToMoveTo(direction, ourTile, t.Loyalty);
            if (moveToTile == null)
                return; // no free tile

            // move to selected direction
            t.SetFromRect(t.ClickRect);
            t.MovingTimer = 0.75f;
            t.UpdateMoveActions(-1);
            t.ResetMoveTimer();
            moveToTile.AddTroop(t);
            ourTile.TroopsHere.Remove(t);
        }
        
        // try 3 directions to move into, based on general direction to the target
        private PlanetGridSquare PickTileToMoveTo(TileDirection direction, PlanetGridSquare ourTile, Empire us)
        {
            PlanetGridSquare bestFreeTile = FreeTile(direction, ourTile, us);
            if (bestFreeTile != null)
                return bestFreeTile;

            // try alternate tiles
            switch (direction)
            {
                case TileDirection.North:     return FreeTile(TileDirection.NorthEast, ourTile, us) ?? 
                                                     FreeTile(TileDirection.NorthWest, ourTile, us);
                case TileDirection.South:     return FreeTile(TileDirection.SouthEast, ourTile, us) ??
                                                     FreeTile(TileDirection.SouthWest, ourTile, us);
                case TileDirection.East:      return FreeTile(TileDirection.NorthEast, ourTile, us) ??
                                                     FreeTile(TileDirection.SouthEast, ourTile, us);
                case TileDirection.West:      return FreeTile(TileDirection.NorthWest, ourTile, us) ??
                                                     FreeTile(TileDirection.SouthWest, ourTile, us);
                case TileDirection.NorthEast: return FreeTile(TileDirection.North, ourTile, us) ??
                                                     FreeTile(TileDirection.East, ourTile, us);
                case TileDirection.NorthWest: return FreeTile(TileDirection.North, ourTile, us) ??
                                                     FreeTile(TileDirection.West, ourTile, us);
                case TileDirection.SouthEast: return FreeTile(TileDirection.South, ourTile, us) ??
                                                     FreeTile(TileDirection.East, ourTile, us);
                case TileDirection.SouthWest: return FreeTile(TileDirection.South, ourTile, us) ??
                                                     FreeTile(TileDirection.West, ourTile, us);
                default: return null;
            }
        }

        private PlanetGridSquare FreeTile(TileDirection direction, PlanetGridSquare ourTile, Empire us)
        {
            Point newCords        = ourTile.ConvertDirectionToCoordinates(direction);
            PlanetGridSquare tile = Ground.GetTileByCoordinates(newCords.X, newCords.Y);

            if (tile != null && tile.IsTileFree(us) && tile != ourTile)
                return tile;
            return null;
        }

        bool SpotClosestHostile(PlanetGridSquare spotterTile, Empire spotterOwner, out PlanetGridSquare targetTile)
        {
            targetTile = null;
            if (spotterTile.LockOnEnemyTroop(spotterOwner, out _))
            {
                targetTile = spotterTile; // We have enemy on our tile
            }
            else
            {
                foreach (PlanetGridSquare scannedTile in TilesList.OrderBy(tile =>
                    Math.Abs(tile.x - spotterTile.x) + Math.Abs(tile.y - spotterTile.y)))
                {
                    if (scannedTile.HostilesTargetsOnTile(spotterOwner, Owner))
                    {
                        targetTile = scannedTile;
                        break;
                    }
                }
            }

            return targetTile != null;
        }

        private void DoBuildingTimers(FixedSimTime timeStep)
        {
            if (BuildingList.Count <= 0)
                return;

            for (int i = 0; i < BuildingList.Count; i++)
            {
                Building building = BuildingList[i];
                if (building == null || !building.IsAttackable)
                    continue;

                building.UpdateAttackTimer(-timeStep.FixedTime);
                if (building.AttackTimer < 0.0)
                {
                    building.UpdateAttackActions(1);
                    building.ResetAttackTimer();
                }
            }
        }

        private void DoTroopTimers(FixedSimTime timeStep)
        {
            if (NoTroopsOnPlanet)
                return;

            for (int x = TroopList.Count - 1; x >= 0; x--)
            {
                Troop troop = TroopList[x];
                if (troop == null)
                    continue;

                if (troop.Strength <= 0f)
                {
                    for (int i = TilesList.Count - 1; i >= 0; i--)
                    {
                        TilesList[i].TroopsHere.RemoveRef(troop);
                    }
                    TroopList.RemoveAtSwapLast(x);
                    continue;
                }

                troop.UpdateLaunchTimer(-timeStep.FixedTime);
                troop.UpdateMoveTimer(-timeStep.FixedTime);
                troop.MovingTimer -= timeStep.FixedTime;
                if (troop.MoveTimer < 0f)
                {
                    troop.UpdateMoveActions(troop.MaxStoredActions);
                    troop.ResetMoveTimer();
                }

                troop.UpdateAttackTimer(-timeStep.FixedTime);
                if (troop.AttackTimer < 0f)
                {
                    troop.UpdateAttackActions(troop.MaxStoredActions);
                    troop.ResetAttackTimer();
                }
            }
        }

        private void ResolveTacticalCombats(FixedSimTime timeStep, bool isViewing = false)
        {
            using (ActiveCombats.AcquireReadLock())
                for (int i = ActiveCombats.Count - 1; i >= 0; i--)
                {
                    Combat combat = ActiveCombats[i];
                    if (combat.Done)
                    {
                        ActiveCombats.QueuePendingRemoval(combat);
                        break;
                    }

                    combat.Timer -= timeStep.FixedTime;
                    if (combat.Timer < 3.0 && combat.Phase == 1)
                        combat.ResolveDamage(isViewing);
                    else if (combat.Phase == 2)
                        ActiveCombats.QueuePendingRemoval(combat);
                }
        }

        private void ResolveDiplomacy(int invadingForces, Empire invadingEmpire)
        {
            if (invadingForces <= NumInvadersLast || NumInvadersLast != 0)
                return; // FB - nothing to change if no new troops invade

            Empire player = Empire.Universe.PlayerEmpire;

            if (Owner.isPlayer) // notify player of invasion
            {
                Empire.Universe.NotificationManager.AddEnemyTroopsLandedNotification(Ground, invadingEmpire, Owner);
            }
            else if (invadingEmpire.isPlayer && !Owner.isFaction && !player.IsAtWarWith(Owner))
            {
                if (player.IsNAPactWith(Owner))
                {
                    DiplomacyScreen.Show(Owner, "Invaded NA Pact", ParentSystem);
                    player.GetEmpireAI().DeclareWarOn(Owner, WarType.ImperialistWar);
                    Owner.GetRelations(player).Trust -= 50f;
                    Owner.GetRelations(player).AddAngerDiplomaticConflict(50);
                }
                else
                {
                    DiplomacyScreen.Show(Owner, "Invaded Start War", ParentSystem);
                    player.GetEmpireAI().DeclareWarOn(Owner, WarType.ImperialistWar);
                    Owner.GetRelations(player).Trust -= 25f;
                    Owner.GetRelations(player).AddAngerDiplomaticConflict(25);
                }
            }
        }

        private void ResolvePlanetaryBattle(FixedSimTime timeStep)
        {
            if (Empire.Universe.LookingAtPlanet 
                && Empire.Universe.workersPanel is CombatScreen screen 
                && screen.p == Ground)
            {
                ResolveTacticalCombats(timeStep, isViewing: true);
            }
            else
            {
                ResolveTacticalCombats(timeStep);
            }

            if (ActiveCombats.Count > 0)
                InCombatTimer = 10f;

            ActiveCombats.ApplyPendingRemovals();
            if (NoTroopsOnPlanet || Owner == null)
                return;

            var forces = new Forces(Owner, TilesList);
            ResolveDiplomacy(forces.InvadingForces, forces.InvadingEmpire);
            NumInvadersLast = forces.InvadingForces;

            if (forces.InvadingForces <= 0 || forces.DefendingForces != 0)
                return; // Planet is still fighting or all invading forces are destroyed

            Ground.ChangeOwnerByInvasion(forces.InvadingEmpire, Ground.Level);
        }

        public float GroundStrength(Empire empire)
        {
            float strength = 0;
            if (Owner == empire)
                strength += BuildingList.Sum(BuildingCombatStrength);

            using (TroopList.AcquireReadLock())
                strength += TroopList.Where(t => t.Loyalty == empire).Sum(str => str.Strength);
            return strength;
        }

        public float TroopStrength()
        {
            float strength = 0;

            using (TroopList.AcquireReadLock())
                strength += TroopList.Where(t => t.Loyalty == Ground.Owner).Sum(str => str.Strength);
            return strength;
        }

        public int GetPotentialGroundTroops()
        {
            int num = 0;

            foreach (PlanetGridSquare pgs in TilesList)
            {
                num += pgs.MaxAllowedTroops;
            }
            return num;
        }

        public float GroundStrengthOther(Empire allButThisEmpire)
        {
            float enemyTroopStrength = TroopList.Where(t => 
                t.OwnerString != allButThisEmpire.data.Traits.Name).Sum(t => t.ActualStrengthMax);

            if (Owner == allButThisEmpire)
                return enemyTroopStrength; // The planet is ours, so no need to check from buildings

            for (int i = 0; i < BuildingList.Count; i++)
            {
                Building b;
                try
                {
                    b = BuildingList[i];
                }
                catch
                {
                    continue;
                }
                enemyTroopStrength += BuildingCombatStrength(b);
            }
            return enemyTroopStrength;            
        }

        float BuildingCombatStrength(Building b)
        {
            float strength = 0;
            if (b?.IsMilitary == true)
            {
                strength += b.CombatStrength;
                strength += b.InvadeInjurePoints * 5;
            }
            return strength;
        }

        public bool TroopsHereAreEnemies(Empire empire)
        {
            bool enemies = false;
            using (TroopList.AcquireReadLock())
                foreach (Troop t in TroopList)
                {
                    if (t.Loyalty == empire)
                        continue;

                    if (!empire.GetRelations(t.Loyalty, out Relationship trouble) || trouble.AtWar)
                    {
                        enemies = true;
                        break;
                    }
                }
            return enemies;
        }

        public int NumFreeTiles(Empire empire)
        {
            return TilesList.Count(t => t.IsTileFree(empire));
        }

        public int GetEnemyAssets(Planet planet, Empire empire)
        {
            int numTroops          = planet.TroopsHere.Count(t => t.Loyalty != empire);
            int numCombatBuildings = planet.Owner != empire ? planet.BuildingList.Count(b => b.IsAttackable) : 0;

            return numTroops + numCombatBuildings;
        }

        public Array<Troop> EmpireTroops(Empire empire, int maxToTake)
        {
            var troops = new Array<Troop>();
            for (int x = 0; x < TroopList.Count; x++)
            {
                Troop troop = TroopList[x];
                if (troop.Loyalty != empire) continue;

                if (maxToTake-- > 0)
                    troops.Add(troop);
            }

            return troops;
        }

        //Added by McShooterz: heal builds and troops every turn
        public void HealTroops(int healAmount)
        {
            if (RecentCombat)
                return;

            using (TroopList.AcquireReadLock())
                foreach (Troop troop in TroopList)
                    troop.HealTroop(healAmount);
        }

        public struct Forces
        {
            public readonly int DefendingForces;
            public readonly int InvadingForces;
            public readonly Empire InvadingEmpire;

            public Forces(Empire defendingEmpire, Array<PlanetGridSquare> tileList)
            {
                DefendingForces = 0;
                InvadingForces  = 0;
                InvadingEmpire  = null;
                for (int i = 0; i < tileList.Count; i++)
                {
                    PlanetGridSquare tile = tileList[i];
                    var troops = tile.TroopsHere.GetInternalArrayItems();
                    for (int x = troops.Length - 1; x >= 0; x--)
                    {
                        Troop troop = troops[x];
                        if (troop != null)
                        {
                            if (troop.Loyalty != null && troop.Loyalty != defendingEmpire)
                            {
                                ++InvadingForces;
                                // This is broken. Multiple invading empire will switch this over and over
                                // making this value unpredictable.
                                InvadingEmpire = troop.Loyalty; 
                            }
                            else
                                ++DefendingForces;
                        }
                    }

                    if (tile.CombatBuildingOnTile)
                        ++DefendingForces;
                }
            }
        }
    }

    public enum TargetType
    {
        Soft,
        Hard
    }
}