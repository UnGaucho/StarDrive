using System;
using System.Diagnostics;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    /// <summary>
    /// used to classify a group of ships into fleets according to the fleet ratios.
    /// usage: create class and give it ships. it will talley their fleet characteristics
    /// and provide methods for extracting ship sets used for fleets.
    /// in general before extracting make sure that the tallies at least match what is wanted. 
    /// </summary>
    public class FleetShips
    {
        public float AccumulatedStrength { get; private set; }
        private readonly Empire OwnerEmpire;
        private readonly FleetRatios Ratios;
        private Array<Ship> Ships = new Array<Ship>();
        public float WantedFleetCompletePercentage = 0.25f;
        public int InvasionTroops { get; private set; }
        public float InvasionTroopStrength { get; private set; }
        public int BombSecsAvailable { get; private set; }

        public FleetShips(Empire ownerEmpire)
        {
            OwnerEmpire = ownerEmpire;
            Ratios = new FleetRatios(OwnerEmpire);
        }

        public FleetShips(Empire ownerEmpire, Ship[] ships) : this(ownerEmpire)
        {
            foreach (var ship in ships) AddShip(ship);
        }

        public bool AddShip(Ship ship)
        {
            if (!ship.ShipIsGoodForGoals())
                return false;

            if (ship.fleet != null)
            {
                Log.Error($"FleetRatios: attempting to add a ship already in a fleet '{ship.fleet.Name}'. removing from fleet");
                ship.ClearFleet();
                foreach (var fleet in OwnerEmpire.GetFleetsDict())
                {
                    fleet.Value.RemoveShip(ship);
                }
            }

            if (ship.IsPlatformOrStation
                || ship.AI.BadGuysNear
                || ship.engineState == Ship.MoveState.Warp
                || ship.fleet != null
                || ship.Mothership != null
                || ship.AI.State == AIState.Scrap
                || ship.AI.State == AIState.Resupply
                || ship.AI.State == AIState.Refit
                || ship.fleet != null)
                return false;

            AccumulatedStrength += ship.GetStrength();
            Ships.Add(ship);

            if (ShipData.ShipRoleToRoleType(ship.DesignRole) == ShipData.RoleType.Troop)
            {
                InvasionTroops += ship.Carrier.PlanetAssaultCount;
                InvasionTroopStrength += ship.Carrier.PlanetAssaultStrength;
            }
            if (ship.DesignRole == ShipData.RoleName.bomber)
                BombSecsAvailable += ship.BombsGoodFor60Secs;

            return true;
        }

        public int ExtractFleetShipsUpToStrength(float strength, float setCompletePercent, int wantedFleetCount,
            out Array<Ship> ships)
        {
            float accumulatedStrength;
            int completeFleets = 0;
            ships = new Array<Ship>();
            int fleetCount = wantedFleetCount.LowerBound(1);
            var utilityShips = new Array<Ship>();
            do
            {
                var gatheredShips = GetCoreFleet();
                if (gatheredShips.IsEmpty)
                    break;

                accumulatedStrength = gatheredShips.Sum(s => s.GetStrength());
                ships.AddRange(gatheredShips);


                if (ships.Count >= Ratios.MinCombatFleet * setCompletePercent)
                {
                    completeFleets++;
                    fleetCount--;
                }
            }
            while (accumulatedStrength <= strength || fleetCount > 0);

            if (ships.Sum(s => s.GetStrength()) >= strength && fleetCount <= 0)
                completeFleets = wantedFleetCount;

            for (int x =0; x< (wantedFleetCount + completeFleets).LowerBound(1); x++)
                utilityShips.AddRange(GetSupplementalFleet());

            ships.AddRange(utilityShips);
            return completeFleets;
        }

        public Array<Ship> GetBasicFleet()
        {
            var ships = new Array<Ship>();
            ships.AddRange(ExtractShips(Ships, Ratios.MinFighters, ShipData.RoleName.fighter));
            ships.AddRange(ExtractShips(Ships, Ratios.MinCorvettes, ShipData.RoleName.corvette));
            ships.AddRange(ExtractShips(Ships, Ratios.MinFrigates, ShipData.RoleName.frigate));
            ships.AddRange(ExtractShips(Ships, Ratios.MinCruisers, ShipData.RoleName.cruiser));
            ships.AddRange(ExtractShips(Ships, Ratios.MinCapitals, ShipData.RoleName.capital));
            return ships;
        }

        public Array<Ship> GetCoreFleet()
        {
            var ships = new Array<Ship>();
            ships.AddRange(ExtractShips(Ships, Ratios.MinFighters, ShipData.RoleName.fighter));
            ships.AddRange(ExtractShips(Ships, Ratios.MinCorvettes, ShipData.RoleName.corvette));
            ships.AddRange(ExtractShips(Ships, Ratios.MinFrigates, ShipData.RoleName.frigate));
            ships.AddRange(ExtractShips(Ships, Ratios.MinCruisers, ShipData.RoleName.cruiser));
            ships.AddRange(ExtractShips(Ships, Ratios.MinCapitals, ShipData.RoleName.capital));
            return ships;
        }

        public Array<Ship> GetSupplementalFleet()
        {
            var ships = new Array<Ship>();
            ships.AddRange(ExtractShips(Ships, Ratios.MinCarriers, ShipData.RoleName.carrier));
            ships.AddRange(ExtractShips(Ships, Ratios.MinSupport, ShipData.RoleName.support));
            return ships;
        }

        public Array<Ship> ExtractSetsOfCombatShips(float strength, float setCompletePercentage, int wantedFleetCount , out int fleetCount)
        {
            Array<Ship> ships = new Array<Ship>();
            fleetCount = ExtractFleetShipsUpToStrength(strength, setCompletePercentage, wantedFleetCount,
                out Array<Ship> fleetShips);
            if (fleetCount > 0)
                ships.AddRange(fleetShips);

            return ships;
        }

        public Array<Ship> ExtractTroops(int planetAssaultTroopsWanted)
        {
            var ships = ExtractShipsByFeatures(Ships, planetAssaultTroopsWanted, 1, 1
                                , s =>
                                    {
                                        if (s.DesignRoleType == ShipData.RoleType.Troop)
                                            return s.Carrier.PlanetAssaultCount;
                                        return 0;
                                    });
            return ships;
        }

        public Array<Ship> ExtractBombers(int bombSecsWanted, int fleetCount)
        {
            var ships = new Array<Ship>();
            if (bombSecsWanted > 0 && Ratios.MinBombers > 0)
                ships = ExtractShipsByFeatures(Ships, bombSecsWanted, 1
                                    , fleetCount * (int)Ratios.MinBombers
                                    , s =>
                                    {
                                        if (s.DesignRole == ShipData.RoleName.bomber)
                                            return s.BombsGoodFor60Secs;
                                        return 0;
                                    });
            return ships;
        }

        private Array<Ship> ExtractShips(Array<Ship> ships, float wanted, ShipData.RoleName role)
            => ExtractShips(ships, wanted, s => s.DesignRole == role);

        private Array<Ship> ExtractShips(Array<Ship> ships, float wanted, Func<Ship, bool> shipFilter)
        {
            var shipSet = new Array<Ship>();
            if (wanted > 0)
                for (int x = ships.Count - 1; x >= 0; x--)
                {
                    Ship ship = ships[x];
                    if (!shipFilter(ship))
                        continue;

                    shipSet.Add(ship);
                    Ships.RemoveSwapLast(ship);
                    AccumulatedStrength += ship.GetStrength();
                    if (shipSet.Count >= wanted)
                        break;
                }

            return shipSet;
        }
        /// <summary>
        /// Extracts ships with wanted feature up to the count of the wanted feature.
        /// ex. I want 10 bombs. give me as many ships as it takes to get 10 bombs.
        /// </summary>
        /// <param name="ships">Ships to be processed</param>
        /// /// <param name="totalFeatureWanted">max of total features found on all matching ships</param>
        /// <param name="minWantedFeature">min count of feature the ship should have</param>
        /// <param name="shipFeatureCount">Number of times ship matches the feature.
        /// like ship.BombsGoodFor60Secs</param>
        /// <returns></returns>
        private Array<Ship> ExtractShipsByFeatures(Array<Ship> ships, int totalFeatureWanted, 
                                                   float minWantedFeature, int shipCount, Func<Ship, int> shipFeatureCount)
        {
            var shipSet = new Array<Ship>();
            if (minWantedFeature < 1) return shipSet;

            for (int x = ships.Count - 1; x >= 0; x--)
            {
                if (totalFeatureWanted <= 0 && shipSet.Count >= shipCount)
                    break;
                Ship ship = ships[x];
                int countOfShipFeature = shipFeatureCount(ship);
                if (countOfShipFeature >= minWantedFeature)
                {
                    Ships.RemoveSwapLast(ship);
                    shipSet.Add(ship);
                    totalFeatureWanted -= countOfShipFeature;
                }
            }

            return shipSet;
        }

        /// <summary>
        /// Extracts a ship set with wanted characteristics. 
        /// </summary>
        /// <param name="minStrength">Combat strength of fleet ships</param>
        /// <param name="bombingSecs">Time fleet should be able to bomb</param>
        /// <param name="wantedTroopStrength">Troop strength to invade with</param>
        /// <param name="planetTroops">Troops still on planets</param>
        /// /// <param name="minimumFleetSize">Attempt to get this many fleets</param>
        /// <returns></returns>
        public Array<Ship> ExtractShipSet(float minStrength, int bombingSecs,
            int wantedTroopStrength, Array<Troop> planetTroops, int minimumFleetSize)
        {
            Array<Ship> ships = ExtractSetsOfCombatShips(minStrength, WantedFleetCompletePercentage
                , minimumFleetSize, out int fleetCount);
            if (ships.IsEmpty)
                return new Array<Ship>();

            LaunchTroopsAndAddToShipList(wantedTroopStrength, planetTroops);
            ships.AddRange(ExtractTroops(wantedTroopStrength));
            ships.AddRange(ExtractBombers(bombingSecs, fleetCount));

            CheckForShipErrors(ships);

            return ships;
        }

        static void CheckForShipErrors(Array<Ship> ships)
        {
            if (Debugger.IsAttached)
            {
                foreach (var ship in ships)
                {
                    if (ship.fleet != null)
                        throw new Exception("Fleet should be null here.");

                    int n = ships.CountRef(ship);
                    //if (n > 1) 
                    //    throw new Exception($"Fleet ships contain duplicates({n}): {ship}.");
                }
            }
        }

        private void LaunchTroopsAndAddToShipList(int wantedTroopStrength, Array<Troop> planetTroops)
        {
            foreach (Troop troop in planetTroops.Filter(t => t.HostPlanet != null &&
                                                             !t.HostPlanet.RecentCombat))
            {
                if (InvasionTroopStrength > wantedTroopStrength)
                    break;

                if (troop.Loyalty == null || !troop.CanMove)
                    continue;
                Ship launched = troop.Launch();
                if (launched == null)
                {
                    Log.Warning($"CreateFleet: Troop launched from planet became null");
                    continue;
                }

                AddShip(launched);
            }
        }
    }
}