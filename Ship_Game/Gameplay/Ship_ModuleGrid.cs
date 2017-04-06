﻿using System;
using Microsoft.Xna.Framework;

namespace Ship_Game.Gameplay
{
    public sealed partial class Ship
    {
        public ShipModule[] ModuleSlotList;
        private ShipModule[] SparseModuleGrid;   // single dimensional grid, for performance reasons
        private ShipModule[] ExternalModuleGrid; // only contains external modules
        public int NumExternalSlots { get; private set; }
        private int GridWidth;
        private int GridHeight;
        private Vector2 GridOrigin;

        private void CreateModuleGrid()
        {
            float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                Vector2 topLeft = module.Position;
                var botRight = new Vector2(topLeft.X + module.XSIZE * 16.0f,
                                           topLeft.Y + module.YSIZE * 16.0f);
                if (topLeft.X  < minX) minX = topLeft.X;
                if (topLeft.Y  < minY) minY = topLeft.Y;
                if (botRight.X > maxX) maxX = botRight.X;
                if (botRight.Y > maxY) maxY = botRight.Y;
            }

            GridOrigin = new Vector2(minX, minY);
            GridWidth  = (int)(maxX - minX) / 16;
            GridHeight = (int)(maxY - minY) / 16;
            SparseModuleGrid   = new ShipModule[GridWidth * GridHeight];
            ExternalModuleGrid = new ShipModule[GridWidth * GridHeight];

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                UpdateGridSlot(SparseModuleGrid, ModuleSlotList[i], becameActive: true);
            }
        }

        private void AddExternalModule(ShipModule module, int x, int y, int quadrant)
        {
            if (module.isExternal) return;
            ++NumExternalSlots;
            module.isExternal = true;
            module.quadrant   = quadrant;
            UpdateGridSlot(ExternalModuleGrid, x, y, module, becameActive: true);
        }

        private void RemoveExternalModule(ShipModule module, int x, int y)
        {
            if (!module.isExternal) return;
            --NumExternalSlots;
            module.isExternal = false;
            module.quadrant   = 0;
            UpdateGridSlot(ExternalModuleGrid, x, y, module, becameActive: false);
        }

        private bool IsModuleInactiveAt(int x, int y)
        {
            int idx = x + y * GridWidth;
            ShipModule module = (uint)idx < SparseModuleGrid.Length ? SparseModuleGrid[idx] : null;
            return module == null || !module.Active;
        }

        private int GetQuadrantEstimate(int x, int y)
        {
            Vector2 dir = new Vector2(x - (GridWidth / 2), y - (GridHeight / 2)).Normalized();
            if (dir.X <= 0f && dir.Y <= 0f)
                return 0;
            if (Math.Abs(dir.Y) <= 0.5f)
                return dir.X <= 0f ? 1 /*top*/ : 3 /*bottom*/;
            return dir.X <= 0f ? 4 /*left*/ : 2 /*right*/;
        }

        private bool CheckIfShouldBeExternal(int x, int y)
        {
            if (!GetModuleAt(SparseModuleGrid, x, y, out ShipModule module))
                return false;

            SlotPointAt(module.Position, out x, out y); // now get the true topleft root coordinates of module
            bool shouldBeExternal = module.Active &&
                                    IsModuleInactiveAt(x, y - 1) ||
                                    IsModuleInactiveAt(x - 1, y) ||
                                    IsModuleInactiveAt(x + module.XSIZE, y) ||
                                    IsModuleInactiveAt(x, y + module.YSIZE);
            if (shouldBeExternal)
            {
                if (!module.isExternal)
                    AddExternalModule(module, x, y, GetQuadrantEstimate(x, y));
                return true;
            }
            if (module.isExternal)
                RemoveExternalModule(module, x, y);
            return false;
        }

        private void InitExternalSlots()
        {
            NumExternalSlots = 0;
            for (int y = 0; y < GridHeight; ++y)
            {
                for (int x = 0; x < GridWidth; ++x)
                {
                    int idx = x + y*GridWidth;
                    if (ExternalModuleGrid[idx] != null || !CheckIfShouldBeExternal(x, y))
                        continue;
                    // ReSharper disable once PossibleNullReferenceException
                    x += ExternalModuleGrid[idx].XSIZE - 1; // skip slots that span this module
                }
            }
        }

        // updates the isExternal status of a module, depending on whether it died or resurrected
        public void UpdateExternalSlots(ShipModule module, bool becameActive)
        {
            SlotPointAt(module.Position, out int x, out int y);

            if (becameActive) // we resurrected, so add us to external module grid and update all surrounding slots
                AddExternalModule(module, x, y, GetQuadrantEstimate(x, y));
            else // we turned inactive, so clear self from external module grid and 
                RemoveExternalModule(module, x, y);

            CheckIfShouldBeExternal(x, y - 1);
            CheckIfShouldBeExternal(x - 1, y);
            CheckIfShouldBeExternal(x + module.XSIZE, y);
            CheckIfShouldBeExternal(x, y + module.YSIZE);
        }

        private void UpdateGridSlot(ShipModule[] sparseGrid, int gridX, int gridY, ShipModule module, bool becameActive)
        {
            int endX = gridX + module.XSIZE, endY = gridY + module.YSIZE;
            for (int y = gridY; y < endY; ++y)
                for (int x = gridX; x < endX; ++x)
                    sparseGrid[x + y * GridWidth] = becameActive ? module : null;
        }
        private void UpdateGridSlot(ShipModule[] sparseGrid, ShipModule module, bool becameActive)
        {
            SlotPointAt(module.Position, out int x, out int y);
            UpdateGridSlot(sparseGrid, x, y, module, becameActive);
        }

        private void SlotPointAt(Vector2 pos, out int x, out int y)
        {
            Vector2 offset = pos - GridOrigin;
            x = (int)(offset.X / 16.0f);
            y = (int)(offset.Y / 16.0f);
        }

        public bool TryGetModule(Vector2 pos, out ShipModule module)
        {
            SlotPointAt(pos, out int x, out int y);
            return GetModuleAt(SparseModuleGrid, x, y, out module);
        }

        // safe and fast module lookup by x,y where coordinates (0,1) (2,1) etc
        private bool GetModuleAt(ShipModule[] sparseGrid, int x, int y, out ShipModule module)
        {
            module = (uint)x < GridWidth && (uint)y < GridHeight ? sparseGrid[x + y * GridWidth] : null;
            return module != null;
        }

        // @note Only Active (alive) modules are in ExternalSlots. This is because ExternalSlots get
        //       updated every time a module dies. The code for that is in ShipModule.cs
        // @note This method is optimized for fast instant lookup, with a semi-optimal fallback floodfill search
        public ShipModule FindClosestExternalModule(Vector2 point)
        {
            if (NumExternalSlots == 0)
                return null;
            return RadialSearch(point, 0f, false, ExternalModuleGrid, GridWidth, GridHeight);
        }

        public ShipModule HitTestExternalModules(Vector2 hitPos, float hitRadius, bool ignoreShields = false)
        {
            if (NumExternalSlots == 0)
                return null;
            return RadialSearch(hitPos, hitRadius, ignoreShields, ExternalModuleGrid, GridWidth, GridHeight);
        }

        public ShipModule FindUnshieldedExternalModule(int quadrant)
        {
            for (int i = 0; i < ExternalModuleGrid.Length; ++i)
            {
                ShipModule module = ExternalModuleGrid[i];
                if (module != null && module.quadrant == quadrant && module.Health > 0f && module.ShieldPower <= 0f)
                    return module;
            }
            return null; // aargghh ;(
        }

        // Generic shipmodule grid search with an optional predicate filter
        private ShipModule RadialSearch(Vector2 worldPos, float radius, bool ignoreShields, ShipModule[] grid, int width, int height)
        {
            Vector2 center = worldPos - GridOrigin;
            int firstX = (int)((center.X - radius) / 16.0f);
            int lastX  = (int)((center.X + radius) / 16.0f);
            int firstY = (int)((center.Y - radius) / 16.0f);
            int lastY  = (int)((center.Y + radius) / 16.0f);

            // Luckily .NET can successfully optimize this into something faster than Math.Min/Max on 32-bit CLR
            if (firstX < 0) firstX = 0; else if (firstX >= width)  firstX = width - 1;
            if (lastX  < 0) lastX  = 0; else if (lastX  >= width)  lastX  = width - 1;
            if (firstY < 0) firstY = 0; else if (firstY >= height) firstY = height - 1;
            if (lastY  < 0) lastY  = 0; else if (lastY  >= height) lastY  = height - 1;

            int minX = (firstX + lastX) / 2;
            int minY = (firstY + lastY) / 2;
            int maxX = minX;
            int maxY = minY;
            for (;;)
            {
                ShipModule m;
                bool didExpand = false;
                if (minX > firstX) // test all modules to the left
                {
                    --minX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[minX + y * width]) != null && m.Active
                            && (radius <= 0f || m.HitTest(worldPos, radius, ignoreShields))) return m;
                }
                if (maxX < lastX) // test all modules to the right
                {
                    ++maxX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[maxX + y * width]) != null && m.Active
                            && (radius <= 0f || m.HitTest(worldPos, radius, ignoreShields))) return m;
                }
                if (minY > firstY) // test all top modules
                {
                    --minY; didExpand = true;
                    int rowstart = minY * width;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[rowstart + x]) != null && m.Active
                            && (radius <= 0f || m.HitTest(worldPos, radius, ignoreShields))) return m;
                }
                if (maxY < lastY) // test all bottom modules
                {
                    ++maxY; didExpand = true;
                    int rowstart = maxY * width;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[rowstart + x]) != null && m.Active
                            && (radius <= 0f || m.HitTest(worldPos, radius, ignoreShields))) return m;
                }
                if (!didExpand) return null; // aargh, looks like we didn't find any!
            }
        }


        public ShipModule RayHitTestExternalModules(
            Vector2 startPos, Vector2 direction, float distance, float rayRadius, bool ignoreShields = false)
        {
            return RayHitTestExternalModules(startPos, startPos + direction * distance, rayRadius, ignoreShields);
        }

        // @todo This needs optimization
        public ShipModule RayHitTestExternalModules(Vector2 startPos, Vector2 endPos, float rayRadius, bool ignoreShields = false)
        {
            for (int i = 0; i < ExternalModuleGrid.Length; ++i)
            {
                ShipModule module = ExternalModuleGrid[i];
                if (module == null || !module.Active || module.Health <= 0f)
                    continue;

                ++GlobalStats.DistanceCheckTotal;

                Vector2 point = module.Center.FindClosestPointOnLine(startPos, endPos);
                if (module.HitTest(point, rayRadius, ignoreShields))
                    return module;
            }
            return null;
        }

        // find ShipModules that collide with a this wide RAY
        // direction must be normalized!!
        // results are sorted by distance
        public Array<ShipModule> RayHitTestModules(
            Vector2 startPos, Vector2 direction, float distance, float rayRadius, bool ignoreShields = false)
        {
            Vector2 endPos = startPos + direction * distance;

            var modules = new Array<ShipModule>();
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (!module.Active || module.Health <= 0f)
                    continue;

                ++GlobalStats.DistanceCheckTotal;

                Vector2 point = module.Position.FindClosestPointOnLine(startPos, endPos);
                if (module.HitTest(point, rayRadius, ignoreShields))
                    modules.Add(module);
            }
            modules.Sort(module => startPos.SqDist(module.Position));
            return modules;
        }

        // find ShipModules that fall into hit radius (eg an explosion)
        // results are sorted by distance
        public Array<ShipModule> HitTestModules(Vector2 hitPos, float hitRadius, bool ignoreShields = false)
        {
            var modules = new Array<ShipModule>();
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (!module.Active || module.Health <= 0f)
                    continue;

                ++GlobalStats.DistanceCheckTotal;

                if (module.HitTest(hitPos, hitRadius, ignoreShields))
                    modules.Add(module);
            }
            modules.Sort(module => hitPos.SqDist(module.Position));
            return modules;
        }
        
        private ShipModule ClosestExternalModuleSlot(Vector2 center, float maxRange=999999f)
        {
            float nearest = maxRange*maxRange;
            ShipModule closestModule = null;
            for (int i = 0; i < ExternalModuleGrid.Length; ++i)
            {
                ShipModule slot = ExternalModuleGrid[i];
                if (slot == null || !slot.Active || slot.quadrant < 1 || slot.Health <= 0f)
                    continue;

                float sqDist = center.SqDist(slot.Center);
                if (sqDist >= nearest && closestModule != null)
                    continue;
                nearest       = sqDist;
                closestModule = slot;
            }
            return closestModule;
        }

        private Array<ShipModule> FilterSlotsInDamageRange(ShipModule[] slots, ShipModule closestExtSlot)
        {
            Vector2 extSlotCenter = closestExtSlot.Center;
            int quadrant          = closestExtSlot.quadrant;
            float sqDamageRadius  = Center.SqDist(extSlotCenter);

            var filtered = new Array<ShipModule>();
            for (int i = 0; i < slots.Length; ++i)
            {
                ShipModule module = slots[i];
                if (!module.Active || module.Health <= 0f || 
                    (module.quadrant != quadrant && module.isExternal))
                    continue;
                if (module.Center.SqDist(extSlotCenter) < sqDamageRadius)
                    filtered.Add(module);
            }
            return filtered;
        }

        // Refactor by RedFox: Picks a random internal module to target and updates targetting list if needed
        private ShipModule TargetRandomInternalModule(ref Array<ShipModule> inAttackerTargetting, 
                                                      Vector2 center, int level, float weaponRange=999999f)
        {
            ShipModule closestExtSlot = ClosestExternalModuleSlot(center, weaponRange);

            if (closestExtSlot == null) // ship might be destroyed, no point in targeting it
                return null;

            if (inAttackerTargetting == null || !inAttackerTargetting.Contains(closestExtSlot))
            {
                inAttackerTargetting = FilterSlotsInDamageRange(ModuleSlotList, closestExtSlot);
                if (level > 1)
                {
                    // Sort Descending, so first element is the module with greatest TargettingValue
                    inAttackerTargetting.Sort((sa, sb) => sb.ModuleTargettingValue 
                                                        - sa.ModuleTargettingValue);
                }
            }

            if (inAttackerTargetting.Count == 0)
                return null;
            // higher levels lower the limit, which causes a better random pick
            int limit = inAttackerTargetting.Count / (level + 1);
            return inAttackerTargetting[RandomMath.InRange(limit)];
        }

        public ShipModule GetRandomInternalModule(Weapon source)
        {
            float searchRange = source.Range + 100;
            Vector2 center    = source.Owner?.Center ?? source.Center;
            int level         = source.Owner?.Level  ?? 0;
            return TargetRandomInternalModule(ref source.AttackerTargetting, center, level, searchRange);
        }

        public ShipModule GetRandomInternalModule(Projectile source)
        {
            Vector2 center = source.Owner?.Center ?? source.Center;
            int level      = source.Owner?.Level  ?? 0;
            return TargetRandomInternalModule(ref source.Weapon.AttackerTargetting, center, level);
        }
    }
}
