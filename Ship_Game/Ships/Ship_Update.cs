using Microsoft.Xna.Framework;
using Ship_Game.AI;
using SynapseGaming.LightingSystem.Core;
using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        // > 0 if ship is outside frustum
        float OutsideFrustumTimer;

        // after X seconds of ships being invisible, we remove their scene objects
        const float RemoveInvisibleSceneObjectsAfterTime = 15f;

        public void ShowSceneObjectAt(Vector3 position)
        {
            if (ShipSO == null)
            {
                Log.Info("Showing SceneObject");
                CreateSceneObject();
            }
            ShipSO.World = Matrix.CreateTranslation(position);
            ShipSO.Visibility = GlobalStats.ShipVisibility;
        }

        // NOTE: This is called on the main UI Thread by UniverseScreen
        // check UniverseScreen.QueueShipSceneObject()
        public void CreateSceneObject()
        {
            if (StarDriveGame.Instance == null || ShipSO != null)
                return; // allow creating invisible ships in Unit Tests

            //Log.Info($"CreateSO {Id} {Name}");
            shipData.LoadModel(out ShipSO, Empire.Universe.ContentManager);
            ShipSO.World = Matrix.CreateTranslation(new Vector3(Position, 0f));

            // Since we just created the object, it must be visible
            UpdateVisibility(0f, forceVisible: true);

            ScreenManager.Instance.AddObject(ShipSO);
        }

        public void RemoveSceneObject()
        {
            SceneObject so = ShipSO;
            ShipSO = null;
            if (so != null)
            {
                //Log.Info($"RemoveSO {Id} {Name}");
                so.Clear();
                ScreenManager.Instance.RemoveObject(so);
            }
        }

        void UpdateVisibility(float elapsedTime, bool forceVisible)
        {
            bool inFrustum = forceVisible || (System == null || System.isVisible)
                && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView
                && (Empire.Universe.Frustum.Contains(Position, 2000f) || 
                    (AI?.Target != null &&
                     Empire.Universe.Frustum.Contains(AI.Target.Position, WeaponsMaxRange)));

            InFrustum = inFrustum;
            if (inFrustum) OutsideFrustumTimer = 0f;
            else           OutsideFrustumTimer += elapsedTime;

            if (ShipSO != null) // allow null SceneObject to support ship.Update in UnitTests
            {
                if (!inFrustum && OutsideFrustumTimer > RemoveInvisibleSceneObjectsAfterTime)
                {
                    RemoveSceneObject();
                }
                else
                {
                    ShipSO.Visibility = inFrustum ? GlobalStats.ShipVisibility : ObjectVisibility.None;
                }
            }
        }

        public override void Update(float elapsedTime)
        {
            if (!ShipInitialized)
                return;

            if (Active && (ModuleSlotsDestroyed || Health <= 0))
            {
                if (Health <= 0)
                {
                    Log.Warning($"Ship Terminated due to 0 health bug. Active: {Active}, has live modules: {ModuleSlotsDestroyed}");
                }
                Die(null, true);
            }

            if (!Active)
                return;

            if (RandomEventManager.ActiveEvent?.InhibitWarp == true)
            {
                Inhibited = true;
                InhibitedTimer = 10f;
            }

            if (ScuttleTimer > -1f || ScuttleTimer < -1f)
            {
                ScuttleTimer -= elapsedTime;
                if (ScuttleTimer <= 0f) Die(null, true);
            }

            UpdateVisibility(elapsedTime, forceVisible: false);
            ShieldRechargeTimer += elapsedTime;

            if (TetheredTo != null)
            {
                Position = TetheredTo.Center + TetherOffset;
                Center   = TetheredTo.Center + TetherOffset;
                VelocityMaximum = 0;
            }
            if (Mothership != null && !Mothership.Active) //Problematic for drones...
                Mothership = null;

            SetFleetCapableStatus();

            if (!dying) UpdateAlive(elapsedTime);
            else        UpdateDying(elapsedTime);
        }

        void UpdateAlive(float elapsedTime)
        {
            ExploreCurrentSystem(elapsedTime);

            if (EMPdisabled)
            {
                float third = Radius / 3f;
                for (int i = 5 - 1; i >= 0; --i)
                {
                    Vector3 randPos = UniverseRandom.Vector32D(third);
                    Empire.Universe.lightning.AddParticleThreadA(Center.ToVec3() + randPos, Vector3.Zero);
                }
            }

            if (elapsedTime > 0f)
            {
                Projectiles.Update(elapsedTime);
                Beams.Update(elapsedTime);
                if (!EMPdisabled && Active)
                    AI.Update(elapsedTime);
            }

            if (!Active)
                return;

            InCombatTimer -= elapsedTime;
            if (InCombatTimer > 0.0f)
            {
                InCombat = true;
            }
            else
            {
                InCombat = false;
                if (AI.State == AIState.Combat && loyalty != EmpireManager.Player)
                {
                    AI.ClearOrders();
                }
            }

            if (elapsedTime > 0f)
            {
                UpdateShipStatus(elapsedTime);
                UpdateEnginesAndVelocity(elapsedTime);
            }

            if (InFrustum)
            {
                if (ShipSO != null)
                {
                    ShipSO.World = Matrix.CreateRotationY(yRotation)
                                 * Matrix.CreateRotationZ(Rotation)
                                 * Matrix.CreateTranslation(new Vector3(Center, 0.0f));
                    ShipSO.UpdateAnimation(ScreenManager.CurrentScreen.FrameDeltaTime);
                    UpdateThrusters();
                }
                else // auto-create scene objects if possible
                {
                    Empire.Universe?.QueueSceneObjectCreation(this);
                }
            }

            SoundEmitter.Position = new Vector3(Center, 0);

            ResetFrameThrustState();
        }

        void ExploreCurrentSystem(float elapsedTime)
        {
            if (System != null && elapsedTime > 0f && loyalty?.isFaction == false
                && !System.IsFullyExploredBy(loyalty)
                && System.PlanetList != null) // Added easy out for fully explored systems
            {
                foreach (Planet p in System.PlanetList)
                {
                    if (p.IsExploredBy(loyalty)) // already explored
                        continue;
                    if (p.Center.OutsideRadius(Center, 3000f))
                        continue;

                    if (loyalty == EmpireManager.Player)
                    {
                        for (int index = 0; index < p.BuildingList.Count; index++)
                        {
                            Building building = p.BuildingList[index];
                            if (building.EventHere)
                                Empire.Universe.NotificationManager.AddFoundSomethingInteresting(p);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < p.BuildingList.Count; i++)
                        {
                            Building building = p.BuildingList[i];
                            if (building.EventHere && loyalty != EmpireManager.Player && p.Owner == null)
                                loyalty.GetEmpireAI().SendExplorationFleet(p);
                        }
                    }

                    p.SetExploredBy(loyalty);
                    System.UpdateFullyExploredBy(loyalty);
                }
            }
        }

        void UpdateThrusters()
        {
            Color thrust0 = loyalty.ThrustColor0;
            Color thrust1 = loyalty.ThrustColor1;
            float velocityPercent = Velocity.Length() / VelocityMaximum;
            foreach (Thruster thruster in ThrusterList)
            {
                thruster.UpdatePosition();
                if (ThrustThisFrame != Ships.Thrust.Coast)
                {
                    if (engineState == MoveState.Warp)
                    {
                        if (thruster.heat < velocityPercent)
                            thruster.heat += 0.06f;
                        thruster.Update(Direction3D, thruster.heat, 0.004f, Empire.Universe.CamPos, thrust0, thrust1);
                    }
                    else
                    {
                        if (thruster.heat < velocityPercent)
                            thruster.heat += 0.06f;
                        if (thruster.heat > 0.600000023841858)
                            thruster.heat = 0.6f;
                        thruster.Update(Direction3D, thruster.heat, 0.002f, Empire.Universe.CamPos, thrust0, thrust1);
                    }
                }
                else
                {
                    thruster.heat = 0.01f;
                    thruster.Update(Direction3D, 0.1f, 1.0f / 500.0f, Empire.Universe.CamPos, thrust0, thrust1);
                }
            }
        }

        AudioHandle DeathSfx;

        void UpdateDying(float elapsedTime)
        {
            ThrusterList.Clear();
            dietimer -= elapsedTime;
            if (dietimer <= 1.9f && InFrustum && (DeathSfx == null || DeathSfx.IsStopped))
            {
                string cueName;
                if (SurfaceArea < 80) cueName = "sd_explosion_ship_warpdet_small";
                else if (SurfaceArea < 250) cueName = "sd_explosion_ship_warpdet_medium";
                else cueName = "sd_explosion_ship_warpdet_large";

                if (DeathSfx == null)
                    DeathSfx = new AudioHandle();
                DeathSfx.PlaySfxAsync(cueName, SoundEmitter);
            }
            if (dietimer <= 0.0f)
            {
                reallyDie = true;
                Die(LastDamagedBy, true);
                return;
            }

            if (ShipSO == null)
                return;

            // for a cool death effect, make the ship accelerate out of control:
            ApplyThrust(200f, Ships.Thrust.Forward);
            UpdateVelocityAndPosition(elapsedTime);

            int num1 = UniverseRandom.IntBetween(0, 60);
            if (num1 >= 57 && InFrustum)
            {
                Vector3 position = UniverseRandom.Vector3D(0f, Radius);
                ExplosionManager.AddExplosion(position, Velocity, ShipSO.WorldBoundingSphere.Radius, 2.5f, ExplosionType.Ship);
                Empire.Universe.flash.AddParticleThreadA(position, Vector3.Zero);
            }
            if (num1 >= 40)
            {
                Vector3 position = UniverseRandom.Vector3D(0f, Radius);
                Empire.Universe.sparks.AddParticleThreadA(position, Vector3.Zero);
            }
            yRotation += DieRotation.X * elapsedTime;
            xRotation += DieRotation.Y * elapsedTime;
            Rotation  += DieRotation.Z * elapsedTime;
            Rotation = Rotation.AsNormalizedRadians(); // [0; +2PI]

            if (inSensorRange && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.ShipView)
            {
                ShipSO.World = Matrix.CreateRotationY(yRotation)
                             * Matrix.CreateRotationX(xRotation)
                             * Matrix.CreateRotationZ(Rotation)
                             * Matrix.CreateTranslation(new Vector3(Center, 0.0f));
                ShipSO.UpdateAnimation(ScreenManager.CurrentScreen.FrameDeltaTime);
            }

            Projectiles.Update(elapsedTime);

            SoundEmitter.Position = new Vector3(Center, 0);

            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ModuleSlotList[i].UpdateWhileDying(elapsedTime);
            }
        }

        void CheckAndPowerConduit(ShipModule module)
        {
            if (!module.Active)
                return;
            module.Powered = true;
            module.CheckedConduits = true;
            Vector2 center = module.LocalCenter;
            for (int x = 0; x < ModuleSlotList.Length; x++)
            {
                ShipModule slot = ModuleSlotList[x];
                if (slot == module || slot.ModuleType != ShipModuleType.PowerConduit || slot.CheckedConduits)
                    continue;
                var distanceX = (int) Math.Abs(center.X - slot.LocalCenter.X) ;
                var distanceY = (int) Math.Abs(center.Y - slot.LocalCenter.Y) ;
                if (distanceX + distanceY > 16)
                {
                    if (distanceX + distanceY > 33)
                        continue;
                    if (distanceX + distanceY < 33)
                        continue;
                }

                CheckAndPowerConduit(slot);
            }
        }

        public void RecalculatePower()
        {
            ShouldRecalculatePower = false;

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule slot      = ModuleSlotList[i];
                slot.Powered         = false;
                slot.CheckedConduits = false;
            }

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                //better fix for modules that dont use power.
                if (module.PowerRadius < 1 && (module.PowerDraw <= 0 || module.AlwaysPowered))
                {
                    module.Powered = true;
                    continue;
                }
                //Filter by powerplants.
                if (!module.Is(ShipModuleType.PowerPlant) || !module.Active) continue;
                //This is a change. powerplants are now marked powered
                module.Powered = true;
                Vector2 moduleCenter = module.LocalCenter;
                //conduit check.
                foreach (ShipModule slot2 in ModuleSlotList)
                {
                    if (slot2.ModuleType != ShipModuleType.PowerConduit || slot2.Powered)
                        continue;

                    if (!IsAnyPartOfModuleInRadius(module, slot2.LocalCenter, 16)) continue;
                    CheckAndPowerConduit(slot2);
                }
            }
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (!module.Active || module.PowerRadius < 1 || !module.Powered )
                    continue;

                float cx = module.LocalCenter.X;
                float cy = module.LocalCenter.Y;
                int powerRadius = module.PowerRadius * 16 + (int)module.Radius;

                foreach (ShipModule slot2 in ModuleSlotList)
                {
                    if (!slot2.Active || slot2.Powered  || slot2 == module || slot2.ModuleType == ShipModuleType.PowerConduit)
                        continue;

                    int distanceFromPowerX = (int)Math.Abs(cx - (slot2.Position.X + 8)) ;
                    int distanceFromPowerY = (int)Math.Abs(cy - (slot2.Position.Y + 8));
                    if (distanceFromPowerX + distanceFromPowerY <= powerRadius)
                    {
                        slot2.Powered = true;
                        continue;
                    }
                    //if its really far away dont bother.
                    if (distanceFromPowerX + distanceFromPowerY > slot2.Radius * 2 + powerRadius)
                        continue;
                    slot2.Powered = IsAnyPartOfModuleInRadius(slot2, new Vector2(cx, cy), powerRadius);
                }
            }
        }
        //not sure where to put this. I guess shipModule but its huge. Maybe an extension?
        private static bool IsAnyPartOfModuleInRadius(ShipModule moduleAreaToCheck, Vector2 pos, int radius)
        {
            float cx = pos.X;
            float cy = pos.Y;

            for (int y = 0; y < moduleAreaToCheck.YSIZE; ++y)
            {
                float sy = moduleAreaToCheck.Position.Y + (y * 16) +8;
                for (int x = 0; x < moduleAreaToCheck.XSIZE; ++x)
                {
                    if (y == moduleAreaToCheck.YSIZE * 16 && x == moduleAreaToCheck.XSIZE *16) continue;
                        float sx = moduleAreaToCheck.Position.X + (x * 16) +8;
                    if ((int) Math.Abs(cx - sx) + (int) Math.Abs(cy - sy) <= radius + 8)
                        return true;
                }
            }
            return false;
        }
    }
}