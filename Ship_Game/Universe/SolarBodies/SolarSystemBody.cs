﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public enum SunZone
    {
        Near,
        Habital,
        Far,
        VeryFar,
        Any
    }

    public enum PlanetType
    {
        Other,
        Barren,
        Terran,
    }
    public enum Richness
    {
        UltraPoor,
        Poor,
        Average,
        Rich,
        UltraRich,
    }
    public class SolarSystemBody : Explorable
    {
        public Matrix RingWorld;
        public SceneObject SO;
        // ReSharper disable once InconsistentNaming some conflict issues makhing this GUID and possible save and load issues changing this. 
        public Guid guid = Guid.NewGuid();
        protected AudioEmitter Emit = new AudioEmitter();
        public Vector2 Center;
        public SolarSystem ParentSystem;
        public Matrix CloudMatrix;
        public bool HasEarthLikeClouds;
        public string SpecialDescription;
        public bool HasShipyard;
        public string Name;
        public string Description;
        public Empire Owner;
        public float OrbitalAngle;
        public float OrbitalRadius;
        public int PlanetType;
        public bool HasRings;
        public float PlanetTilt;
        public float RingTilt;
        public float Scale;
        public Matrix World;
        public bool Habitable;
        public string PlanetComposition;
        public string Type;
        protected float Zrotate;
        public int DevelopmentLevel;
        public bool UniqueHab = false;
        public int UniqueHabPercent;
        public SunZone Zone { get; private set; }
        protected AudioEmitter Emitter;
        protected float InvisibleRadius;
        public float GravityWellRadius { get; protected set; }
        public Array<PlanetGridSquare> TilesList = new Array<PlanetGridSquare>(35);
        protected float HabitalTileChance = 10;
        public float Population;
        public float Density;
        public float Fertility;
        public float MineralRichness;
        public float MaxPopulation;
        public Array<Building> BuildingList = new Array<Building>();
        public float ShieldStrengthCurrent;
        public float ShieldStrengthMax;
        protected Shield Shield;
        private float PosUpdateTimer = 1f;
        private float ZrotateAmount = 0.03f;
        public string DevelopmentStatus = "Undeveloped";
        public float TerraformPoints;
        public float TerraformToAdd;

        protected void PlayPlanetSfx(string sfx, Vector3 position)
        {
            if (Emitter == null)
                Emitter = new AudioEmitter();
            Emitter.Position = position;
            GameAudio.PlaySfxAsync(sfx, Emitter);
        }

        public float ObjectRadius
        {
            get => SO != null ? SO.WorldBoundingSphere.Radius : InvisibleRadius;
            set => InvisibleRadius = SO != null ? SO.WorldBoundingSphere.Radius : value;
        }
        public string GetTypeTranslation()
        {
            switch (Type)
            {
                case "Terran": return Localizer.Token(1447);
                case "Barren": return Localizer.Token(1448);
                case "Gas Giant": return Localizer.Token(1449);
                case "Volcanic": return Localizer.Token(1450);
                case "Tundra": return Localizer.Token(1451);
                case "Desert": return Localizer.Token(1452);
                case "Steppe": return Localizer.Token(1453);
                case "Swamp": return Localizer.Token(1454);
                case "Ice": return Localizer.Token(1455);
                case "Oceanic": return Localizer.Token(1456);
                default: return "";
            }
        }
        protected void GenerateType(SunZone sunZone)
        {
            for (int x = 0; x < 5; x++)
            {
                Type = "";
                PlanetComposition = "";
                HasEarthLikeClouds = false;
                Habitable = false;
                MaxPopulation = 0;
                Fertility = 0;
                PlanetType = RandomMath.IntBetween(1, 24);
                TilesList.Clear();
                ApplyPlanetType();
                if (Zone == sunZone || (Zone == SunZone.Any && sunZone == SunZone.Near)) break;
                if (x > 2 && Zone == SunZone.Any) break;
            }
        }

        protected void ApplyPlanetType()
        {
            HabitalTileChance = 20;
            switch (PlanetType)
            {
                case 1:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1700);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.RandomBetween(0.8f, 1.5f);
                    Zone = SunZone.Habital;
                    HabitalTileChance = 20;
                    break;
                case 2:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    Zone = SunZone.Far;
                    break;
                case 3:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 4:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 5:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 6:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    Zone = SunZone.Far;
                    break;
                case 7:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 8:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 9:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1705);
                    Zone = SunZone.Any;
                    break;
                case 10:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1706);
                    Zone = SunZone.Far;
                    break;
                case 11:
                    Type = "Tundra";
                    PlanetComposition = Localizer.Token(1707);
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.AvgRandomBetween(0.5f, 1f);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    Zone = SunZone.Far;
                    HabitalTileChance = 20;
                    break;
                case 12:
                    Type = "Gas Giant";
                    Habitable = false;
                    PlanetComposition = Localizer.Token(1708);
                    Zone = SunZone.Far;
                    break;
                case 13:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1709);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.AvgRandomBetween(1f, 3f);
                    Zone = SunZone.Habital;
                    HabitalTileChance = 75;
                    break;
                case 14:
                    Type = "Desert";
                    PlanetComposition = Localizer.Token(1710);
                    HabitalTileChance = RandomMath.AvgRandomBetween(10f, 45f);
                    MaxPopulation = (int)HabitalTileChance * 100;
                    Fertility = RandomMath.AvgRandomBetween(-2f, 2f);
                    Fertility = Fertility < .8f ? .8f : Fertility;
                    Habitable = true;
                    Zone = SunZone.Near;
                    break;
                case 15:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    PlanetType = 26;
                    Zone = SunZone.Far;
                    break;
                case 16:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1712);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 17:
                    Type = "Ice";
                    PlanetComposition = Localizer.Token(1713);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.VeryFar;
                    break;
                case 18:
                    Type = "Steppe";
                    PlanetComposition = Localizer.Token(1714);
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 45f);
                    MaxPopulation = (int)HabitalTileChance * 200;
                    Fertility = RandomMath.AvgRandomBetween(0f, 2f);
                    Fertility = Fertility < .8f ? .8f : Fertility;
                    Habitable = true;
                    Zone = SunZone.Habital;
                    break;
                case 19:
                    Type = "Swamp";
                    Habitable = true;
                    PlanetComposition = Localizer.Token(1715);
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 45f);
                    MaxPopulation = HabitalTileChance * 200;
                    Fertility = RandomMath.AvgRandomBetween(-2f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    HasEarthLikeClouds = true;
                    Zone = SunZone.Near;
                    break;
                case 20:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    Zone = SunZone.Far;
                    break;
                case 21:
                    Type = "Oceanic";
                    PlanetComposition = Localizer.Token(1716);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 45f);
                    MaxPopulation = HabitalTileChance * 100 + 1500;
                    Fertility = RandomMath.AvgRandomBetween(-3f, 5f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 22:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1717);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopulation = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(0f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 23:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1718);
                    Zone = SunZone.Near;
                    break;
                case 24:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1719);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 25:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1720);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopulation = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(-.50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 26:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    Zone = SunZone.Far;
                    break;
                case 27:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1721);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopulation = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(-50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 29:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1722);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(50f, 80f);
                    MaxPopulation = HabitalTileChance * 150f;
                    Fertility = RandomMath.AvgRandomBetween(-50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
            }
        }
        public void SetPlanetAttributes(bool setType = true)
        {
            HasEarthLikeClouds = false;
            float richness = RandomMath.RandomBetween(0.0f, 100f);
            if (richness >= 92.5f) MineralRichness = RandomMath.RandomBetween(2.00f, 2.50f);
            else if (richness >= 85.0f) MineralRichness = RandomMath.RandomBetween(1.50f, 2.00f);
            else if (richness >= 25.0f) MineralRichness = RandomMath.RandomBetween(0.75f, 1.50f);
            else if (richness >= 12.5f) MineralRichness = RandomMath.RandomBetween(0.25f, 0.75f);
            else if (richness < 12.5f) MineralRichness = RandomMath.RandomBetween(0.10f, 0.25f);

            if (setType) ApplyPlanetType();
            if (!Habitable)
                MineralRichness = 0.0f;


            AddEventsAndCommodities();
        }

        protected void AddEventsAndCommodities()
        {
            switch (Type)
            {
                case "Terran":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.TerranHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.TerranChance, item.TerranInstanceMax);
                    break;
                case "Steppe":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.SteppeHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.SteppeChance, item.SteppeInstanceMax);
                    break;
                case "Ice":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.IceHab ?? 15);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.IceChance, item.IceInstanceMax);
                    break;
                case "Barren":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.BarrenHab ?? 0);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.BarrenChance, item.BarrenInstanceMax);
                    break;
                case "Tundra":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.OceanHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.TundraChance, item.TundraInstanceMax);
                    break;
                case "Desert":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.OceanHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.DesertChance, item.DesertInstanceMax);
                    break;
                case "Oceanic":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.OceanHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.OceanicChance, item.OceanicInstanceMax);
                    break;
                case "Swamp":
                    SetTileHabitability(GlobalStats.ActiveModInfo?.SteppeHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.SwampChance, item.SwampInstanceMax);
                    break;
            }
            AddTileEvents();
        }

        protected void SetTileHabitability(float habChance)
        {
            {
                if (UniqueHab)
                {
                    habChance = UniqueHabPercent;
                }
                bool habitable = false;
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        if (habChance > 0)
                            habitable = RandomMath.RandomBetween(0, 100) < habChance;

                        TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, null, habitable));
                    }
                }
            }
        }

        protected void AddTileEvents()
        {
            if (RandomMath.RandomBetween(0.0f, 100f) <= 15 && Habitable)
            {
                Array<string> list = new Array<string>();
                foreach (var kv in ResourceManager.BuildingsDict)
                {
                    if (!string.IsNullOrEmpty(kv.Value.EventTriggerUID) && !kv.Value.NoRandomSpawn)
                        list.Add(kv.Key);
                }
                int index = (int)RandomMath.RandomBetween(0f, list.Count + 0.85f);
                if (index >= list.Count)
                    index = list.Count - 1;
                var b = AssignBuildingToRandomTile(ResourceManager.CreateBuilding(list[index]));
                BuildingList.Add(b.building);
                Log.Info($"Event building : {b.building.Name} : created on {Name}");
            }
        }
        public void SpawnRandomItem(RandomItem randItem, float chance, float instanceMax)
        {
            if ((GlobalStats.HardcoreRuleset || !randItem.HardCoreOnly) && RandomMath.RandomBetween(0.0f, 100f) < chance)
            {
                int itemCount = (int)RandomMath.RandomBetween(1f, instanceMax + 0.95f);
                for (int i = 0; i < itemCount; ++i)
                {
                    if (!ResourceManager.BuildingsDict.ContainsKey(randItem.BuildingID)) continue;
                    var pgs = AssignBuildingToRandomTile(ResourceManager.CreateBuilding(randItem.BuildingID));
                    pgs.Habitable = true;
                    Log.Info($"Resouce Created : '{pgs.building.Name}' : on '{Name}' ");
                    BuildingList.Add(pgs.building);
                }
            }
        }
        public PlanetGridSquare AssignBuildingToRandomTile(Building b, bool habitable = false)
        {
            PlanetGridSquare[] list;
            list = !habitable ? TilesList.FilterBy(planetGridSquare => planetGridSquare.building == null) 
                : TilesList.FilterBy(planetGridSquare => planetGridSquare.building == null && planetGridSquare.Habitable);
            if (list.Length == 0)
                return null;

            int index = RandomMath.InRange(list.Length - 1);
            var targetPGS = TilesList.Find(pgs => pgs == list[index]);
            targetPGS.building = b;
            return targetPGS;

        }
        public void SetPlanetAttributes(float mrich)
        {
            float num1 = mrich;
            if (num1 >= 87.5f)
            {
                //this.richness = Planet.Richness.UltraRich;
                MineralRichness = 2.5f;
            }
            else if (num1 >= 75f)
            {
                //this.richness = Planet.Richness.Rich;
                MineralRichness = 1.5f;
            }
            else if (num1 >= 25.0)
            {
                //this.richness = Planet.Richness.Average;
                MineralRichness = 1f;
            }
            else if (num1 >= 12.5)
            {
                MineralRichness = 0.5f;
                //this.richness = Planet.Richness.Poor;
            }
            else if (num1 < 12.5)
            {
                MineralRichness = 0.1f;
                //this.richness = Planet.Richness.UltraPoor;
            }

            TilesList.Clear();
            switch (PlanetType)
            {
                case 1:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1700);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.RandomBetween(0.5f, 2f);
                    HabitalTileChance = 20;
                    break;
                case 2:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 3:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 4:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 5:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 6:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 7:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 8:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 9:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1705);
                    break;
                case 10:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1706);
                    break;
                case 11:
                    Type = "Tundra";
                    PlanetComposition = Localizer.Token(1707);
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.RandomBetween(0.5f, 0.9f);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    HabitalTileChance = 20;
                    break;
                case 12:
                    Type = "Gas Giant";
                    Habitable = false;
                    PlanetComposition = Localizer.Token(1708);
                    break;
                case 13:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1709);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(0.8f, 3f);
                    HabitalTileChance = 75;
                    break;
                case 14:
                    Type = "Desert";
                    PlanetComposition = Localizer.Token(1710);
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    Fertility = RandomMath.RandomBetween(0.2f, 1.8f);
                    Habitable = true;
                    HabitalTileChance = 20;
                    break;
                case 15:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    PlanetType = 26;
                    break;
                case 16:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1712);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 17:
                    Type = "Ice";
                    PlanetComposition = Localizer.Token(1713);
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    HabitalTileChance = 10;
                    break;
                case 18:
                    Type = "Steppe";
                    PlanetComposition = Localizer.Token(1714);
                    Fertility = RandomMath.RandomBetween(0.4f, 1.4f);
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(2000f, 4000f);
                    Habitable = true;
                    HabitalTileChance = 50;
                    break;
                case 19:
                    Type = "Swamp";
                    Habitable = true;
                    PlanetComposition = Localizer.Token(1712);
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    Fertility = RandomMath.RandomBetween(1f, 5f);
                    HasEarthLikeClouds = true;
                    HabitalTileChance = 20;
                    break;
                case 20:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 21:
                    Type = "Oceanic";
                    PlanetComposition = Localizer.Token(1716);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(3000f, 6000f);
                    Fertility = RandomMath.RandomBetween(2f, 5f);
                    HabitalTileChance = 20;
                    break;
                case 22:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1717);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 75;
                    break;
                case 23:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1718);
                    break;
                case 24:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1719);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 25:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1720);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 2f);
                    HabitalTileChance = 90;
                    break;
                case 26:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 27:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1721);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 60;
                    break;
                case 29:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1722);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 50;
                    break;
            }

            if (!Habitable)
                MineralRichness = 0.0f;
            else
            {
                if (!(Fertility > 0)) return;
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        bool habitableTile = (int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance;
                        TilesList.Add(new PlanetGridSquare(x, y, 0, 0, 0, null, habitableTile));

                    }
                }
            }


        }
        public void LoadAttributes()
        {
            switch (PlanetType)
            {
                case 1:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1700);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    break;
                case 2:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 3:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    break;
                case 4:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    break;
                case 5:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    Habitable = true;
                    break;
                case 6:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 7:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1704);
                    Habitable = true;
                    break;
                case 8:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1703);
                    Habitable = true;
                    break;
                case 9:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1705);
                    break;
                case 10:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1706);
                    break;
                case 11:
                    Type = "Tundra";
                    PlanetComposition = Localizer.Token(1707);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    break;
                case 12:
                    Type = "Gas Giant";
                    Habitable = false;
                    PlanetComposition = Localizer.Token(1708);
                    break;
                case 13:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1709);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 14:
                    Type = "Desert";
                    PlanetComposition = Localizer.Token(1710);
                    Habitable = true;
                    break;
                case 15:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    PlanetType = 26;
                    break;
                case 16:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1712);
                    Habitable = true;
                    break;
                case 17:
                    Type = "Ice";
                    PlanetComposition = Localizer.Token(1713);
                    Habitable = true;
                    break;
                case 18:
                    Type = "Steppe";
                    PlanetComposition = Localizer.Token(1714);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    break;
                case 19:
                    Type = "Swamp";
                    PlanetComposition = "";
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 20:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 21:
                    Type = "Oceanic";
                    PlanetComposition = Localizer.Token(1716);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 22:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1717);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 23:
                    Type = "Volcanic";
                    PlanetComposition = Localizer.Token(1718);
                    break;
                case 24:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1719);
                    Habitable = true;
                    break;
                case 25:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1720);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 26:
                    Type = "Gas Giant";
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 27:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1721);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 29:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1722);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
            }
        }
        public string GetRichness()
        {
            if (MineralRichness > 2.5)
                return Localizer.Token(1442);
            if (MineralRichness > 1.5)
                return Localizer.Token(1443);
            if (MineralRichness > 0.75)
                return Localizer.Token(1444);
            if (MineralRichness > 0.25)
                return Localizer.Token(1445);
            else
                return Localizer.Token(1446);
        }

        public string GetOwnerName()
        {
            if (Owner != null)
                return Owner.data.Traits.Singular;
            return Habitable ? " None" : " Uninhabitable";
        }

        public string GetTile()
        {
            if (Type != "Terran")
                return Type;
            switch (PlanetType)
            {
                case 1: return "Terran";
                case 13: return "Terran_2";
                case 22: return "Terran_3";
                default: return "Terran";
            }
        }

        public void InitializePlanetMesh(GameScreen screen)
        {
            Shield = ShieldManager.AddPlanetaryShield(Center);
            UpdateDescription();
            CreatePlanetSceneObject(screen);

            GravityWellRadius = (float)(GlobalStats.GravityWellRange * (1 + ((Math.Log(Scale)) / 1.5)));
        }

        protected void UpdatePosition(float elapsedTime)
        {
            Zrotate += ZrotateAmount * elapsedTime;
            if (!Empire.Universe.Paused)
            {
                OrbitalAngle += (float)Math.Asin(15.0 / OrbitalRadius);
                if (OrbitalAngle >= 360.0f)
                    OrbitalAngle -= 360f;
            }
            PosUpdateTimer -= elapsedTime;
            if (PosUpdateTimer <= 0.0f || ParentSystem.isVisible)
            {
                PosUpdateTimer = 5f;
                Center = ParentSystem.Position.PointOnCircle(OrbitalAngle, OrbitalRadius);
            }
            if (ParentSystem.isVisible)
            {
                SO.World = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(Scale) *
                           Matrix.CreateRotationZ(-Zrotate) * Matrix.CreateRotationX(-45f.ToRadians()) *
                           Matrix.CreateTranslation(new Vector3(Center, 2500f));
                CloudMatrix = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(Scale) *
                              Matrix.CreateRotationZ((float) (-Zrotate / 1.5)) *
                              Matrix.CreateRotationX(-45f.ToRadians()) *
                              Matrix.CreateTranslation(new Vector3(Center, 2500f));
                RingWorld = Matrix.Identity * Matrix.CreateRotationX(RingTilt.ToRadians()) *
                            Matrix.CreateScale(5f) * Matrix.CreateTranslation(new Vector3(Center, 2500f));
                SO.Visibility = ObjectVisibility.Rendered;
            }
            else
                SO.Visibility = ObjectVisibility.None;
        }

        protected void CreatePlanetSceneObject(GameScreen screen)
        {
            if (SO != null)
                screen?.RemoveObject(SO);

            SO = ResourceManager.GetPlanetarySceneMesh("Model/SpaceObjects/planet_" + PlanetType);
            SO.World = Matrix.CreateScale(Scale * 3)
                       * Matrix.CreateTranslation(new Vector3(Center, 2500f));

            RingWorld = Matrix.CreateRotationX(RingTilt.ToRadians())
                        * Matrix.CreateScale(5f)
                        * Matrix.CreateTranslation(new Vector3(Center, 2500f));

            screen?.AddObject(SO);
        }
        protected void UpdateDescription()
        {
            if (SpecialDescription != null)
            {
                Description = SpecialDescription;
            }
            else
            {
                Description = "";
                var planet1 = this;
                string str1 = planet1.Description + Name + " " + PlanetComposition + ". ";
                planet1.Description = str1;
                if (Fertility > 2)
                {
                    if (PlanetType == 21)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1729);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 13 || PlanetType == 22)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1730);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1731);
                        planet2.Description = str2;
                    }
                }
                else if (Fertility > 1)
                {
                    if (PlanetType == 19)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1732);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 21)
                        Description += Localizer.Token(1733);
                    else if (PlanetType == 13 || PlanetType == 22)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1734);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1735);
                        planet2.Description = str2;
                    }
                }
                else if (Fertility > 0.6f)
                {
                    if (PlanetType == 14)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1736);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 21)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1737);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 17)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1738);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 19)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1739);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 18)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1740);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 11)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1741);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 13 || PlanetType == 22)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1742);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1743);
                        planet2.Description = str2;
                    }
                }
                else
                {
                    switch (PlanetType) {
                        case 9:
                        case 23:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1744);
                            planet2.Description = str2;
                            break;
                        }
                        case 20:
                        case 15:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1745);
                            planet2.Description = str2;
                            break;
                        }
                        case 17:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1746);
                            planet2.Description = str2;
                            break;
                        }
                        case 18:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1747);
                            planet2.Description = str2;
                            break;
                        }
                        case 11:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1748);
                            planet2.Description = str2;
                            break;
                        }
                        case 14:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1749);
                            planet2.Description = str2;
                            break;
                        }
                        case 2:
                        case 6:
                        case 10:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1750);
                            planet2.Description = str2;
                            break;
                        }
                        case 3:
                        case 4:
                        case 16:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1751);
                            planet2.Description = str2;
                            break;
                        }
                        case 1:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1752);
                            planet2.Description = str2;
                            break;
                        }
                        default:
                            if (Habitable)
                            {
                                var planet2 = this;
                                string str2 = planet2.Description ?? "";
                                planet2.Description = str2;
                            }
                            else
                            {
                                var planet2 = this;
                                string str2 = planet2.Description + Localizer.Token(1753);
                                planet2.Description = str2;
                            }
                            break;
                    }
                }
                if (Fertility < 0.6f && MineralRichness >= 2 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1754);
                    planet2.Description = str2;
                    if (MineralRichness > 3)
                    {
                        var planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1755);
                        planet3.Description = str3;
                    }
                    else if (MineralRichness >= 2)
                    {
                        var planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1756);
                        planet3.Description = str3;
                    }
                    else
                    {
                        if (MineralRichness < 1)
                            return;
                        var planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1757);
                        planet3.Description = str3;
                    }
                }
                else if (MineralRichness > 3 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1758);
                    planet2.Description = str2;
                }
                else if (MineralRichness >= 2 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Name + Localizer.Token(1759);
                    planet2.Description = str2;
                }
                else if (MineralRichness >= 1 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Name + Localizer.Token(1760);
                    planet2.Description = str2;
                }
                else
                {
                    if (MineralRichness >= 1 || !Habitable)
                        return;
                    if (PlanetType == 14)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Name + Localizer.Token(1761);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Name + Localizer.Token(1762);
                        planet2.Description = str2;
                    }
                }
            }
        }
        public void Terraform()
        {
            switch (PlanetType)
            {
                case 7:
                    Type = "Barren";
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopulation = (int)RandomMath.RandomBetween(0.0f, 500f);
                    HasEarthLikeClouds = false;
                    Habitable = true;
                    break;
                case 11:
                    Type = "Tundra";
                    PlanetComposition = Localizer.Token(1724);
                    MaxPopulation = (int)RandomMath.RandomBetween(4000f, 8000f);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    break;
                case 14:
                    Type = "Desert";
                    PlanetComposition = Localizer.Token(1725);
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    Habitable = true;
                    break;
                case 18:
                    Type = "Steppe";
                    PlanetComposition = Localizer.Token(1726);
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(2000f, 4000f);
                    Habitable = true;
                    break;
                case 19:
                    Type = "Swamp";
                    PlanetComposition = Localizer.Token(1727);
                    Habitable = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(1000f, 3000f);
                    HasEarthLikeClouds = true;
                    break;
                case 21:
                    Type = "Oceanic";
                    PlanetComposition = Localizer.Token(1728);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(3000f, 6000f);
                    break;
                case 22:
                    Type = "Terran";
                    PlanetComposition = Localizer.Token(1717);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopulation = (int)RandomMath.RandomBetween(6000f, 10000f);
                    break;
            }
            foreach (PlanetGridSquare planetGridSquare in TilesList)
            {
                switch (Type)
                {
                    case "Barren":
                        if (!planetGridSquare.Biosphere)
                        {
                            planetGridSquare.Habitable = false;
                            continue;
                        }
                        else
                            continue;
                    case "Terran":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Swamp":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Ocean":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Desert":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Steppe":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    case "Tundra":
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
                        {
                            planetGridSquare.Habitable = true;
                            continue;
                        }
                        else
                            continue;
                    default:
                        continue;
                }
            }
            UpdateDescription();
            CreatePlanetSceneObject(Empire.Universe);
        }

        
    }
}