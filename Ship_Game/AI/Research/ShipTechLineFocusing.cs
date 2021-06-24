using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.AI.Research
{
    public class ShipTechLineFocusing
    {
        readonly Empire OwnerEmpire;
        public Ship BestCombatShip { get; private set; }

        void DebugLog(string text) => Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);

        public ShipTechLineFocusing (Empire empire)
        {
            OwnerEmpire = empire;
        }

        public Array<TechEntry> LineFocusShipTechs(string modifier, Array<TechEntry> availableTechs, string scriptedOrRandom)
        {
            if (BestCombatShip != null)
            {
                if (OwnerEmpire.ShipsWeCanBuild.Contains(BestCombatShip.Name)
                    || OwnerEmpire.structuresWeCanBuild.Contains(BestCombatShip.Name)
                    || BestCombatShip.shipData.IsShipyard)
                    BestCombatShip = null;
                else
                if (!BestCombatShip.shipData.TechsNeeded.Except(OwnerEmpire.ShipTechs).Any())
                    BestCombatShip = null;
            }
            HashSet<string> shipFilteredTechs = FindBestShip(modifier, availableTechs, scriptedOrRandom);

            //now that we have a target ship to build filter out all the current techs that are not needed to build it.

            availableTechs = ConvertStringToTech(shipFilteredTechs);
            return availableTechs;
        }

        private static bool IsRoleValid(ShipData.RoleName role)
        {
            switch (role)
            {
                case ShipData.RoleName.disabled:
                case ShipData.RoleName.supply:
                case ShipData.RoleName.troop:
                case ShipData.RoleName.prototype:
                case ShipData.RoleName.construction: return false;
                case ShipData.RoleName.freighter:
                case ShipData.RoleName.colony:
                case ShipData.RoleName.ssp:
                case ShipData.RoleName.platform:
                case ShipData.RoleName.station:
                case ShipData.RoleName.troopShip:
                case ShipData.RoleName.support:
                case ShipData.RoleName.bomber:
                case ShipData.RoleName.carrier:
                case ShipData.RoleName.fighter:
                case ShipData.RoleName.scout:
                case ShipData.RoleName.gunboat:
                case ShipData.RoleName.drone:
                case ShipData.RoleName.corvette:
                case ShipData.RoleName.frigate:
                case ShipData.RoleName.destroyer:
                case ShipData.RoleName.cruiser:
                case ShipData.RoleName.battleship:
                case ShipData.RoleName.capital: break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return true;
        }

        private bool ShipHasUndiscoveredTech(Ship ship)
        {
            foreach (string techName in ship.shipData.TechsNeeded)
            {
                if (!OwnerEmpire.HasDiscovered(techName))
                    return true;
            }
            return false;
        }

        private bool ShipHasResearchableTech(Ship ship)
        {
            foreach (string techName in ship.shipData.TechsNeeded)
            {
                var tech = OwnerEmpire.GetTechEntry(techName);
                if (!tech.Unlocked && tech.IsOnlyShipTech() )
                    return true;
            }
            return false;
        }

        private Array<Ship> GetResearchableShips(Array<Ship> racialShips, HashSet<string> shipTechs)
        {
            racialShips = new Array<Ship>(racialShips.Filter(s => shipTechs.Intersect(s.shipData.TechsNeeded).Any()));
            var researchableShips = new Array<Ship>();
            foreach (Ship shortTermBest in racialShips)
            {
                // don't try to research ships we have all the tech for.
                if (!ShipHasResearchableTech(shortTermBest)) continue;
                // Don't build ships intended for carriers if there arent any carriers.
                if (!OwnerEmpire.canBuildCarriers && shortTermBest.shipData.CarrierShip)
                    continue;
                // filter out bad roles....
                if (!IsRoleValid(shortTermBest.shipData.HullRole)) continue;
                if (!IsRoleValid(shortTermBest.DesignRole)) continue;
                if (!IsRoleValid(shortTermBest.shipData.Role)) continue;
                if (!shortTermBest.shipData.UnLockable) continue;
                if (ShipHasUndiscoveredTech(shortTermBest)) continue;
                if (!shortTermBest.ShipGoodToBuild(OwnerEmpire, Status.Critical)) continue;

                researchableShips.Add(shortTermBest);
            }
            return researchableShips;
        }

        Array<Ship> FilterRacialShips()
        {
            var racialShips = new Array<Ship>();
            foreach (Ship shortTermBest in ResourceManager.GetShipTemplates())
            {
                // restrict to to ships available to this empire.
                string shipStyle = shortTermBest.shipData.ShipStyle ?? shortTermBest.shipData.BaseHull?.ShipStyle;
                if (shipStyle.IsEmpty())
                {
                    Log.Warning($"Ship {shortTermBest?.Name} Tech FilterRacialShip found a bad ship");
                    continue;
                }
                if (shortTermBest.shipData.ShipStyle == null)
                    continue;

                if (shortTermBest.shipData.IsShipyard)
                    continue;

                if (!OwnerEmpire.ShipStyleMatch(shipStyle))
                {
                    if (shipStyle != "Platforms" && shipStyle != "Misc")
                        continue;
                }

                if (shortTermBest.shipData.TechsNeeded.Count == 0)
                {
                    if (Empire.Universe.Debug)
                    {
                        Log.Info(OwnerEmpire.data.PortraitName + " : no techlist :" + shortTermBest.Name);
                    }
                    continue;
                }
                racialShips.Add(shortTermBest);
            }
            return racialShips;
        }

        SortedList<int, Array<Ship>> BucketShips(Array<Ship> ships, Func<Ship, int> bucketSort)
        {
            //SortRoles
            /*
             * take each ship and create buckets using the bucketSort ascending.
             */
            var roleSorter = new SortedList<int, Array<Ship>>();

            foreach (Ship ship in ships)
            {
                int key = bucketSort(ship);
                if (roleSorter.TryGetValue(key, out Array<Ship> test))
                    test.Add(ship);
                else
                {
                    test = new Array<Ship> { ship };
                    roleSorter.Add(key, test);
                }
            }
            return roleSorter;
        }

        int PickRandomKey(SortedList<int, Array<Ship>> sortedShips, float indexDivisor = 2)
        {
            //choose role
            /*
             * here set the default return to the first array in rolesorter.
             * then iterate through the keys with an ever increasing chance to choose a key.
             */
            int keyChosen = sortedShips.Keys.First();

            int x = (int)(sortedShips.Count / indexDivisor);

            foreach (var role in sortedShips)
            {
                float chance = (float)x++ / sortedShips.Count ;
                float rand = RandomMath.AvgRandomBetween(.001f, 1f, 2);

                if (rand > chance) continue;
                return role.Key;
            }
            return keyChosen;
        }

        bool GetLineFocusedShip(Array<Ship> researchableShips, HashSet<string> shipTechs, HashSet<string> nonShipTechs)
        {
            // Bucket ships by how many techs they have and we already have.
            SortedList<int, Array<Ship>> techCountSorter = TechCountSorter(researchableShips,shipTechs);

            if (techCountSorter.Count == 0)
                return false; // Bail if there aren't any ships to research
            //choose Ship
            var ships = techCountSorter[techCountSorter.Keys.First()];
            if (ships.Count <= 0)
                return false;

            float averageShipTechCost = float.MaxValue;

            foreach (string techName in nonShipTechs)
            {
                var tech = OwnerEmpire.GetTechEntry(techName);
                float techCost = tech.Tech.ActualCost;
                if (!tech.Unlocked && tech.Tech.RootNode == 0 && averageShipTechCost > techCost)
                    averageShipTechCost = techCost;
            }

            //averageShipTechCost /= nonShipTechs.Count;

            // find cheapest ship to research in current set of ships. 
            // adjust cost of some techs to make ships more or less wanted. 
            var pickedShip = ships.FindMin(s =>
            {
                float techScore = 0;
                foreach (string techName in s.shipData.TechsNeeded)
                {
                    var tech = OwnerEmpire.GetTechEntry(techName);
                    if (!tech.Unlocked && tech.Tech.RootNode == 0)
                    {
                        var cost = tech.Tech.ActualCost;

                        if (tech.IsTechnologyType(TechnologyType.Economic) && !s.isColonyShip) cost *= 2f;
                        else if (tech.IsTechnologyType(TechnologyType.ShipHull)) cost *= 1.5f;
                        if (tech.IsTechnologyType(TechnologyType.Colonization)) cost *= 0.5f;
                        if (tech.IsTechnologyType(TechnologyType.GroundCombat)) cost = 0;

                        techScore += cost;
                    }
                }
                if (s.IsPlatformOrStation)
                    techScore *= 1.5f;
                if (s.isColonyShip)
                    techScore *= 0.75f;
                if (s.DesignRole == ShipData.RoleName.freighter)
                    techScore *= 1.25f;
                switch(s.DesignRole)
                {
                    case ShipData.RoleName.platform:
                    case ShipData.RoleName.station: techScore *= 1.5f; break;
                    case ShipData.RoleName.colony: techScore *= 0.75f; break;
                    case ShipData.RoleName.freighter: techScore *= 1.25f; break;
                    case ShipData.RoleName.troopShip when !OwnerEmpire.canBuildTroopShips: techScore *= 0.75f; break;
                    case ShipData.RoleName.support when !OwnerEmpire.canBuildSupportShips: techScore *= 0.75f; break;
                    case ShipData.RoleName.bomber when !OwnerEmpire.canBuildBombers: techScore *= 0.75f; break;
                    case ShipData.RoleName.carrier when !OwnerEmpire.canBuildCarriers: techScore *= 0.75f; break;
                }
                float costRatio = techScore / averageShipTechCost;
                return techScore * costRatio;
            });

            BestCombatShip = pickedShip;
            return true;
        }

        Array<Ship> PickMinimumAmount(SortedList<int,Array<Ship>> list, int maxGroups = 3, int minAmount = 10)
        {
            Array<Ship> selected = new Array<Ship>();
            
            foreach (var group in list)
            {
                if (--maxGroups > 0 || selected.Count < minAmount)
                    selected.AddRange(group.Value);

            }
            return selected;
        }

        SortedList<int, Array<Ship>> RoleSorter(SortedList<int, Array<Ship>> hullSorter, int hullKey)
        {
            return BucketShips(hullSorter[hullKey], s =>
            {
                switch (s.DesignRole)
                {
                    case ShipData.RoleName.platform:
                    case ShipData.RoleName.station:
                    case ShipData.RoleName.scout:
                    case ShipData.RoleName.drone:
                    case ShipData.RoleName.fighter:
                    case ShipData.RoleName.freighter:  return 0;
                    case ShipData.RoleName.colony:
                    case ShipData.RoleName.supply:
                    case ShipData.RoleName.troop:
                    case ShipData.RoleName.troopShip:
                    case ShipData.RoleName.support:
                    case ShipData.RoleName.carrier:    return 2;
                    case ShipData.RoleName.gunboat:
                    case ShipData.RoleName.corvette:   return 5;
                    case ShipData.RoleName.bomber:
                    case ShipData.RoleName.frigate:
                    case ShipData.RoleName.destroyer:
                    case ShipData.RoleName.cruiser:    return 1;
                    case ShipData.RoleName.battleship: return 2;
                    case ShipData.RoleName.capital:    return 3;
                    default: return (int)s.DesignRole;
                }
            });
        }

        SortedList<int, Array<Ship>> HullSorter(SortedList<int, Array<Ship>> costSorter)
        {
            var shipsToSort = PickMinimumAmount(costSorter);

            return BucketShips(shipsToSort, hull =>
            {
                int countOfHullTechs = hull.shipData.BaseHull.TechsNeeded.Except(OwnerEmpire.ShipTechs).Count();

                return countOfHullTechs < 2 ? 0 : countOfHullTechs;
            });
        }

        SortedList<int, Array<Ship>> TechCostSorter(Array<Ship> shipsToSort)
        {

            return BucketShips(shipsToSort, shortTermBest =>
            {
                int costNormalizer = (int)(OwnerEmpire.Research.MaxResearchPotential / 10);
                int techCost = OwnerEmpire.TechCost(shortTermBest);
                techCost /=  costNormalizer.LowerBound(1); 
                return techCost;
            });
        }

        // todo: dont use a sorted list
        SortedList<int, Array<Ship>> TechCountSorter(Array<Ship> shipsToSort, HashSet<string> shipTechs)
        {
            // find max techs we have already researched for a design role. 
            // this is the line focusing check. by comparing to this number we can see how many techs 
            // a ship shares with other ships in that design role. 
            var deepestTechForRole = new Map<ShipData.RoleName, int>();
            foreach (var hull in shipsToSort)
            {
                if (deepestTechForRole.TryGetValue(hull.DesignRole, out int depth))
                {
                    int hullDepth = hull.shipData.TechsNeeded.Intersect(OwnerEmpire.ShipTechs).Count();
                    deepestTechForRole[hull.DesignRole] = hullDepth > depth ? hullDepth : depth;
                }
                else
                {
                    deepestTechForRole.Add(hull.DesignRole, hull.shipData.TechsNeeded.Intersect(OwnerEmpire.ShipTechs).Count());
                }
            }


            return BucketShips(shipsToSort, shortTermBest =>
            {
                bool priority           = false;
                bool includeHangarShips = false;
                bool excludeDrones      = false;
                
                // set some priorities
                switch (shortTermBest.DesignRole)
                {
                    case ShipData.RoleName.ssp:
                    case ShipData.RoleName.colony:                                         priority = true; break;
                    case ShipData.RoleName.troopShip when !OwnerEmpire.canBuildTroopShips: priority = true; break;
                    case ShipData.RoleName.bomber when !OwnerEmpire.canBuildBombers:       priority = true; break;
                    case ShipData.RoleName.carrier when !OwnerEmpire.canBuildCarriers:     priority = true; break;
                    case ShipData.RoleName.station:
                    case ShipData.RoleName.platform:                                       priority = true; break;
                }

                // if its a hangar only ship or a drone dont research this until we can use them. 
                if (!OwnerEmpire.canBuildCarriers && (shortTermBest.shipData.CarrierShip || shortTermBest.DesignRole == ShipData.RoleName.drone))
                    excludeDrones = true;

                // once we can build carriers bump the priority of small ships
                if (OwnerEmpire.canBuildCarriers)
                {
                    switch (shortTermBest.DesignRole)
                    {
                        case ShipData.RoleName.corvette when OwnerEmpire.CanBuildBattleships: includeHangarShips = true; break;
                        case ShipData.RoleName.fighter when OwnerEmpire.canBuildCruisers:     includeHangarShips = true; break;
                        case ShipData.RoleName.drone:                                         includeHangarShips = true; break;
                    }
                }

                int hasResearched         = shortTermBest.shipData.TechsNeeded.Intersect(OwnerEmpire.ShipTechs).Count();
                int unResearchable        = shortTermBest.shipData.TechsNeeded.Except(shipTechs).Count();
                int shipTechsResearchable = shortTermBest.shipData.TechsNeeded.Intersect(shipTechs).Count();
                int techsNotResearchable  = unResearchable - hasResearched
                ;
                int deepestTech = deepestTechForRole[shortTermBest.DesignRole];

                // The ship has no techs that can researched. 
                if (shipTechsResearchable == 0)
                    return 3;

                // include if the number of already researched tech is within range
                // *note that this is a self adjusting process.
                // All ships will be considered at first and then filtered out as the
                // number of ships with the same tech increases. 
                // however the process is using the current set of ships.
                // the line focusing will always have ships to match. 
                bool lineFocused = hasResearched >= deepestTech / 2;// * 3 / 4; 

                // threshold for the number of techs the ships needs that it cant currently research
                int limitForTechsNotInList = 6;
                limitForTechsNotInList += priority ? 5 : 0;
                limitForTechsNotInList += includeHangarShips ? 5 : 0;
                //&& techsNotResearchable <= limitForTechsNotInList
                // all good ships go in bucket 0 not wanted in bucket 1.
                // by always taking the first available bucket we are assured we will always have ships to research
                return !excludeDrones && lineFocused  ? 0 : 1;
            });
        }

        HashSet<string> FindBestShip(string modifier, Array<TechEntry> availableTechs, string command)
        {
            var shipTechs = new HashSet<string>();
            var nonShipTechs = new HashSet<string>();
            bool needShipTech = modifier.Contains("Ship");

            foreach (TechEntry techEntry in availableTechs)
            {
                if (techEntry.ContainsNonShipTechOrBonus())
                    nonShipTechs.Add(techEntry.UID);
                else
                    shipTechs.Add(techEntry.UID);
            }

            // If not researching ship techs then dont research any ship tech.
            if (!needShipTech)
                return nonShipTechs;

            // If we have a best ship already then use that and return.
            // But only if not using a script
            if (BestCombatShip != null && command == "RANDOM")
                return UseBestShipTechs(shipTechs, nonShipTechs);

            // Doesn't have a best ship so find one
            // Filter out ships we cant use
            Array<Ship> racialShips = FilterRacialShips();
            Array<Ship> researchableShips = GetResearchableShips(racialShips,shipTechs);

            if (researchableShips.Count <= 0) 
                return nonShipTechs;
            // If not using a script dont get a best ship.
            // Or if the modder decided they want to use short term researchable tech only
            if (command != "RANDOM"
                || (GlobalStats.HasMod && !GlobalStats.ActiveModInfo.EnableShipTechLineFocusing))
            {
                return UseResearchableShipTechs(researchableShips, shipTechs, nonShipTechs);
            }

            // Now find a new ship to research that uses most of the tech we already have.
            GetLineFocusedShip(researchableShips, shipTechs, nonShipTechs);
            return BestCombatShip != null ? UseBestShipTechs(shipTechs, nonShipTechs) : shipTechs;
        }

        private HashSet<string> UseBestShipTechs(HashSet<string> shipTechs, HashSet<string> nonShipTechs)
        {
            // Match researchable techs to techs ship needs.
            if (OwnerEmpire.ShipsWeCanBuild.Contains(BestCombatShip?.Name)) 
                BestCombatShip = null;

            if (BestCombatShip != null)
            {
                var bestShipTechs = shipTechs.Intersect(BestCombatShip.shipData.TechsNeeded);
                if (!bestShipTechs.Any())
                {
                    var bestNoneShipTechs = nonShipTechs.Intersect(BestCombatShip.shipData.TechsNeeded);
                    if (!bestNoneShipTechs.Any())
                        BestCombatShip = null;
                    else
                        Log.Warning($"ship tech classified as non ship tech {bestNoneShipTechs.First()} for {BestCombatShip}");
                }
                if (BestCombatShip != null)
                    return UseOnlyWantedShipTechs(bestShipTechs, nonShipTechs);
            }
            return UseOnlyWantedShipTechs(shipTechs, nonShipTechs);
        }

        private HashSet<string> UseResearchableShipTechs(Array<Ship> researchableShips, HashSet<string> shipTechs, HashSet<string> nonShipTechs)
        {
            //filter out all current shiptechs that arent in researchableShips.
            var sortedShips               = researchableShips.Sorted(ExtractTechCost);
            HashSet<string> goodShipTechs = new HashSet<string>();
            foreach (var ship in sortedShips)
            {
                if (TryExtractNeedTechs(ship, out HashSet<string> techs))
                {
                    var researchableTechs = shipTechs.Intersect(techs).ToArray();
                    if (researchableTechs.Length > 0)
                    {
                        foreach (var techName in researchableTechs)
                            goodShipTechs.Add(techName);

                        break;
                    }
                }
            }

            return UseOnlyWantedShipTechs(goodShipTechs, nonShipTechs);
        }

        private HashSet<string> UseOnlyWantedShipTechs(IEnumerable<string> shipTechs, HashSet<string> nonShipTechs)
        {
            //combine the wanted shiptechs with the nonshiptechs.
            var generalTech = new HashSet<string>();
            foreach (var bTech in shipTechs)
                generalTech.Add(bTech);
            foreach (var nonShip in nonShipTechs)
                generalTech.Add(nonShip);
            return generalTech;
        }

        private Array<TechEntry> ConvertStringToTech(HashSet<string> shipTechs)
        {
            var bestShipTechs = new Array<TechEntry>();

            foreach (string shipTech in shipTechs)
            {
                if (OwnerEmpire.TryGetTechEntry(shipTech, out TechEntry test))
                {
                    // repeater compensator. This needs some deeper logic.
                    // I current just say if you research one level.
                    // Dont research any more.
                    if (test.MaxLevel > 1 && test.Level > 1) continue;
                    bestShipTechs.Add(test);
                }
            }
            return bestShipTechs;
        }

        bool TryExtractNeedTechs(Ship ship, out HashSet<string> techsToAdd)
        {
            if (OwnerEmpire.IsHullUnlocked(ship.shipData.Hull))
            {
                techsToAdd = ship.shipData.TechsNeeded;
                return true;
            }

            string hullTech = "";
            techsToAdd = new HashSet<string>();
            var shipTechs      = ConvertStringToTech(ship.shipData.TechsNeeded);
            for (int i = 0; i < shipTechs.Count; i++)
            {
                TechEntry tech = shipTechs[i];
                if (!tech.Unlocked)
                {
                    if (tech.GetUnlockableHulls(OwnerEmpire).Count > 0)
                    {
                        if (hullTech.IsEmpty())
                            hullTech = tech.UID;
                        else  // we are looking for a ship which is only one hull away
                            return false;
                    }
                    else
                    {
                        techsToAdd.Add(tech.UID);
                    }
                }
            }


            // If there are no new  tech to reseach besides the hull, its time to research the hull
            if (techsToAdd.Count == 0 && hullTech.NotEmpty())
                techsToAdd.Add(hullTech);

            return techsToAdd.Count > 0;
        }

        float ExtractTechCost(Ship ship)
        {
            float totalCost = 0;
            var shipTechs = ConvertStringToTech(ship.shipData.TechsNeeded);
            for (int i = 0; i < shipTechs.Count; i++)
            {
                TechEntry tech = shipTechs[i];
                if (tech.Locked)
                    totalCost += tech.Tech.ActualCost;
            }

            return totalCost;
        }

        public bool BestShipNeedsHull(Array<TechEntry> availableTechs) => ShipHullTech(BestCombatShip, availableTechs) != null;

        public TechEntry BestShipsHull(Array<TechEntry> availableTechs) => ShipHullTech(BestCombatShip, availableTechs);

        public TechEntry ShipHullTech(Ship bestShip, Array<TechEntry> availableTechs)
        {
            if (bestShip == null) return null;

            var shipTechs = ConvertStringToTech(bestShip.shipData.TechsNeeded);
            foreach (TechEntry tech in shipTechs)
            {
                if (tech.GetUnlockableHulls(OwnerEmpire).Count > 0)
                    return tech;
            }
            return null;
        }

        public bool WasBestShipHullNotChosen(string topic, Array<TechEntry> availableTechs)
        {
            var hullTech = BestShipsHull(availableTechs);

            if (hullTech != null && hullTech.UID != topic)
            {
                BestCombatShip = null;
                return true;
            }
            return false;
        }
    }
}
