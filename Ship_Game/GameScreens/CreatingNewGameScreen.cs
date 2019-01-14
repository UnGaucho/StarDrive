// Type: Ship_Game.CreatingNewGameScreen
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace Ship_Game
{
    public sealed class CreatingNewGameScreen : GameScreen
    {
        private float Scale = 1f;
        private int NumSystems = 50;
        private AutoResetEvent WorkerBeginEvent = new AutoResetEvent(false);
        private ManualResetEvent WorkerCompletedEvent = new ManualResetEvent(true);
        private Array<Vector2> ClaimedSpots = new Array<Vector2>();
        private RaceDesignScreen.GameMode Mode;
        private Vector2 GalacticCenter;
        private UniverseData Data;
        private Empire Player;
        private UniverseData.GameDifficulty Difficulty;
        private int NumOpponents;
        private MainMenuScreen mmscreen;
        private Texture2D LoadingScreenTexture;
        private string AdviceText;
        private Ship playerShip;
        private TaskResult BackgroundTask;
        private UniverseScreen us;
        private float PercentLoaded;
        

        public CreatingNewGameScreen(Empire player, string universeSize, 
                float starNumModifier, int numOpponents, RaceDesignScreen.GameMode gamemode, 
                int gameScale, UniverseData.GameDifficulty difficulty, MainMenuScreen mmscreen) : base(mmscreen)
        {
            GlobalStats.RemnantArmageddon = false;
            GlobalStats.RemnantKills = 0;
            this.mmscreen = mmscreen;
            foreach (var art in ResourceManager.ArtifactsDict)
                art.Value.Discovered = false;

            RandomEventManager.ActiveEvent = null;
            Difficulty = difficulty;
            if      (gameScale == 5) Scale = 8;
            else if (gameScale == 6) Scale = 16;
            else                     Scale = gameScale;

            Mode = gamemode;
            NumOpponents = numOpponents;
            EmpireManager.Clear();

            ResourceManager.LoadEncounters();
            Player = player;
            player.Initialize();
            player.data.CurrentAutoScout     = player.data.ScoutShip;
            player.data.CurrentAutoColony    = player.data.ColonyShip;
            player.data.CurrentAutoFreighter = player.data.FreighterShip;
            player.data.CurrentConstructor   = player.data.ConstructorShip;
            Data = new UniverseData
            {
                FTLSpeedModifier      = GlobalStats.FTLInSystemModifier,
                EnemyFTLSpeedModifier = GlobalStats.EnemyFTLInSystemModifier,                    
                GravityWells          = GlobalStats.PlanetaryGravityWells,
                FTLinNeutralSystem    = GlobalStats.WarpInSystem,
                difficulty            = difficulty
            };

            bool corners = Mode == RaceDesignScreen.GameMode.Corners;
            int size;
            switch (universeSize)
            {
                default: /*"Tiny"*/ size = 16;                 Data.Size = new Vector2(1750000); break;
                case "Small":       size = corners ? 32 : 30;  Data.Size = new Vector2(3500000); break;
                case "Medium":      size = corners ? 48 : 45;  Data.Size = new Vector2(5500000); break;
                case "Large":       size = corners ? 64 : 70;  Data.Size = new Vector2(9000000); break;
                case "Huge":        size = corners ? 80 : 92;  Data.Size = new Vector2(13500000); break;
                case "Epic":        size = corners ? 112: 115; Data.Size = new Vector2(20000000); break;
                case "TrulyEpic":   size = corners ? 144: 160; Data.Size = new Vector2(33554423); break;
            }

            NumSystems = (int)(size * starNumModifier);           
            //Log.Info($"Empire.ProjectorRadius = {Empire.ProjectorRadius}");
            
            Data.Size *= Scale;
            Data.EmpireList.Add(player);
            EmpireManager.Add(player);
            GalacticCenter = new Vector2(0f, 0f);  // Gretman (for new negative Map dimensions)
            StatTracker.SnapshotsDict.Clear();

            CurrentGame.StartNew(Data);
        }

        public override void LoadContent()
        {
            ScreenManager.ClearScene();
            LoadingScreenTexture = ResourceManager.LoadRandomLoadingScreen(TransientContent);
            AdviceText = Fonts.Arial12Bold.ParseText(ResourceManager.LoadRandomAdvice(), 500f);

            BackgroundTask = Parallel.Run(GenerateSystems);
            base.LoadContent();
        }

        private void FinalizeEmpires()
        {
            for (int empireId = 0; empireId < Data.EmpireList.Count; empireId++)
            {
                float majorProgress = (empireId+1) / (float)Data.EmpireList.Count;

                Empire empire = Data.EmpireList[empireId];
                if (empire.isFaction)
                    continue;

                IReadOnlyList<Planet> planets = empire.GetPlanets();
                for (int planetId = 0; planetId < planets.Count; planetId++)
                {
                    Planet planet = planets[planetId];
                    planet.MineralRichness += GlobalStats.StartingPlanetRichness;
                    planet.ParentSystem.SetExploredBy(empire);
                    planet.SetExploredBy(empire);

                    foreach (Planet p in planet.ParentSystem.PlanetList)
                        p.SetExploredBy(empire);

                    if (planet.ParentSystem.OwnerList.Count == 0)
                    {
                        planet.ParentSystem.OwnerList.Add(empire);
                        foreach (Planet planet2 in planet.ParentSystem.PlanetList)
                            planet2.SetExploredBy(empire);
                    }

                    if (planet.HasShipyard)
                    {
                        SpaceStation spaceStation = new SpaceStation {planet = planet};
                        planet.Station = spaceStation;
                        spaceStation.ParentSystem = planet.ParentSystem;
                        spaceStation.LoadContent(ScreenManager);
                    }

                    string colonyShip = empire.data.DefaultColonyShip;
                    if (GlobalStats.HardcoreRuleset) colonyShip += " STL";

                    Ship ship1 = Ship.CreateShipAt(colonyShip, empire, planet, new Vector2(-2000, -2000), true);
                    Data.MasterShipList.Add(ship1);

                    string startingScout = empire.data.StartingScout;
                    if (GlobalStats.HardcoreRuleset) startingScout += " STL";

                    Ship ship2 = Ship.CreateShipAt(startingScout, empire, planet, new Vector2(-2500, -2000), true);
                    Data.MasterShipList.Add(ship2);

                    if (empire == Player)
                    {
                        string starterShip = empire.data.Traits.Prototype == 0
                            ? empire.data.StartingShip
                            : empire.data.PrototypeShip;

                        playerShip = Ship.CreateShipAt(starterShip, empire, planet, new Vector2(350f, 0.0f), true);
                        playerShip.SensorRange = 100000f; // @todo What is this range hack?

                        if (GlobalStats.ActiveModInfo == null || playerShip.VanityName == "")
                            playerShip.VanityName = "Perseverance";

                        Data.MasterShipList.Add(playerShip);

                        // Doctor: I think commenting this should completely stop all the recognition of the starter ship being the 'controlled' ship for the pie menu.
                        Data.playerShip = playerShip;

                        planet.colonyType = Planet.ColonyType.Colony;
                    }
                    else
                    {
                        string starterShip = empire.data.StartingShip;
                        if (GlobalStats.HardcoreRuleset) starterShip += " STL";
                        starterShip = empire.data.Traits.Prototype == 0 ? starterShip : empire.data.PrototypeShip;

                        Ship ship3 = Ship.CreateShipAt(starterShip, empire, planet, new Vector2(-2500, -2000), true);
                        Data.MasterShipList.Add(ship3);

                        //empire.AddShip(ship3);
                        //empire.GetForcePool().Add(ship3);
                    }

                    float minorProgress = (planetId+1) / (float)planets.Count;
                    UpdateCurrentStepProgress(majorProgress * minorProgress);
                }
            }

            foreach (Empire empire in Data.EmpireList)
            {
                if (empire.isFaction || empire.data.Traits.BonusExplored <= 0)
                    continue;

                Planet homeworld = empire.GetPlanets()[0];
                SolarSystem[] closestSystems = Data.SolarSystemsList.Sorted(system => homeworld.Center.Distance(system.Position));
                int numSystemsExplored = Data.SolarSystemsList.Count >= 20 ? empire.data.Traits.BonusExplored : Data.SolarSystemsList.Count;
                for (int i = 0; i < numSystemsExplored; ++i)
                {
                    SolarSystem system = closestSystems[i];
                    system.SetExploredBy(empire);
                    foreach (Planet planet in system.PlanetList)
                        planet.SetExploredBy(empire);
                }
            }
        }

        private void FinalizeSolarSystems()
        {
            foreach (SolarSystem solarSystem1 in Data.SolarSystemsList)
            {
                var list = new Array<SysDisPair>();
                foreach (SolarSystem solarSystem2 in Data.SolarSystemsList)
                {
                    if (solarSystem1 != solarSystem2)
                    {
                        float num1 = Vector2.Distance(solarSystem1.Position, solarSystem2.Position);
                        if (list.Count < 5)
                        {
                            list.Add(new SysDisPair
                            {
                                System = solarSystem2,
                                Distance = num1
                            });
                        }
                        else
                        {
                            int index1 = 0;
                            float num2 = 0.0f;
                            for (int index2 = 0; index2 < 5; ++index2)
                            {
                                if (list[index2].Distance > num2)
                                {
                                    index1 = index2;
                                    num2 = list[index2].Distance;
                                }
                            }
                            if (num1 < num2)
                                list[index1] = new SysDisPair
                                {
                                    System = solarSystem2,
                                    Distance = num1
                                };
                        }
                    }
                }
                foreach (SysDisPair sysDisPair in list)
                    solarSystem1.FiveClosestSystems.Add(sysDisPair.System);
            }

        }

        private void SubmitSceneObjectsForRendering(SolarSystem wipSystem)
        {
            foreach (Planet planet in wipSystem.PlanetList)
            {
                planet.ParentSystem = wipSystem;
                planet.Center += wipSystem.Position;
                planet.InitializePlanetMesh(this);
            }
            foreach (Asteroid asteroid in wipSystem.AsteroidsList)
            {
                asteroid.Position3D.X += wipSystem.Position.X;
                asteroid.Position3D.Y += wipSystem.Position.Y;
                asteroid.Initialize();
                AddObject(asteroid.So);
            }
            foreach (Moon moon in wipSystem.MoonList)
            {
                moon.Initialize();
                AddObject(moon.So);
            }
            foreach (Ship ship in wipSystem.ShipList)
            {
                ship.Position = ship.loyalty.GetPlanets()[0].Center + new Vector2(6000f, 2000f);
                ship.InitializeShip();
            }
        }

        void GenerateInitialSystemData()
        {
            Stopwatch s = Stopwatch.StartNew();
            CreateOpponents(); // 156ms
            UpdateCurrentStepProgress(0.15f);
            Log.Info(ConsoleColor.Blue, $"    ## CreateOpponents elapsed: {s.NextMillis()}ms");
            
            PopulateRelations(); // 1ms

            ResourceManager.MarkShipDesignsUnlockable(); // 240ms
            UpdateCurrentStepProgress(0.35f);
            Log.Info(ConsoleColor.Blue, $"    ## MarkShipDesignsUnlockable elapsed: {s.NextMillis()}ms");

            LoadEmpireStartingSystems(); // 420
            UpdateCurrentStepProgress(0.65f);
            Log.Info(ConsoleColor.Blue, $"    ## LoadEmpireStartingSystems elapsed: {s.NextMillis()}ms");

            GenerateRandomSystems(); // 425ms
            UpdateCurrentStepProgress(0.95f);
            Log.Info(ConsoleColor.Blue, $"    ## GenerateRandomSystems elapsed: {s.NextMillis()}ms");

            // This section added by Gretman
            if (Mode != RaceDesignScreen.GameMode.Corners)            
                SolarSystemSpacing(Data.SolarSystemsList); // 2ms    
            else
            {
                short whichCorner = StartingPositionCorners();

                foreach (SolarSystem system in Data.SolarSystemsList)
                {
                    //This will distribute all the rest of the planets evenly
                    if (system.isStartingSystem || system.DontStartNearPlayer)
                        continue;
                    system.Position = GenerateRandomCorners(whichCorner);
                    whichCorner += 1;   //Only change which corner if a system is actually created
                    if (whichCorner > 3) whichCorner = 0;
                }
            }
            UpdateCurrentStepProgress(1.00f);
        }

        void CreateOpponents()
        {
            EmpireData[] majorRaces = ResourceManager.MajorRaces.Filter(data => data != Player.data);

            // create a randomly shuffled list of opponents
            var opponents = new Array<EmpireData>(majorRaces);
            opponents.Shuffle();
            opponents.Resize(Math.Min(opponents.Count, NumOpponents)); // truncate

            foreach (EmpireData data in opponents)
            {
                Empire e = Data.CreateEmpire(data);
                RacialTrait t = e.data.Traits;
                switch (Difficulty)
                {
                    case UniverseData.GameDifficulty.Easy:
                        t.ProductionMod -= 0.25f;
                        t.ResearchMod -= 0.25f;
                        t.TaxMod -= 0.25f;
                        t.ModHpModifier -= 0.25f;
                        break;
                    case UniverseData.GameDifficulty.Hard:
                        e.data.FlatMoneyBonus += 10;
                        t.ProductionMod += 0.5f;
                        t.ResearchMod += 0.75f;
                        t.TaxMod += 0.5f;
                        t.ShipCostMod -= 0.2f;
                        break;
                    case UniverseData.GameDifficulty.Brutal:
                        e.data.FlatMoneyBonus += 20; // cheaty cheat
                        t.ProductionMod += 1.0f;
                        t.ResearchMod = 1.33f;
                        t.TaxMod += 1.0f;
                        t.ShipCostMod -= 0.5f;
                        break;
                }
            }
            
            foreach (EmpireData data in ResourceManager.MinorRaces)
                Data.CreateEmpire(data);
        }

        void PopulateRelations()
        {
            foreach (Empire empire in Data.EmpireList)
            {
                foreach (Empire e in Data.EmpireList)
                {
                    if (empire == e)
                        continue;

                    var r = new Relationship(e.data.Traits.Name);
                    empire.AddRelationships(e, r);
                    if (e == Player && Difficulty > UniverseData.GameDifficulty.Normal)
                    {
                        float angerMod = (int) Difficulty * (90 - empire.data.DiplomaticPersonality.Trustworthiness);
                        r.Anger_DiplomaticConflict = angerMod;
                        r.Anger_MilitaryConflict = 1;
                    }
                }
            }
        }

        void LoadEmpireStartingSystems()
        {
            foreach (Empire e in Data.EmpireList)
            {
                if (e.isFaction)
                    continue;
                SolarSystem sys;
                SolarSystemData systemData = ResourceManager.LoadSolarSystemData(e.data.Traits.HomeSystemName);
                if (systemData == null)
                {
                    sys = new SolarSystem();
                    sys.GenerateStartingSystem(e.data.Traits.HomeSystemName, Data, Scale, e);
                }
                else sys = SolarSystem.GenerateSystemFromData(systemData, e);

                sys.isStartingSystem = true;
                Data.SolarSystemsList.Add(sys);
            }
        }

        void GenerateRandomSystems()
        {
            int systemCount = 0;
            foreach (SolarSystemData systemData in ResourceManager.LoadRandomSolarSystems())
            {
                if (systemCount > NumSystems)
                    break;
                var solarSystem = SolarSystem.GenerateSystemFromData(systemData, null);
                solarSystem.DontStartNearPlayer = true; // Added by Gretman
                Data.SolarSystemsList.Add(solarSystem);
                systemCount++;
            }

            var markovNameGenerator = new MarkovNameGenerator(File.ReadAllText("Content/NameGenerators/names.txt"), 3, 5);
            var solarSystem1 = new SolarSystem();
            solarSystem1.GenerateCorsairSystem(markovNameGenerator.NextName);
            solarSystem1.DontStartNearPlayer = true;
            Data.SolarSystemsList.Add(solarSystem1);
            for (; systemCount < NumSystems; ++systemCount)
            {
                var solarSystem2 = new SolarSystem();
                solarSystem2.GenerateRandomSystem(markovNameGenerator.NextName, Data, Scale);
                Data.SolarSystemsList.Add(solarSystem2);
            }
        }

        private void SolarSystemSpacing(Array<SolarSystem> solarSystems)
        {
            foreach (SolarSystem solarSystem2 in solarSystems)
            {
                float spacing = 350000f;
                if (solarSystem2.isStartingSystem || solarSystem2.DontStartNearPlayer)
                    spacing = Data.Size.X / (2f - 1f / (Data.EmpireList.Count - 1));
                solarSystem2.Position = GenerateRandomSysPos(spacing);
            }
        }

        private short StartingPositionCorners()
        {
            short whichcorner = (short) RandomMath.RandomBetween(0, 4); //So the player doesnt always end up in the same corner;
            foreach (SolarSystem solarSystem2 in Data.SolarSystemsList)
            {
                if (solarSystem2.isStartingSystem || solarSystem2.DontStartNearPlayer)
                {
                    if (solarSystem2.isStartingSystem)
                    {
                        //Corner Values
                        //0 = Top Left
                        //1 = Top Right
                        //2 = Bottom Left
                        //3 = Bottom Right

                        //Put the 4 Home Planets into their corners, nessled nicely back a bit
                        float RandomoffsetX =
                            RandomMath.RandomBetween(0, 19) / 100; //Do want some variance in location, but still in the back
                        float RandomoffsetY = RandomMath.RandomBetween(0, 19) / 100;
                        float MinOffset = 0.04f; //Minimum Offset
                        //Theorectical Min = 0.04 (4%)                  Theoretical Max = 0.18 (18%)

                        float CornerOffset = 0.75f; //Additional Offset for being in corner
                        //Theoretical Min with Corneroffset = 0.84 (84%)    Theoretical Max with Corneroffset = 0.98 (98%)  <--- thats wwaayy in the corner, but still good  =)
                        switch (whichcorner)
                        {
                            case 0:
                                solarSystem2.Position = new Vector2(
                                    (-Data.Size.X + (Data.Size.X * (MinOffset + RandomoffsetX))),
                                    (-Data.Size.Y + (Data.Size.Y * (MinOffset + RandomoffsetX))));
                                ClaimedSpots.Add(solarSystem2.Position);
                                break;
                            case 1:
                                solarSystem2.Position = new Vector2(
                                    (Data.Size.X * (MinOffset + RandomoffsetX + CornerOffset)),
                                    (-Data.Size.Y + (Data.Size.Y * (MinOffset + RandomoffsetX))));
                                ClaimedSpots.Add(solarSystem2.Position);
                                break;
                            case 2:
                                solarSystem2.Position = new Vector2(
                                    (-Data.Size.X + (Data.Size.X * (MinOffset + RandomoffsetX))),
                                    (Data.Size.Y * (MinOffset + RandomoffsetX + CornerOffset)));
                                ClaimedSpots.Add(solarSystem2.Position);
                                break;
                            case 3:
                                solarSystem2.Position = new Vector2(
                                    (Data.Size.X * (MinOffset + RandomoffsetX + CornerOffset)),
                                    (Data.Size.Y * (MinOffset + RandomoffsetX + CornerOffset)));
                                ClaimedSpots.Add(solarSystem2.Position);
                                break;
                        }
                    }
                    else
                        solarSystem2.Position =
                            GenerateRandomCorners(
                                whichcorner); //This will distribute the extra planets from "/SolarSystems/Random" evenly

                    whichcorner += 1;
                    if (whichcorner > 3) whichcorner = 0;
                }
            }

            return whichcorner;
        }

        int CurrentStep;
        readonly float[] StepProportions = { 0.48f, 0.20f, 0.32f, };

        void UpdateCurrentStepProgress(float stepProgress)
        {
            float total = 0f;
            for (int i = 0; i < CurrentStep; ++i)
                total += StepProportions[i];
            total += StepProportions[CurrentStep] * stepProgress;
            PercentLoaded = total;
        }

        private void GenerateSystems()
        {
            Stopwatch total = Stopwatch.StartNew();
            Stopwatch s = Stopwatch.StartNew();

            CurrentStep = 0;
            GenerateInitialSystemData();
            Log.Info(ConsoleColor.Blue, $"  GenerateInitialSystemData elapsed: {s.NextMillis()}ms");

            CurrentStep = 1;
            for (int i = 0; i < Data.SolarSystemsList.Count; ++i)
            {
                SubmitSceneObjectsForRendering(Data.SolarSystemsList[i]);
                UpdateCurrentStepProgress(i / (float)Data.SolarSystemsList.Count);
            }
            Log.Info(ConsoleColor.Blue, $"  SubmitSceneObjectsForRendering elapsed: {s.NextMillis()}ms");
            
            CurrentStep = 2;
            FinalizeSolarSystems();
            FinalizeEmpires();
            Log.Info(ConsoleColor.Blue, $"  FinalizeEmpires elapsed: {s.NextMillis()}ms");
            Log.Info(ConsoleColor.DarkRed, $"TOTAL CreatingNewGameScreen Worker elapsed: {total.Elapsed.TotalMilliseconds}ms");
        }

        public Vector2 GenerateRandomSysPos(float spacing)
        {
            float safteyBreak = 1;
            Vector2 sysPos;
            do {
                spacing *= safteyBreak;
                sysPos = RandomMath.Vector2D(Data.Size.X - 100000f);
                safteyBreak *= .97f;
            } while (!SystemPosOK(sysPos, spacing));

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        public Vector2 GenerateRandomCorners(short corner) //Added by Gretman for Corners Game type
        {
            //Corner Values
            //0 = Top Left
            //1 = Top Right
            //2 = Bottom Left
            //3 = Bottom Right

            float SizeX = Data.Size.X * 2;     //Allow for new negative coordinates
            float SizeY = Data.Size.Y * 2;

            double CornerSizeX = SizeX * 0.4;    //20% of map per corner
            double CornerSizeY = SizeY * 0.4;

            double offsetX = 100000;
            double offsetY = 100000;
            if (corner == 1 || corner == 3)
                offsetX = SizeX * 0.6 - 100000;    //This creates a Huge blank "Neutral Zone" between corner areas
            if (corner == 2 || corner == 3)
                offsetY = SizeY * 0.6 - 100000;

            Vector2 sysPos;
            long noinfiniteloop = 0;
            do
            {
                sysPos = new Vector2(   RandomMath.RandomBetween(-Data.Size.X + (float)offsetX, -Data.Size.X + (float)(CornerSizeX + offsetX)),
                                        RandomMath.RandomBetween(-Data.Size.Y + (float)offsetY, -Data.Size.Y + (float)(CornerSizeY + offsetY)));
                noinfiniteloop += 1000;
            } 
            //Decrease the acceptable proximity slightly each attempt, so there wont be an infinite loop here on 'tiny' + 'SuperPacked' maps
            while (!SystemPosOK(sysPos, 400000 - noinfiniteloop));
            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        public void GenerateArm(int numOfStars, float rotation)
        {
            Random random = new Random();
            Vector2 vector2 = GalacticCenter;
            float num1 = (float)(2f / numOfStars * 2.0 * 3.14159274101257);
            for (int index = 0; index < numOfStars; ++index)
            {
                float num2 = (float)Math.Pow(Data.Size.X - 0.0850000008940697 * Data.Size.X, index / (float)numOfStars);
                float num3 = index * num1 + rotation;
                float x = vector2.X + (float)Math.Cos(num3) * num2;
                float y = vector2.Y + (float)Math.Sin(num3) * num2;
                Vector2 sysPos = new Vector2(RandomMath.RandomBetween(-10000f, 10000f) * index, (float)(RandomMath.RandomBetween(-10000f, 10000f) * (double)index / 4.0));
                sysPos = new Vector2(x, y) + sysPos;
                if (SystemPosOK(sysPos))
                {
                    ClaimedSpots.Add(sysPos);
                }
                else
                {
                    while (!SystemPosOK(sysPos))
                    {
                        sysPos.X = GalacticCenter.X + RandomMath.RandomBetween((float)(-(double)Data.Size.X / 2.0 + 0.0850000008940697 * Data.Size.X), (float)(Data.Size.X / 2.0 - 0.0850000008940697 * Data.Size.X));
                        sysPos.Y = GalacticCenter.Y + RandomMath.RandomBetween((float)(-(double)Data.Size.X / 2.0 + 0.0850000008940697 * Data.Size.X), (float)(Data.Size.X / 2.0 - 0.0850000008940697 * Data.Size.X));
                    }
                    ClaimedSpots.Add(sysPos);
                }
            }
        }

        private bool SystemPosOK(Vector2 sysPos)
        {
            foreach (Vector2 vector2 in ClaimedSpots)
            {   //Updated to make use of the negative map values -Gretman
                if (Vector2.Distance(vector2, sysPos) < 300000.0 * Scale 
                    || sysPos.X >  Data.Size.X || sysPos.Y >  Data.Size.Y 
                    || sysPos.X < -Data.Size.X || sysPos.Y < -Data.Size.Y)
                    return false;
            }
            return true;
        }

        private bool SystemPosOK(Vector2 sysPos, float spacing)
        {
            foreach (Vector2 vector2 in ClaimedSpots)
            {
                if (Vector2.Distance(vector2, sysPos) < spacing 
                    || sysPos.X >  Data.Size.X || sysPos.Y >  Data.Size.Y 
                    || sysPos.X < -Data.Size.X || sysPos.Y < -Data.Size.Y)
                    return false;
            }
            return true;
        }

        public static EmpireData CopyEmpireData(EmpireData data)
        {
            EmpireData empireData = new EmpireData
            {
                ArmorPiercingBonus        = data.ArmorPiercingBonus,
                BaseReproductiveRate      = data.BaseReproductiveRate,
                BonusFighterLevels        = data.BonusFighterLevels,
                CounterIntelligenceBudget = 0.0f,
                DefaultColonyShip         = data.DefaultColonyShip,
                DefaultSmallTransport     = data.DefaultSmallTransport,
                DefaultTroopShip          = data.DefaultTroopShip
            };
            if (string.IsNullOrEmpty(empireData.DefaultTroopShip))
            {
                empireData.DefaultTroopShip = empireData.PortraitName + " " + "Troop";
            }
            empireData.DefaultConstructor                     = data.DefaultConstructor;
            empireData.DefaultShipyard                        = data.DefaultShipyard;
            empireData.DiplomacyDialogPath                    = data.DiplomacyDialogPath;
            empireData.DiplomaticPersonality                  = data.DiplomaticPersonality;
            empireData.EconomicPersonality                    = data.EconomicPersonality;
            empireData.EmpireFertilityBonus                   = data.EmpireFertilityBonus;
            empireData.EmpireWideProductionPercentageModifier = data.EmpireWideProductionPercentageModifier;
            empireData.ExcludedDTraits                        = data.ExcludedDTraits;
            empireData.ExcludedETraits                        = data.ExcludedETraits;
            empireData.ExplosiveRadiusReduction               = data.ExplosiveRadiusReduction;
            empireData.FlatMoneyBonus                         = 0.0f;
            empireData.FTLModifier                            = data.FTLModifier;
            empireData.FTLPowerDrainModifier                  = data.FTLPowerDrainModifier;
            empireData.FuelCellModifier                       = data.FuelCellModifier;
            empireData.Inhibitors                             = data.Inhibitors;
            empireData.MassModifier                           = data.MassModifier;
            //Doctor: Armour Mass Mod
            empireData.ArmourMassModifier         = data.ArmourMassModifier;
            empireData.MissileDodgeChance         = data.MissileDodgeChance;
            empireData.MissileHPModifier          = data.MissileHPModifier;
            empireData.OrdnanceEffectivenessBonus = data.OrdnanceEffectivenessBonus;
            empireData.Privatization              = data.Privatization;
            empireData.SensorModifier             = data.SensorModifier;
            empireData.SpyModifier                = data.SpyModifier;
            empireData.SpoolTimeModifier          = data.SpoolTimeModifier;
            empireData.StartingScout              = data.StartingScout;
            empireData.StartingShip               = data.StartingShip;
            empireData.SubLightModifier           = data.SubLightModifier;
            empireData.TaxRate                    = data.TaxRate;
            empireData.TroopDescriptionIndex      = data.TroopDescriptionIndex;
            empireData.TroopNameIndex             = data.TroopNameIndex;
            empireData.PowerFlowMod               = data.PowerFlowMod;
            empireData.ShieldPowerMod             = data.ShieldPowerMod;
            //Doctor: Civilian Maint Mod
            empireData.CivMaintMod = data.CivMaintMod;

            empireData.Traits = new RacialTrait
            {
                Aquatic              = data.Traits.Aquatic,
                Assimilators         = data.Traits.Assimilators,
                B                    = 128f,
                Blind                = data.Traits.Blind,
                BonusExplored        = data.Traits.BonusExplored,
                Burrowers            = data.Traits.Burrowers,
                ConsumptionModifier  = data.Traits.ConsumptionModifier,
                Cybernetic           = data.Traits.Cybernetic,
                DiplomacyMod         = data.Traits.DiplomacyMod,
                DodgeMod             = data.Traits.DodgeMod,
                EnergyDamageMod      = data.Traits.EnergyDamageMod,
                FlagIndex            = data.Traits.FlagIndex,
                G                    = 128f,
                GenericMaxPopMod     = data.Traits.GenericMaxPopMod,
                GroundCombatModifier = data.Traits.GroundCombatModifier,
                InBordersSpeedBonus  = data.Traits.InBordersSpeedBonus,
                MaintMod             = data.Traits.MaintMod,
                Mercantile           = data.Traits.Mercantile,
                Militaristic         = data.Traits.Militaristic,
                Miners               = data.Traits.Miners,
                ModHpModifier        = data.Traits.ModHpModifier,
                PassengerModifier    = data.Traits.PassengerModifier,
                ProductionMod        = data.Traits.ProductionMod,
                R                    = 128f,
                RepairMod            = data.Traits.RepairMod,
                ReproductionMod      = data.Traits.ReproductionMod,
                PopGrowthMax         = data.Traits.PopGrowthMax,
                PopGrowthMin         = data.Traits.PopGrowthMin,
                ResearchMod          = data.Traits.ResearchMod,
                ShipCostMod          = data.Traits.ShipCostMod,
                ShipType             = data.Traits.ShipType,
                Singular             = data.RebelSing,
                Plural               = data.RebelPlur,
                Spiritual            = data.Traits.Spiritual,
                SpyMultiplier        = data.Traits.SpyMultiplier,
                SpyModifier          = data.Traits.SpyModifier,
                TaxMod               = data.Traits.TaxMod
            };
            empireData.TurnsBelowZero = 0;
            return empireData;
        }

        public override bool HandleInput(InputState input)
        {
            if (BackgroundTask?.IsComplete != true || !input.InGameSelect)
                return false;

            GameAudio.StopGenericMusic(immediate: false);
            
            us = new UniverseScreen(Data)
            {
                player         = Player,
                CamPos = new Vector3(-playerShip.Center.X, playerShip.Center.Y, 5000f),
                ScreenManager  = ScreenManager,
                GameScale      = Scale
            };

            EmpireShipBonuses.RefreshBonuses();

            UniverseScreen.GameScaleStatic = Scale;
            ScreenManager.AddScreen(us);

            Log.Info("CreatingNewGameScreen.UpdateAllSystems(0.01)");
            us.UpdateAllSystems(0.01f);
            mmscreen.OnPlaybackStopped(null, null);
            ScreenManager.RemoveScreen(mmscreen);
 
            ExitScreen();
            return true;
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.GraphicsDevice.Clear(Color.Black);
            batch.Begin();

            if (!BackgroundTask.IsComplete)
            {
                // heavily throttle main draw thread, so the worker thread can turbo
                Thread.Sleep(33);
            }

            int width  = ScreenWidth;
            int height = ScreenHeight;
            batch.Draw(LoadingScreenTexture, new Rectangle(width / 2 - 960, height / 2 - 540, 1920, 1080), Color.White);
            
            var r = new Rectangle(width / 2 - 150, height - 25, 300, 25);
            new ProgressBar(r) { Max = 100f, Progress = PercentLoaded * 100f }.Draw(batch);

            var position = new Vector2(ScreenCenter.X - 250f, (float)(r.Y - Fonts.Arial12Bold.MeasureString(AdviceText).Y - 5.0));
            batch.DrawString(Fonts.Arial12Bold, AdviceText, position, Color.White);
            
            if (BackgroundTask.IsComplete)
            {
                PercentLoaded = 1f;
                position.Y = (float)(position.Y - Fonts.Pirulen16.LineSpacing - 10.0);
                string token = Localizer.Token(2108);
                position.X = ScreenCenter.X - Fonts.Pirulen16.MeasureString(token).X / 2f;

                GameTime gameTime = Game1.Instance.GameTime;
                var color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, 
                    (byte)(Math.Abs(Math.Sin(gameTime.TotalGameTime.TotalSeconds)) * byte.MaxValue));
                batch.DrawString(Fonts.Pirulen16, token, position, color);
            }

            batch.End();
        }

        protected override void Destroy()
        {
            lock (this) {
                WorkerBeginEvent?.Dispose(ref WorkerBeginEvent);
                WorkerCompletedEvent?.Dispose(ref WorkerCompletedEvent);
                LoadingScreenTexture?.Dispose(ref LoadingScreenTexture);                
            }
            base.Destroy();
        }

        private struct SysDisPair
        {
            public SolarSystem System;
            public float Distance;
        }
    }
}
