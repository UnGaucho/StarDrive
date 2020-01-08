using System;
using System.Collections.Generic;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        /// <summary>
        /// NOTE: By the original game design, this list contains
        /// our troops and also enemy troops.
        /// It can get mighty confusing, but that's what we got.
        /// </summary>
        readonly Array<Troop> OurTroops = new Array<Troop>();
        readonly Array<Troop> HostileTroops = new Array<Troop>();

        // OUR troops count
        public int TroopCount => OurTroops.Count;

        // TRUE if we have any troops present on this ship
        // @warning Some of these MAY be enemy troops!
        public bool HasOurTroops => OurTroops.Count > 0;
        
        // TRUE if we have own troops or if we launched these troops on to operations
        public bool HasTroopsPresentOrLaunched => HasOurTroops || Carrier.LaunchedAssaultShuttles > 0;

        public bool TroopsAreBoardingShip => HostileTroops.Count > 0;
        public int NumPlayerTroopsOnShip  => loyalty.isPlayer ? OurTroops.Count : HostileTroops.Count;
        public int NumAiTroopsOnShip      => loyalty.isPlayer ? HostileTroops.Count : OurTroops.Count;

        // NOTE: could be an enemy troop or a friendly one
        public void AddTroop(Troop troop)
        {
            if (troop.Loyalty == loyalty)
                OurTroops.Add(troop);
            else
                HostileTroops.Add(troop);
        }

        public void RemoveAnyTroop(Troop troop)
        {
            if (troop.Loyalty == loyalty)
                OurTroops.RemoveRef(troop);
            else
                HostileTroops.RemoveRef(troop);
        }

        public float GetOurTroopStrength(int maxTroops)
        {
            float strength = 0;
            for (int i = 0; i < maxTroops && i < OurTroops.Count; ++i)
                strength += OurTroops[i].Strength;
            return strength;
        }

        public int NumTroopsRebasingHere
        {
            get
            {
                using (loyalty.GetShips().AcquireReadLock())
                {
                    return loyalty.GetShips().Count(s => s.AI.State == AIState.RebaseToShip &&
                                                    s.AI.EscortTarget == this);
                }
            }
        }

        public bool TryLandSingleTroopOnShip(Ship targetShip)
        {
            if (OurTroops.Count > 0)
            {
                OurTroops[0].LandOnShip(targetShip);
                return true;
            }
            return false;
        }

        public bool TryLandSingleTroopOnPlanet(Planet targetPlanet)
        {
            return OurTroops.Count > 0 && OurTroops[0].TryLandTroop(targetPlanet);
        }

        public bool GetOurFirstTroop(out Troop troop)
        {
            if (OurTroops.Count > 0)
            {
                troop = OurTroops[0];
                return true;
            }
            troop = null;
            return false;
        }

        public IReadOnlyList<Troop> GetOurTroops(int maxTroops = 0)
        {
            if (maxTroops == 0 || maxTroops >= OurTroops.Count)
                return OurTroops;

            var troops = new Array<Troop>(maxTroops);
            for (int i = 0; i < maxTroops; ++i)
                troops.Add(OurTroops[i]);
            return troops;
        }
        
        public Array<Troop> GetFriendlyAndHostileTroops()
        {
            var all = new Array<Troop>(OurTroops.Count + HostileTroops.Count);
            all.AddRange(OurTroops);
            all.AddRange(HostileTroops);
            return all;
        }

        public int LandTroopsOnShip(Ship targetShip, int maxTroopsToLand = 0)
        {
            int landed = 0;
            // NOTE: Need to create a copy of TroopList here,
            // because `LandOnShip` will modify TroopList
            Troop[] troopsToLand = OurTroops.ToArray();
            for (int i = 0; i < troopsToLand.Length; ++i)
            {
                if (maxTroopsToLand != 0 && landed >= maxTroopsToLand)
                    break;

                troopsToLand[i].LandOnShip(targetShip);
                ++landed;
            }
            return landed;
        }

        // This will launch troops without having issues with modifying it's own TroopsHere
        public int LandTroopsOnPlanet(Planet planet, int maxTroopsToLand = 0)
        {
            int landed = 0;
            // @note: Need to create a copy of TroopList here,
            // because `TryLandTroop` will modify TroopList if landing is successful
            Troop[] troopsToLand = OurTroops.ToArray();
            for (int i = 0; i < troopsToLand.Length; ++i)
            {
                if (maxTroopsToLand != 0 && landed >= maxTroopsToLand)
                    break;

                if (troopsToLand[i].TryLandTroop(planet))
                    ++landed;
                else break; // no more free tiles, probably
            }
            return landed;
        }

        void SpawnTroopsForNewShip(ShipModule module)
        {
            string troopType    = "Wyvern";
            string tankType     = "Wyvern";
            string redshirtType = "Wyvern";

            IReadOnlyList<Troop> unlockedTroops = loyalty?.GetUnlockedTroops();
            if (unlockedTroops?.Count > 0)
            {
                troopType    = unlockedTroops.FindMax(troop => troop.SoftAttack).Name;
                tankType     = unlockedTroops.FindMax(troop => troop.HardAttack).Name;
                redshirtType = unlockedTroops.FindMin(troop => troop.SoftAttack).Name; // redshirts are weakest
                troopType    = (troopType == redshirtType) ? tankType : troopType;
            }

            for (int i = 0; i < module.TroopsSupplied; ++i) // TroopLoad (?)
            {
                string type = troopType;
                int numHangarsBays = Carrier.AllTroopBays.Length;
                if (numHangarsBays < OurTroops.Count + 1) //FB: if you have more troop_capacity than hangars, consider adding some tanks
                {
                    type = troopType; // ex: "Space Marine"
                    if (OurTroops.Count(troop => troop.Name == tankType) <= numHangarsBays)
                        type = tankType;
                    // number of tanks will be up to number of hangars bays you have. If you have 8 barracks and 8 hangar bays
                    // you will get 8 infantry. if you have  8 barracks and 4 bays, you'll get 4 tanks and 4 infantry .
                    // If you have  16 barracks and 4 bays, you'll still get 4 tanks and 12 infantry.
                    // logic here is that tanks needs hangarbays and barracks, and infantry just needs barracks.
                }

                Troop newTroop = ResourceManager.CreateTroop(type, loyalty);
                newTroop.LandOnShip(this);
            }
        }

        void UpdateTroopBoardingDefense()
        {
            TroopBoardingDefense = 0f;
            for (int i = 0; i < OurTroops.Count; i++)
            {
                Troop troop = OurTroops[i];
                troop.SetShip(this);
                TroopBoardingDefense += troop.Strength;
            }
            for (int i = 0; i < HostileTroops.Count; ++i)
            {
                HostileTroops[i].SetShip(this);
            }
        }

        void RefreshMechanicalBoardingDefense()
        {
            MechanicalBoardingDefense =  ModuleSlotList.Sum(module => module.MechanicalBoardingDefense);
        }

        void DisengageExcessTroops(int troopsToRemove) // excess troops will leave the ship, usually after successful boarding
        {
            Troop[] toRemove = OurTroops.ToArray();
            for (int i = 0; i < troopsToRemove && i < toRemove.Length; ++i)
            {
                Troop troop = toRemove[i];
                Ship assaultShip = CreateTroopShipAtPoint(GetAssaultShuttleName(loyalty), loyalty, Center, troop);
                assaultShip.Velocity = UniverseRandom.RandomDirection() * assaultShip.SpeedLimit + Velocity;

                Ship friendlyTroopShipToRebase = FindClosestAllyToRebase(assaultShip);

                bool rebaseSucceeded = false;
                if (friendlyTroopShipToRebase != null)
                    rebaseSucceeded = friendlyTroopShipToRebase.Carrier.RebaseAssaultShip(assaultShip);

                if (!rebaseSucceeded) // did not found a friendly troopship to rebase to
                    assaultShip.AI.OrderRebaseToNearest();

                if (assaultShip.AI.State == AIState.AwaitingOrders) // nowhere to rebase
                    assaultShip.DoEscort(this);
            }
        }

        Ship FindClosestAllyToRebase(Ship ship)
        {
            ship.AI.ScanForCombatTargets(ship, ship.SensorRange); // to find friendlies nearby
            return ship.AI.FriendliesNearby.FindMinFiltered(
                troopShip => troopShip.Carrier.NumTroopsInShipAndInSpace < troopShip.TroopCapacity &&
                             troopShip.Carrier.HasActiveTroopBays,
                troopShip => ship.Center.SqDist(troopShip.Center));
        }

        void HealOurTroops()
        {
            for (int i = 0; i < OurTroops.Count; ++i)
            {
                Troop troop = OurTroops[i];
                troop.Strength = (troop.Strength += HealPerTurn).Clamped(0, troop.ActualStrengthMax);
            }
        }
        
        void UpdateTroops()
        {
            UpdateTroopBoardingDefense();

            if (HealPerTurn > 0)
                HealOurTroops();

            if (OurTroops.Count > 0)
            {
                // leave a garrison of 1 if a ship without barracks was boarded
                int troopThreshold = TroopCapacity + (TroopCapacity > 0 ? 0 : 1);
                if (!InCombat && HostileTroops.Count == 0 && OurTroops.Count > troopThreshold)
                {
                    DisengageExcessTroops(OurTroops.Count - troopThreshold);
                }
            }


            if (HostileTroops.Count > 0) // Combat!!
            {
                var combatTurn = new ShipTroopCombatTurn(this);
                combatTurn.ResolveCombat();
            }
        }

        struct ShipTroopCombatTurn
        {
            readonly Ship Ship;

            public ShipTroopCombatTurn(Ship ship)
            {
                Ship = ship;
            }

            public void ResolveCombat()
            {
                ResolveOwnVersusEnemy();
                ResolveEnemyVersusOwn();

                if (Ship.OurTroops.Count == 0 &&
                    Ship.MechanicalBoardingDefense <= 0f &&
                    Ship.HostileTroops.Count > 0) // enemy troops won:
                {
                    Ship.ChangeLoyalty(changeTo: Ship.HostileTroops[0].Loyalty);
                    Ship.RefreshMechanicalBoardingDefense();
                }
            }

            void ResolveOwnVersusEnemy()
            {
                if (Ship.OurTroops.Count == 0 || Ship.HostileTroops.Count == 0)
                    return;

                float ourCombinedDefense = 0f;

                for (int i = 0; i < Ship.MechanicalBoardingDefense; ++i)
                    if (UniverseRandom.RollDice(50f)) // 50%
                        ourCombinedDefense += 1f;

                foreach (Troop troop in Ship.OurTroops)
                {
                    for (int i = 0; i < troop.Strength; ++i)
                        if (UniverseRandom.RollDice(troop.BoardingStrength))
                            ourCombinedDefense += 1f;
                }

                foreach (Troop badBoy in Ship.HostileTroops.ToArray()) // BadBoys will be modified
                {
                    if (ourCombinedDefense > 0)
                        badBoy.DamageTroop(Ship, ref ourCombinedDefense);
                    else break;
                }
            }

            void ResolveEnemyVersusOwn()
            {
                if (Ship.HostileTroops.Count == 0)
                    return;

                float enemyAttackPower = 0;
                foreach (Troop troop in Ship.HostileTroops)
                {
                    for (int i = 0; i < troop.Strength; ++i)
                        if (UniverseRandom.RollDice(troop.BoardingStrength))
                            enemyAttackPower += 1.0f;
                }

                foreach (Troop goodGuy in Ship.OurTroops.ToArray()) // OurTroops will be modified
                {
                    if (enemyAttackPower > 0)
                        goodGuy.DamageTroop(Ship, ref enemyAttackPower);
                    else break;
                }

                // spend rest of the attack strength to weaken mechanical defenses:
                if (enemyAttackPower > 0)
                {
                    Ship.MechanicalBoardingDefense = Math.Max(Ship.MechanicalBoardingDefense - enemyAttackPower, 0);
                }
            }
        }
    }
}