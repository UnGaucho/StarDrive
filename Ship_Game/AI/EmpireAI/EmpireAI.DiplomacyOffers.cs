using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        public void AcceptOffer(Offer theirOffer, Offer ourOffer, Empire us, Empire them)
        {
            if (theirOffer.PeaceTreaty)
            {
                Relationship relation          = OwnerEmpire.GetRelations(them);
                relation.AtWar                 = false;
                relation.PreparingForWar       = false;
                relation.ActiveWar.EndStarDate = Empire.Universe.StarDate;
                relation.WarHistory.Add(relation.ActiveWar);
                DTrait ourPersonality = OwnerEmpire.data.DiplomaticPersonality;
                if (ourPersonality != null)
                {
                    relation.Posture = Posture.Neutral;
                    if (relation.Anger_FromShipsInOurBorders > ourPersonality.Territorialism / 3f)
                        relation.Anger_FromShipsInOurBorders = ourPersonality.Territorialism / 3f;
                    if (relation.Anger_TerritorialConflict > ourPersonality.Territorialism / 3f)
                        relation.Anger_TerritorialConflict = ourPersonality.Territorialism / 3f;
                }
                relation.Anger_MilitaryConflict = 0f;
                relation.WarnedAboutShips = false;
                relation.WarnedAboutColonizing = false;
                relation.HaveRejectedDemandTech = false;
                relation.HaveRejected_OpenBorders = false;
                relation.HaveRejected_TRADE = false;
                relation.HasDefenseFleet = false;
                if (relation.DefenseFleet != -1)
                    OwnerEmpire.GetFleetsDict()[relation.DefenseFleet].FleetTask.EndTask();

                RemoveMilitaryTasksTargeting(them);

                relation.ActiveWar = null;
                Relationship relationThem = them.GetRelations(OwnerEmpire);
                relationThem.AtWar = false;
                relationThem.PreparingForWar = false;
                relationThem.ActiveWar.EndStarDate = Empire.Universe.StarDate;
                relationThem.WarHistory.Add(relationThem.ActiveWar);
                relationThem.Posture = Posture.Neutral;
                if (EmpireManager.Player != them)
                {
                    if (relationThem.Anger_FromShipsInOurBorders >
                        them.data.DiplomaticPersonality.Territorialism / 3f)
                    {
                        relationThem.Anger_FromShipsInOurBorders =
                            them.data.DiplomaticPersonality.Territorialism / 3f;
                    }
                    if (relationThem.Anger_TerritorialConflict >
                        them.data.DiplomaticPersonality.Territorialism / 3f)
                    {
                        relationThem.Anger_TerritorialConflict =
                            them.data.DiplomaticPersonality.Territorialism / 3f;
                    }
                    relationThem.Anger_MilitaryConflict = 0f;
                    relationThem.WarnedAboutShips = false;
                    relationThem.WarnedAboutColonizing = false;
                    relationThem.HaveRejectedDemandTech = false;
                    relationThem.HaveRejected_OpenBorders = false;
                    relationThem.HaveRejected_TRADE = false;
                    if (relationThem.DefenseFleet != -1)
                    {
                        them.GetFleetsDict()[relationThem.DefenseFleet].FleetTask.EndTask();
                    }

                    them.GetEmpireAI().RemoveMilitaryTasksTargeting(OwnerEmpire);
                }
                relationThem.ActiveWar = null;
                if (them == Empire.Universe.PlayerEmpire || OwnerEmpire == Empire.Universe.PlayerEmpire)
                {
                    Empire.Universe.NotificationManager.AddPeaceTreatyEnteredNotification(OwnerEmpire, them);
                }
                else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known &&
                         Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Known)
                {
                    Empire.Universe.NotificationManager.AddPeaceTreatyEnteredNotification(OwnerEmpire, them);
                }
            }
            if (theirOffer.NAPact)
            {
                us.GetRelations(them).Treaty_NAPact = true;
                TrustEntry te = new TrustEntry();
                if (Empire.Universe.PlayerEmpire != us)
                {
                    string name = us.data.DiplomaticPersonality.Name;
                    string str = name;

                    if (name != null) // Todo: This screams Enum!
                    {
                        switch (str)
                        {
                            case "Pacifist":
                            case "Cunning":    te.TrustCost = 0f;  break;
                            case "Xenophobic": te.TrustCost = 15f; break;
                            case "Aggressive": te.TrustCost = 35f; break;
                            case "Honorable":  te.TrustCost = 5f;  break;
                            case "Ruthless":   te.TrustCost = 50f; break;
                        }
                    }
                }
                te.Type = TrustEntryType.Treaty;
                us.GetRelations(them).TrustEntries.Add(te);
            }
            if (ourOffer.NAPact)
            {
                them.GetRelations(us).Treaty_NAPact = true;
                if (Empire.Universe.PlayerEmpire != them)
                {
                    TrustEntry te = new TrustEntry();
                    string name1 = them.data.DiplomaticPersonality.Name;
                    string str1 = name1;
                    if (name1 != null)
                    {
                        switch (str1)
                        {
                            case "Pacifist":
                            case "Cunning":    te.TrustCost = 0f;  break;
                            case "Xenophobic": te.TrustCost = 15f; break;
                            case "Aggressive": te.TrustCost = 35f; break;
                            case "Honorable":  te.TrustCost = 5f;  break;
                            case "Ruthless":   te.TrustCost = 50f; break;
                        }
                    }

                    te.Type = TrustEntryType.Treaty;
                    them.GetRelations(us).TrustEntries.Add(te);
                }
            }
            if (theirOffer.TradeTreaty)
            {
                us.GetRelations(them).Treaty_Trade = true;
                us.GetRelations(them).Treaty_Trade_TurnsExisted = 0;
                TrustEntry te = new TrustEntry
                {
                    TrustCost = 0.1f,
                    Type = TrustEntryType.Treaty
                };
                us.GetRelations(them).TrustEntries.Add(te);
            }
            if (ourOffer.TradeTreaty)
            {
                them.GetRelations(us).Treaty_Trade = true;
                them.GetRelations(us).Treaty_Trade_TurnsExisted = 0;
                TrustEntry te = new TrustEntry
                {
                    TrustCost = 0.1f,
                    Type = TrustEntryType.Treaty
                };
                them.GetRelations(us).TrustEntries.Add(te);
            }
            if (theirOffer.OpenBorders)
            {
                us.GetRelations(them).Treaty_OpenBorders = true;
                TrustEntry te = new TrustEntry
                {
                    TrustCost = 5f,
                    Type = TrustEntryType.Treaty
                };
                us.GetRelations(them).TrustEntries.Add(te);
            }
            if (ourOffer.OpenBorders)
            {
                them.GetRelations(us).Treaty_OpenBorders = true;
                TrustEntry te = new TrustEntry
                {
                    TrustCost = 5f,
                    Type = TrustEntryType.Treaty
                };
                them.GetRelations(us).TrustEntries.Add(te);
            }
            foreach (string tech in ourOffer.TechnologiesOffered)
            {
                //Added by McShooterz:
                //Them.UnlockTech(tech);
                them.AcquireTech(tech, us, TechUnlockType.Diplomacy);
                if (Empire.Universe.PlayerEmpire == us)
                {
                    continue;
                }
                TrustEntry te = new TrustEntry
                {
                    TrustCost = ResourceManager.Tech(tech).DiplomaticValueTo(us),
                    TurnTimer = 40,
                    Type = TrustEntryType.Technology
                };
                us.GetRelations(them).TrustEntries.Add(te);
            }
            foreach (string tech in theirOffer.TechnologiesOffered)
            {
                //Added by McShooterz:
                us.AcquireTech(tech, them, TechUnlockType.Diplomacy);
                if (Empire.Universe.PlayerEmpire == them)
                {
                    continue;
                }
                TrustEntry te = new TrustEntry
                {
                    TrustCost = ResourceManager.Tech(tech).DiplomaticValueTo(them),
                    Type = TrustEntryType.Treaty
                };
                them.GetRelations(us).TrustEntries.Add(te);
            }
            foreach (string art in ourOffer.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[art];
                foreach (Artifact artifact in us.data.OwnedArtifacts)
                {
                    if (artifact.Name != art)
                    {
                        continue;
                    }
                    toGive = artifact;
                }
                us.RemoveArtifact(toGive);
                them.AddArtifact(toGive);
            }
            foreach (string art in theirOffer.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[art];
                foreach (Artifact artifact in them.data.OwnedArtifacts)
                {
                    if (artifact.Name != art)
                    {
                        continue;
                    }
                    toGive = artifact;
                }
                them.RemoveArtifact(toGive);
                us.AddArtifact(toGive);
            }
            foreach (string planetName in ourOffer.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> troopShips = new Array<Ship>();
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }

                    // remove our troops from this planet
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {

                        if (pgs.TroopsAreOnTile && pgs.LockOnOurTroop(us, out Troop troop))
                        {
                            troop.SetPlanet(p); // FB - this is for making sure there is a host planet for the troops? strange
                            troopShips.Add(troop.Launch(ignoreMovement: true));
                        }
                    }
                    toRemove.Add(p);
                    p.Owner = them;
                    them.AddPlanet(p);
                    if (them != EmpireManager.Player)
                    {
                        p.colonyType = them.AssessColonyNeeds(p);
                    }
                    p.ParentSystem.OwnerList.Clear();
                    foreach (Planet pl in p.ParentSystem.PlanetList)
                    {
                        if (pl.Owner == null || p.ParentSystem.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.ParentSystem.OwnerList.Add(pl.Owner);
                    }
                    var te = new TrustEntry
                    {
                        TrustCost = p.ColonyWorthTo(us),
                        TurnTimer = 40,
                        Type = TrustEntryType.Technology
                    };
                    us.GetRelations(them).TrustEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    us.RemovePlanet(p);
                }
                foreach (Ship ship in troopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            foreach (string planetName in theirOffer.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> troopShips = new Array<Ship>();
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    toRemove.Add(p);
                    p.Owner = us;
                    us.AddPlanet(p);
                    p.ParentSystem.OwnerList.Clear();
                    foreach (Planet pl in p.ParentSystem.PlanetList)
                    {
                        if (pl.Owner == null || p.ParentSystem.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.ParentSystem.OwnerList.Add(pl.Owner);
                    }

                    // remove troops which are not ours from the planet
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsAreOnTile && pgs.LockOnEnemyTroop(us, out Troop troop))
                        {
                            troop.SetPlanet(p); // FB - this is for making sure there is a host planet for the troops? strange
                            troopShips.Add(troop.Launch(ignoreMovement: true));
                        }
                    }
                    if (Empire.Universe.PlayerEmpire != them)
                    {
                        var te = new TrustEntry
                        {
                            TrustCost = p.ColonyWorthTo(us),
                            TurnTimer = 40,
                            Type = TrustEntryType.Technology
                        };
                        them.GetRelations(us).TrustEntries.Add(te);
                    }
                    if (us == EmpireManager.Player)
                    {
                        continue;
                    }
                    p.colonyType = us.AssessColonyNeeds(p);
                }
                foreach (Planet p in toRemove)
                {
                    them.RemovePlanet(p);
                }
                foreach (Ship ship in troopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
        }

        public void AcceptThreat(Offer theirOffer, Offer ourOffer, Empire us, Empire them)
        {
            if (theirOffer.PeaceTreaty)
            {
                OwnerEmpire.GetRelations(them).AtWar = false;
                OwnerEmpire.GetRelations(them).PreparingForWar = false;
                OwnerEmpire.GetRelations(them).ActiveWar.EndStarDate = Empire.Universe.StarDate;
                OwnerEmpire.GetRelations(them).WarHistory.Add(OwnerEmpire.GetRelations(them).ActiveWar);
                OwnerEmpire.GetRelations(them).Posture = Posture.Neutral;
                if (OwnerEmpire.GetRelations(them).Anger_FromShipsInOurBorders >
                    OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3f)
                {
                    OwnerEmpire.GetRelations(them).Anger_FromShipsInOurBorders =
                        OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3f;
                }
                if (OwnerEmpire.GetRelations(them).Anger_TerritorialConflict >
                    OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3f)
                {
                    OwnerEmpire.GetRelations(them).Anger_TerritorialConflict =
                        OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3f;
                }
                OwnerEmpire.GetRelations(them).Anger_MilitaryConflict = 0f;
                OwnerEmpire.GetRelations(them).WarnedAboutShips = false;
                OwnerEmpire.GetRelations(them).WarnedAboutColonizing = false;
                OwnerEmpire.GetRelations(them).HaveRejectedDemandTech = false;
                OwnerEmpire.GetRelations(them).HaveRejected_OpenBorders = false;
                OwnerEmpire.GetRelations(them).HaveRejected_TRADE = false;
                OwnerEmpire.GetRelations(them).HasDefenseFleet = false;
                if (OwnerEmpire.GetRelations(them).DefenseFleet != -1)
                {
                    OwnerEmpire.GetFleetsDict()[OwnerEmpire.GetRelations(them).DefenseFleet].FleetTask.EndTask();
                }

                RemoveMilitaryTasksTargeting(them);

                OwnerEmpire.GetRelations(them).ActiveWar = null;
                them.GetRelations(OwnerEmpire).AtWar = false;
                them.GetRelations(OwnerEmpire).PreparingForWar = false;
                them.GetRelations(OwnerEmpire).ActiveWar.EndStarDate = Empire.Universe.StarDate;
                them.GetRelations(OwnerEmpire).WarHistory.Add(them.GetRelations(OwnerEmpire).ActiveWar);
                them.GetRelations(OwnerEmpire).Posture = Posture.Neutral;
                if (EmpireManager.Player != them)
                {
                    if (them.GetRelations(OwnerEmpire).Anger_FromShipsInOurBorders >
                        them.data.DiplomaticPersonality.Territorialism / 3f)
                    {
                        them.GetRelations(OwnerEmpire).Anger_FromShipsInOurBorders =
                            them.data.DiplomaticPersonality.Territorialism / 3f;
                    }
                    if (them.GetRelations(OwnerEmpire).Anger_TerritorialConflict >
                        them.data.DiplomaticPersonality.Territorialism / 3f)
                    {
                        them.GetRelations(OwnerEmpire).Anger_TerritorialConflict =
                            them.data.DiplomaticPersonality.Territorialism / 3f;
                    }
                    them.GetRelations(OwnerEmpire).Anger_MilitaryConflict = 0f;
                    them.GetRelations(OwnerEmpire).WarnedAboutShips = false;
                    them.GetRelations(OwnerEmpire).WarnedAboutColonizing = false;
                    them.GetRelations(OwnerEmpire).HaveRejectedDemandTech = false;
                    them.GetRelations(OwnerEmpire).HaveRejected_OpenBorders = false;
                    them.GetRelations(OwnerEmpire).HaveRejected_TRADE = false;
                    if (them.GetRelations(OwnerEmpire).DefenseFleet != -1)
                    {
                        them.GetFleetsDict()[them.GetRelations(OwnerEmpire).DefenseFleet].FleetTask.EndTask();
                    }
                    them.GetEmpireAI().RemoveMilitaryTasksTargeting(OwnerEmpire);
                }
                them.GetRelations(OwnerEmpire).ActiveWar = null;
            }
            if (theirOffer.NAPact)
            {
                us.GetRelations(them).Treaty_NAPact = true;
                FearEntry te = new FearEntry();
                if (Empire.Universe.PlayerEmpire != us)
                {
                    string name = us.data.DiplomaticPersonality.Name;
                    string str = name;
                    if (name != null)
                    {
                        switch (str)
                        {
                            case "Pacifist":
                            case "Cunning":    te.FearCost = 0f;  break;
                            case "Xenophobic": te.FearCost = 15f; break;
                            case "Aggressive": te.FearCost = 35f; break;
                            case "Honorable":  te.FearCost = 5f;  break;
                            case "Ruthless":   te.FearCost = 50f; break;
                        }
                    }
                }
                us.GetRelations(them).FearEntries.Add(te);
            }
            if (ourOffer.NAPact)
            {
                them.GetRelations(us).Treaty_NAPact = true;
                if (Empire.Universe.PlayerEmpire != them)
                {
                    FearEntry te = new FearEntry();
                    string name1 = them.data.DiplomaticPersonality.Name;
                    string str1 = name1;
                    if (name1 != null)
                    {
                        switch (str1)
                        {
                            case "Pacifist":
                            case "Cunning":    te.FearCost = 0f;  break;
                            case "Xenophobic": te.FearCost = 15f; break;
                            case "Aggressive": te.FearCost = 35f; break;
                            case "Honorable":  te.FearCost = 5f;  break;
                            case "Ruthless":   te.FearCost = 50f; break;
                        }
                    }
                    them.GetRelations(us).FearEntries.Add(te);
                }
            }
            if (theirOffer.TradeTreaty)
            {
                us.GetRelations(them).Treaty_Trade = true;
                us.GetRelations(them).Treaty_Trade_TurnsExisted = 0;
                FearEntry te = new FearEntry
                {
                    FearCost = 5f
                };
                us.GetRelations(them).FearEntries.Add(te);
            }
            if (ourOffer.TradeTreaty)
            {
                them.GetRelations(us).Treaty_Trade = true;
                them.GetRelations(us).Treaty_Trade_TurnsExisted = 0;
                FearEntry te = new FearEntry
                {
                    FearCost = 0.1f
                };
                them.GetRelations(us).FearEntries.Add(te);
            }
            if (theirOffer.OpenBorders)
            {
                us.GetRelations(them).Treaty_OpenBorders = true;
                FearEntry te = new FearEntry
                {
                    FearCost = 5f
                };
                us.GetRelations(them).FearEntries.Add(te);
            }
            if (ourOffer.OpenBorders)
            {
                them.GetRelations(us).Treaty_OpenBorders = true;
                FearEntry te = new FearEntry
                {
                    FearCost = 5f
                };
                them.GetRelations(us).FearEntries.Add(te);
            }
            foreach (string tech in ourOffer.TechnologiesOffered)
            {
                them.UnlockTech(tech, TechUnlockType.Diplomacy, us);
                if (Empire.Universe.PlayerEmpire == us)
                {
                    continue;
                }
                FearEntry te = new FearEntry
                {
                    FearCost = ResourceManager.Tech(tech).DiplomaticValueTo(us),
                    TurnTimer = 40
                };
                us.GetRelations(them).FearEntries.Add(te);
            }
            foreach (string tech in theirOffer.TechnologiesOffered)
            {
                us.UnlockTech(tech, TechUnlockType.Diplomacy,them);
                if (Empire.Universe.PlayerEmpire == them)
                {
                    continue;
                }
                FearEntry te = new FearEntry
                {
                    FearCost = ResourceManager.Tech(tech).DiplomaticValueTo(them)
                };
                them.GetRelations(us).FearEntries.Add(te);
            }
            foreach (string art in ourOffer.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[art];
                foreach (Artifact artifact in us.data.OwnedArtifacts)
                {
                    if (artifact.Name != art)
                    {
                        continue;
                    }
                    toGive = artifact;
                }
                us.RemoveArtifact(toGive);
                them.AddArtifact(toGive);
            }
            foreach (string art in theirOffer.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[art];
                foreach (Artifact artifact in them.data.OwnedArtifacts)
                {
                    if (artifact.Name != art)
                    {
                        continue;
                    }
                    toGive = artifact;
                }
                them.RemoveArtifact(toGive);
                us.AddArtifact(toGive);
            }
            foreach (string planetName in ourOffer.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> troopShips = new Array<Ship>();
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }

                    // remove our troops from the planet
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsAreOnTile && pgs.LockOnOurTroop(us, out Troop troop))
                        {
                            troop.SetPlanet(p); // FB - this is for making sure there is a host planet for the troops? strange
                            troopShips.Add(troop.Launch(ignoreMovement: true));
                        }
                    }
                    toRemove.Add(p);
                    p.Owner = them;
                    them.AddPlanet(p);
                    p.ParentSystem.OwnerList.Clear();
                    foreach (Planet pl in p.ParentSystem.PlanetList)
                    {
                        if (pl.Owner == null || p.ParentSystem.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.ParentSystem.OwnerList.Add(pl.Owner);
                    }
                    var te = new FearEntry
                    {
                        FearCost = p.ColonyWorthTo(us),
                        TurnTimer = 40
                    };
                    us.GetRelations(them).FearEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    us.RemovePlanet(p);
                }
                foreach (Ship ship in troopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            foreach (string planetName in theirOffer.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> troopShips = new Array<Ship>();
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    toRemove.Add(p);
                    p.Owner = us;
                    us.AddPlanet(p);
                    p.ParentSystem.OwnerList.Clear();
                    foreach (Planet pl in p.ParentSystem.PlanetList)
                    {
                        if (pl.Owner == null || p.ParentSystem.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.ParentSystem.OwnerList.Add(pl.Owner);
                    }

                    // remove troops which are not ours from the planet
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsAreOnTile && pgs.LockOnEnemyTroop(us, out Troop troop))
                        {
                            troop.SetPlanet(p); // FB - this is for making sure there is a host planet for the troops? strange
                            troopShips.Add(troop.Launch(ignoreMovement: true));
                        }
                    }
                    if (Empire.Universe.PlayerEmpire == them)
                    {
                        continue;
                    }
                    var te = new FearEntry
                    {
                        FearCost = p.ColonyWorthTo(them),
                        TurnTimer = 40
                    };
                    them.GetRelations(us).FearEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    them.RemovePlanet(p);
                }
                foreach (Ship ship in troopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            us.GetRelations(them).UpdateRelationship(us, them);
        }

        public string AnalyzeOffer(Offer theirOffer, Offer ourOffer, Empire them, Offer.Attitude attitude)
        {
            if (theirOffer.Alliance)
            {
                if (!theirOffer.IsBlank() || !ourOffer.IsBlank())
                {
                    return "OFFER_ALLIANCE_TOO_COMPLICATED";
                }
                if (OwnerEmpire.GetRelations(them).Trust < 90f || OwnerEmpire.GetRelations(them).TotalAnger >= 20f ||
                    OwnerEmpire.GetRelations(them).TurnsKnown <= 100)
                {
                    return "AI_ALLIANCE_REJECT";
                }
                SetAlliance(true, them);
                return "AI_ALLIANCE_ACCEPT";
            }
            if (theirOffer.PeaceTreaty)
            {
                PeaceAnswer answer = AnalyzePeaceOffer(theirOffer, ourOffer, them, attitude);
                if (!answer.Peace)
                {
                    return answer.Answer;
                }
                AcceptOffer(theirOffer, ourOffer, OwnerEmpire, them);
                OwnerEmpire.GetRelations(them).Treaty_Peace = true;
                OwnerEmpire.GetRelations(them).PeaceTurnsRemaining = 100;
                them.GetRelations(OwnerEmpire).Treaty_Peace = true;
                them.GetRelations(OwnerEmpire).PeaceTurnsRemaining = 100;
                return answer.Answer;
            }
            Empire us = OwnerEmpire;
            float totalTrustRequiredFromUs = 0f;
            DTrait dt = us.data.DiplomaticPersonality;
            if (ourOffer.TradeTreaty)
            {
                totalTrustRequiredFromUs += dt.Trade;
            }
            if (ourOffer.OpenBorders)
            {
                totalTrustRequiredFromUs += (dt.NAPact + 7.5f);
            }
            if (ourOffer.NAPact)
            {
                totalTrustRequiredFromUs += dt.NAPact;
                int numWars = 0;
                foreach (KeyValuePair<Empire, Relationship> relationship in us.AllRelations)
                {
                    if (relationship.Key.isFaction || !relationship.Value.AtWar)
                    {
                        continue;
                    }
                    numWars++;
                }
                if (numWars > 0 && !us.GetRelations(them).AtWar)
                {
                    totalTrustRequiredFromUs -= dt.NAPact;
                }
                else if (us.GetRelations(them).Threat >= 20f)
                {
                    totalTrustRequiredFromUs -= dt.NAPact;
                }
            }
            foreach (string tech in ourOffer.TechnologiesOffered)
            {
                totalTrustRequiredFromUs += ResourceManager.Tech(tech).DiplomaticValueTo(us, 0.02f);
            }

            float valueToThem = 0f;
            float valueToUs   = 0f;

            if (ourOffer.OpenBorders)   valueToThem += 5f;
            if (theirOffer.OpenBorders) valueToUs   += 0.01f;
            if (ourOffer.NAPact)        valueToThem += 5f;
            if (theirOffer.NAPact)      valueToUs   += 5f;
            if (ourOffer.TradeTreaty)   valueToThem += 5f;
            if (theirOffer.TradeTreaty) valueToUs   += OwnerEmpire.EstimateNetIncomeAtTaxRate(0.5f) < 1 ? 20f : 5f;

            foreach (string tech in ourOffer.TechnologiesOffered)
            {
                valueToThem += ResourceManager.Tech(tech).DiplomaticValueTo(us, 0.02f);
            }

            valueToThem += ourOffer.ArtifactsOffered.Count() * 15f;
            valueToUs   += theirOffer.ArtifactsOffered.Count() * 15f;

            foreach (string tech in theirOffer.TechnologiesOffered)
            {
                valueToUs += ResourceManager.Tech(tech).DiplomaticValueTo(us, 0.02f);
            }
            if (us.GetPlanets().Count - ourOffer.ColoniesOffered.Count + theirOffer.ColoniesOffered.Count < 1)
            {
                us.GetRelations(them).DamageRelationship(us, them, "Insulted", 25f, null);
                return "OfferResponse_Reject_Insulting";
            }
            foreach (string planetName in ourOffer.ColoniesOffered)
            {
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                        continue;

                    float worth = p.ColonyWorthTo(us);
                    foreach (Building b in p.BuildingList)
                        if (b.IsCapital)
                            worth += 200f;
                    float multiplier = 1.25f * p.ParentSystem.PlanetList.Count(other => other.Owner == p.Owner);
                    worth *= multiplier;
                    valueToThem += worth;
                }
            }
            foreach (string planetName in theirOffer.ColoniesOffered)
            {
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    float worth = p.ColonyWorthTo(us);
                    int multiplier = 1 + p.ParentSystem.PlanetList.Count(other => other.Owner == p.Owner);
                    worth *= multiplier;
                    valueToUs += worth;
                }
            }
            valueToUs = valueToUs + them.data.Traits.DiplomacyMod * valueToUs;
            if (valueToThem.AlmostZero() && valueToUs > 0f)
            {
                us.GetRelations(them).ImproveRelations(valueToUs, valueToUs);
                AcceptOffer(theirOffer, ourOffer, us, them);
                return "OfferResponse_Accept_Gift";
            }
            valueToUs -= valueToUs * us.GetRelations(them).TotalAnger / 100f;
            float offerDifferential   = valueToUs / (valueToThem + 0.01f);
            OfferQuality offerQuality = ProcessQuality(valueToUs, valueToThem);
            switch (attitude)
            {
                case Offer.Attitude.Pleading:
                    if (totalTrustRequiredFromUs > us.GetRelations(them).Trust)
                    {
                        if (offerQuality != OfferQuality.Great)
                            return "OfferResponse_InsufficientTrust";

                        us.GetRelations(them).ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                        AcceptOffer(theirOffer, ourOffer, us, them);
                        return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }

                    switch (offerQuality)
                    {
                        case OfferQuality.Insulting:
                            us.GetRelations(them).DamageRelationship(us, them, "Insulted", valueToThem - valueToUs, null);
                            return "OfferResponse_Reject_Insulting";
                        case OfferQuality.Poor:
                            return "OfferResponse_Reject_PoorOffer_EnoughTrust";
                        case OfferQuality.Fair:
                            us.GetRelations(them).ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                            AcceptOffer(theirOffer, ourOffer, us, them);
                            return "OfferResponse_Accept_Fair_Pleading";
                        case OfferQuality.Good:
                            us.GetRelations(them).ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                            AcceptOffer(theirOffer, ourOffer, us, them);
                            return "OfferResponse_Accept_Good";
                        case OfferQuality.Great:
                            us.GetRelations(them).ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                            AcceptOffer(theirOffer, ourOffer, us, them);
                            return "OfferResponse_Accept_Great";
                    }

                    break;
                case Offer.Attitude.Respectful:
                    if (totalTrustRequiredFromUs + us.GetRelations(them).TrustUsed <= us.GetRelations(them).Trust)
                    {
                        switch (offerQuality)
                        {
                            case OfferQuality.Insulting:
                                us.GetRelations(them).DamageRelationship(us, them, "Insulted", valueToThem - valueToUs, null);
                                return "OfferResponse_Reject_Insulting";
                            case OfferQuality.Poor:
                                return "OfferResponse_Reject_PoorOffer_EnoughTrust";
                            case OfferQuality.Fair:
                                us.GetRelations(them).ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                                AcceptOffer(theirOffer, ourOffer, us, them);
                                return "OfferResponse_Accept_Fair";
                            case OfferQuality.Good:
                                us.GetRelations(them).ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                                AcceptOffer(theirOffer, ourOffer, us, them);
                                return "OfferResponse_Accept_Good";
                            case OfferQuality.Great:
                                us.GetRelations(them).ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                                AcceptOffer(theirOffer, ourOffer, us, them);
                                return "OfferResponse_Accept_Great";
                        }
                    }

                    switch (offerQuality)
                    {
                        case OfferQuality.Insulting:
                            us.GetRelations(them).DamageRelationship(us, them, "Insulted", valueToThem - valueToUs, null);
                            return "OfferResponse_Reject_Insulting";
                        case OfferQuality.Poor:
                            return "OfferResponse_Reject_PoorOffer_LowTrust";
                        case OfferQuality.Fair:
                        case OfferQuality.Good:
                            return "OfferResponse_InsufficientTrust";
                        case OfferQuality.Great:
                            us.GetRelations(them).ImproveRelations(valueToUs - valueToThem, valueToUs);
                            AcceptOffer(theirOffer, ourOffer, us, them);
                            return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }

                    break;
                case Offer.Attitude.Threaten:
                    if (dt.Name == "Ruthless")
                        return "OfferResponse_InsufficientFear";

                    us.GetRelations(them).DamageRelationship(us, them, "Insulted", valueToThem - valueToUs, null);

                    if (offerQuality == OfferQuality.Great)
                    {
                        AcceptThreat(theirOffer, ourOffer, us, them);
                        return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }

                    // Lower quality because of threatening attitude
                    offerQuality = offerDifferential < 0.95f ? OfferQuality.Poor : OfferQuality.Fair;

                    if (us.GetRelations(them).Threat <= valueToThem || us.GetRelations(them).FearUsed + valueToThem >=
                        us.GetRelations(them).Threat)
                    {
                        return "OfferResponse_InsufficientFear";
                    }

                    switch (offerQuality)
                    {
                        case OfferQuality.Poor:
                            AcceptThreat(theirOffer, ourOffer, us, them);
                            return "OfferResponse_Accept_Bad_Threatening";
                        case OfferQuality.Fair:
                            AcceptThreat(theirOffer, ourOffer, us, them);
                            return "OfferResponse_Accept_Fair_Threatening";
                    }

                    break;
            }

            return "";
        }

        public PeaceAnswer AnalyzePeaceOffer(Offer theirOffer, Offer ourOffer, Empire them, Offer.Attitude attitude)
        {
            WarState state;
            Empire us = OwnerEmpire;
            DTrait personality = us.data.DiplomaticPersonality;

            float valueToUs   = theirOffer.ArtifactsOffered.Count * 15f;
            float valueToThem = ourOffer.ArtifactsOffered.Count * 15f;
            foreach (string tech in ourOffer.TechnologiesOffered)
                valueToThem += ResourceManager.Tech(tech).DiplomaticValueTo(us);
            foreach (string tech in theirOffer.TechnologiesOffered)
                valueToUs += ResourceManager.Tech(tech).DiplomaticValueTo(us);

            foreach (string planetName in ourOffer.ColoniesOffered)
            {
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name == planetName)
                        valueToThem += p.ColonyWorthTo(us);
                }
            }
            Array<Planet> planetsToUs = new Array<Planet>();
            foreach (string planetName in theirOffer.ColoniesOffered)
            {
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                        continue;
                    planetsToUs.Add(p);
                    float worth = p.ColonyWorthTo(us);
                    foreach (Building b in p.BuildingList)
                        if (b.IsCapital)
                            worth += 100000f; // basically, don't let AI give away their capital too easily
                    valueToUs += worth;
                }
            }

            if (personality.Name.NotEmpty())
            {
                WarType warType = us.GetRelations(them).ActiveWar.WarType;
                WarState warState = WarState.NotApplicable;
                switch (warType)
                {
                    case WarType.BorderConflict: warState = us.GetRelations(them).ActiveWar.GetBorderConflictState(planetsToUs); break;
                    case WarType.ImperialistWar: warState = us.GetRelations(them).ActiveWar.GetWarScoreState();                  break;
                    case WarType.DefensiveWar:   warState = us.GetRelations(them).ActiveWar.GetWarScoreState();                  break;
                }

                switch (personality.Name)
                {
                    case "Pacifist":
                    case "Honorable" when warType == WarType.DefensiveWar:
                        AddToValue(warState, 10, 5, 5, 10, ref valueToUs, ref valueToThem); break;
                    case "Honorable":
                        AddToValue(warState, 15, 8, 8, 15, ref valueToUs, ref valueToThem); break;
                    case "Xenophobic" when warType == WarType.DefensiveWar:
                        AddToValue(warState, 10, 5, 5, 10, ref valueToUs, ref valueToThem); break;
                    case "Xenophobic":
                        AddToValue(warState, 15, 8, 8, 15, ref valueToUs, ref valueToThem); break;
                    case "Aggressive":
                        AddToValue(warState, 10, 5, 75, 200, ref valueToUs, ref valueToThem); break;
                    case "Ruthless":
                        AddToValue(warState, 5, 1, 120, 300, ref valueToUs, ref valueToThem); break;
                    case "Cunning":
                        AddToValue(warState, 10, 5, 5, 10, ref valueToUs, ref valueToThem); break;
                }
            }

            valueToUs += valueToUs * them.data.Traits.DiplomacyMod; // TODO FB - need to be smarter here
            OfferQuality offerQuality = ProcessQuality(valueToUs, valueToThem);
            PeaceAnswer response      = ProcessPeace("REJECT_OFFER_PEACE_POOROFFER"); // Default response is reject
            switch (us.GetRelations(them).ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    state = us.GetRelations(them).ActiveWar.GetBorderConflictState(planetsToUs);
                    switch (state)
                    {
                        case WarState.EvenlyMatched:
                        case WarState.WinningSlightly:
                        case WarState.LosingSlightly:
                            switch (offerQuality)
                            {
                                case OfferQuality.Fair when us.GetRelations(them).ActiveWar.StartingNumContestedSystems > 0:
                                case OfferQuality.Good when us.GetRelations(them).ActiveWar.StartingNumContestedSystems > 0:
                                    response = ProcessPeace("REJECT_OFFER_PEACE_UNWILLING_BC");
                                    break;
                                case OfferQuality.Fair:
                                case OfferQuality.Good:
                                case OfferQuality.Great:
                                    response = ProcessPeace("ACCEPT_OFFER_PEACE", true);
                                    break;
                            }

                            break;
                        case WarState.Dominating when offerQuality >= OfferQuality.Good:
                                response = ProcessPeace("ACCEPT_OFFER_PEACE", true);
                            break;
                        case WarState.ColdWar when offerQuality < OfferQuality.Great:
                            response = ProcessPeace("REJECT_OFFER_PEACE_UNWILLING_BC");
                            break;
                        case WarState.ColdWar: // Great offer for Cold war
                            response = ProcessPeace("ACCEPT_PEACE_COLDWAR", true);
                            break;
                        case WarState.LosingBadly: response = ProcessLosingBadly(offerQuality); 
                            break;
                    }

                    break;
                case WarType.DefensiveWar:
                case WarType.ImperialistWar:
                    state = us.GetRelations(them).ActiveWar.GetWarScoreState();
                    switch (state)
                    {
                        case WarState.EvenlyMatched:
                        case WarState.LosingSlightly:
                        case WarState.WinningSlightly:
                            if (offerQuality >= OfferQuality.Fair)
                                response = ProcessPeace("ACCEPT_OFFER_PEACE", true);

                            break;
                        case WarState.Dominating when offerQuality >= OfferQuality.Good:
                            response = ProcessPeace("ACCEPT_OFFER_PEACE", true);
                            break;
                        case WarState.ColdWar: response = ProcessColdWar(offerQuality); 
                            break;
                        case WarState.LosingBadly: response = ProcessLosingBadly(offerQuality); 
                            break;
                    }

                    break;
            }

            return response; // Genocidal , Skirmish and NotApplicable are refused by default
        }

        PeaceAnswer ProcessColdWar(OfferQuality offerQuality)
        {
            string personality = OwnerEmpire.data.DiplomaticPersonality.Name;

            if (personality.NotEmpty() && personality == "Pacifist" && offerQuality >= OfferQuality.Fair)
                return ProcessPeace("ACCEPT_OFFER_PEACE", true);

            if (offerQuality == OfferQuality.Great)
                return ProcessPeace("ACCEPT_PEACE_COLDWAR", true);
            return ProcessPeace("REJECT_PEACE_RUTHLESS");
        }

        PeaceAnswer ProcessLosingBadly(OfferQuality offerQuality)
        {
            switch (offerQuality)
            {
                case OfferQuality.Fair:
                case OfferQuality.Good:
                case OfferQuality.Great: return ProcessPeace("ACCEPT_OFFER_PEACE", true);
                case OfferQuality.Poor:  return ProcessPeace("ACCEPT_OFFER_PEACE_RELUCTANT", true);
                default:                 return ProcessPeace("REJECT_OFFER_PEACE_POOROFFER"); // Insulting
            }
        }

        PeaceAnswer ProcessPeace(string answer, bool isPeace = false)
        {
            PeaceAnswer response = new PeaceAnswer
            {
                Peace  = isPeace,
                Answer = answer
            };

            return response;
        }

        public struct PeaceAnswer
        {
            public string Answer;
            public bool Peace;
        }

        public void SetAlliance(bool ally, Empire them)
        {
            if (ally)
            {
                OwnerEmpire.GetRelations(them).Treaty_Alliance = true;
                OwnerEmpire.GetRelations(them).Treaty_OpenBorders = true;
                them.GetRelations(OwnerEmpire).Treaty_Alliance = true;
                them.GetRelations(OwnerEmpire).Treaty_OpenBorders = true;
                return;
            }
            OwnerEmpire.GetRelations(them).Treaty_Alliance = false;
            OwnerEmpire.GetRelations(them).Treaty_OpenBorders = false;
            them.GetRelations(OwnerEmpire).Treaty_Alliance = false;
            them.GetRelations(OwnerEmpire).Treaty_OpenBorders = false;
        }

        public void SetAlliance(bool ally)
        {
            if (ally)
            {
                OwnerEmpire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_Alliance = true;
                OwnerEmpire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_OpenBorders = true;
                Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Treaty_Alliance = true;
                Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Treaty_OpenBorders = true;
                return;
            }
            OwnerEmpire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_Alliance = false;
            OwnerEmpire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_OpenBorders = false;
            Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Treaty_Alliance = false;
            Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Treaty_OpenBorders = false;
        }

        OfferQuality ProcessQuality(float valueToUs, float valueToThem)
        {
            float offerDifferential = valueToUs / valueToThem.LowerBound(0.0001f);

            if (offerDifferential.AlmostEqual(1) && valueToUs > 0)
                return OfferQuality.Fair;

            if (offerDifferential > 1.45f) return OfferQuality.Great;
            if (offerDifferential > 1.1f)  return OfferQuality.Good;
            if (offerDifferential > 0.9f)  return OfferQuality.Fair;
            if (offerDifferential > 0.65f) return OfferQuality.Poor;

            return OfferQuality.Insulting;
        }

        void AddToValue(WarState warState, float losingBadly, float losingSlightly, float winningSlightly, float dominating, 
            ref float valueToUs, ref float valueToThem)
        {
            switch (warState)
            {
                case WarState.LosingBadly:     valueToUs   += losingBadly;       break;
                case WarState.LosingSlightly:  valueToUs   += losingSlightly;    break;
                case WarState.WinningSlightly: valueToThem += winningSlightly;   break;
                case WarState.Dominating:      valueToThem += dominating;        break;
            }
        }

        enum OfferQuality
        {
            Insulting,
            Poor,
            Fair,
            Good,
            Great
        }
    }
}