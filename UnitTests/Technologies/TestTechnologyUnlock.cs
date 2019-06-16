﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Technologies
{
    [TestClass]
    public class TestTechnologyUnlock : StarDriveTest, IDisposable
    {
        readonly GameDummy Game;
        NotificationManager NotificationManager;

        public TestTechnologyUnlock()
        {
            ResourceManager.LoadTechContentForTesting();
            // UniverseScreen requires a game instance
            Game = new GameDummy();
            Game.Create();
        }

        public void Dispose()
        {
            Empire.Universe?.ExitScreen();
            Game.Dispose();
        }

        void CreateTestEnv(out Empire empire)
        {
            var data = new UniverseData();
            empire = data.CreateEmpire(ResourceManager.MajorRaces[0]);
            empire.isPlayer = true;
            Empire.Universe = new UniverseScreen(data, empire);
            Empire.Universe.player = empire;
            NotificationManager = new NotificationManager(Empire.Universe.ScreenManager, Empire.Universe);
            Empire.Universe.aw = new AutomationWindow(Empire.Universe);

        }
        /// <summary>
        /// Add 12 notifications. 4 spy, 4 planet, 4, 4 spy
        /// </summary>
        /// <param name="empire"></param>

        [TestMethod]
        public void TestTechUnlock()
        {
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "FrigateConstruction");
            Assert.IsTrue(tech.Unlocked);
            Dispose();
        }

        TechEntry UnlockTech(Empire empire, string UID)
        {
            TechEntry tech = empire.GetTechEntry(UID);
            empire.UnlockTech(tech.UID, TechUnlockType.Normal);
            return tech;
        }

        [TestMethod]
        public void TestHullUnlock()
        {
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "FrigateConstruction");
            foreach (string item in tech.GetUnLockableHulls(empire))
                Assert.IsTrue(empire.IsHullUnlocked(item));
            Dispose();
        }

        [TestMethod]
        public void TestModuleUnlock()
        {
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "FrigateConstruction");
            foreach (Technology.UnlockedMod item in tech.GetUnlockableModules(empire))
                Assert.IsTrue(empire.IsModuleUnlocked(item.ModuleUID));
            Dispose();
        }

        [TestMethod]
        public void TestBuildingUnlock()
        {
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "IndustrialFoundations");
            foreach (var item in tech.Tech.BuildingsUnlocked)
                Assert.IsTrue(empire.IsBuildingUnlocked(item.Name));
            Dispose();
        }

        [TestMethod]
        public void TestBonusUnlock()
        {
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "Ace Training");
            var currentBonus = empire.data.BonusFighterLevels;
            foreach (var item in tech.Tech.BonusUnlocked)
            {
                Assert.IsTrue(empire.data.BonusFighterLevels > 0);
                Assert.That.Equal(0, currentBonus - empire.data.BonusFighterLevels);
            }
            Dispose();
        }
        [TestMethod]
        public void TestBonusStacking()
        {
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "Ace Training");
            var currentBonus = empire.data.BonusFighterLevels;
            foreach (var item in tech.Tech.BonusUnlocked)
            {
                Assert.IsTrue(empire.data.BonusFighterLevels > 0);
                Assert.That.Equal(0, currentBonus - empire.data.BonusFighterLevels);
            }
            foreach (var item in tech.Tech.BonusUnlocked)
            {
                Assert.IsTrue(empire.data.BonusFighterLevels > 0);
                Assert.That.Equal(0, currentBonus - empire.data.BonusFighterLevels);
            }
            Dispose();
        }

    }
}
