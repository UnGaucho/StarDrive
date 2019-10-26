﻿
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class FighterScrollList : ScrollList<FighterListItem>
    {
        readonly GameScreen Screen;
        public ShipModule ActiveHangarModule;
        public ShipModule ActiveModule;
        public string HangarShipUIDLast = "";

        public FighterScrollList(Submenu fighterList, GameScreen shipDesignScreen) : base(fighterList, 40)
        {
            Screen = shipDesignScreen;
            AutoManageItems = true;
        }

        public bool HitTest(InputState input)
        {
            return ParentMenu.HitTest(input.CursorPosition) && ActiveModule != null;
        }

        void Populate()
        {
            Reset();
            AddShip(ResourceManager.GetShipTemplate(DynamicHangarOptions.DynamicLaunch.ToString()));
            AddShip(ResourceManager.GetShipTemplate(DynamicHangarOptions.DynamicInterceptor.ToString()));
            AddShip(ResourceManager.GetShipTemplate(DynamicHangarOptions.DynamicAntiShip.ToString()));
            foreach (string shipId in EmpireManager.Player.ShipsWeCanBuild)
            {
                if (!ResourceManager.GetShipTemplate(shipId, out Ship hangarShip))
                    continue;
                string role = ShipData.GetRole(hangarShip.shipData.HullRole);
                if (!ActiveModule.PermittedHangarRoles.Contains(role))
                    continue;
                if (hangarShip.SurfaceArea > ActiveModule.MaximumHangarShipSize)
                    continue;
                AddShip(ResourceManager.ShipsDict[shipId]);
            }
        }
        
        void AddShip(Ship ship)
        {
            AddItem(new FighterListItem(ship)).OnClick = OnItemClicked;
        }

        void OnItemClicked(FighterListItem item)
        {
            ActiveModule.hangarShipUID = item.Ship.Name;
            HangarShipUIDLast = item.Ship.Name;
        }

        public bool HandleInput(InputState input, ShipModule activeModule, ShipModule highlightedModule)
        {
            activeModule = activeModule ?? highlightedModule;
            if (activeModule?.ModuleType == ShipModuleType.Hangar &&
                !activeModule.IsTroopBay && !activeModule.IsSupplyBay)
            {
                ActiveModule = activeModule;
                SetActiveHangarModule(activeModule, ActiveHangarModule);
                
                // handle everything related to scroll list
                if (ParentMenu.HitTest(Screen.Input.CursorPosition))
                {
                    base.HandleInput(input);
                }
                else // we're not hovering the scroll list, just highlight the active ship
                {
                    foreach (FighterListItem e in VisibleExpandedEntries)
                    {
                        if (ActiveModule.hangarShipUID == e.Ship.Name)
                        {
                            SelectionBox = new Selector(e.Rect);
                            break;
                        }
                    }
                }
                return true;
            }
            
            base.HandleInput(input);
            ActiveHangarModule = null;
            ActiveModule = null;
            HangarShipUIDLast = "";
            return false;
        }

        bool HangarNotSelected(ShipModule activeModule, ShipModule activeHangarModule)
            => activeHangarModule == activeModule || activeModule.ModuleType != ShipModuleType.Hangar;

        public void SetActiveHangarModule(ShipModule activeModule, ShipModule activeHangarModule)
        {
            if (HangarNotSelected(activeModule, activeHangarModule))
                return;

            ActiveHangarModule = activeModule;
            Populate();

            Ship fighter = ResourceManager.GetShipTemplate(HangarShipUIDLast, false);
            if (HangarShipUIDLast != "" && activeModule.PermittedHangarRoles.Contains(fighter?.shipData.GetRole()) && activeModule.MaximumHangarShipSize >= fighter?.SurfaceArea)
            {
                activeModule.hangarShipUID = HangarShipUIDLast;
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            if (ActiveHangarModule == null)
                return;

            Screen.DrawRectangle(ParentMenu.Rect, Color.TransparentWhite, Color.Black);
            ParentMenu.Draw(batch);
            base.Draw(batch);
        }
    }
}
