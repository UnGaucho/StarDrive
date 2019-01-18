using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Ship_Game
{
    public partial class ColonyScreen : PlanetScreen, IListScreen
    {
        public Planet P;
        private ToggleButton PlayerDesignsToggle;

        private Menu2 TitleBar;
        private Vector2 TitlePos;
        private Menu1 LeftMenu;
        private Menu1 RightMenu;
        private Submenu PlanetInfo;
        private Submenu pDescription;
        private Submenu pLabor;
        private Submenu pStorage;
        private Submenu pFacilities;
        private Submenu build;
        private Submenu queue;
        private UICheckBox GovSliders;
        private UICheckBox GovBuildings;
        private UITextEntry PlanetName = new UITextEntry();
        private Rectangle PlanetIcon;
        private EmpireUIOverlay eui;
        private float ClickTimer;
        private float TimerDelay = 0.25f;
        private ToggleButton LeftColony;
        private ToggleButton RightColony;
        private UIButton launchTroops;
        private UIButton SendTroops;  //fbedard
        private DropOptions<int> GovernorDropdown;
        public CloseButton close;
        private Rectangle MoneyRect;
        private Array<ThreeStateButton> ResourceButtons = new Array<ThreeStateButton>();
        private ScrollList CommoditiesSL;
        private Rectangle GridPos;
        private Submenu subColonyGrid;
        private ScrollList buildSL;
        private ScrollList QSL;
        private DropDownMenu foodDropDown;
        private DropDownMenu prodDropDown;
        private ProgressBar FoodStorage;
        private ProgressBar ProdStorage;
        private Rectangle FoodStorageIcon;
        private Rectangle ProfStorageIcon;

        ColonySliderGroup Sliders;

        private object DetailInfo;
        private Building ToScrap;
        private ScrollList.Entry ActiveBuildingEntry;

        public bool ClickedTroop;
        bool Reset;
        int EditHoverState;

        private Selector Selector;
        private Rectangle EditNameButton;
        private static bool Popup;  //fbedard
        private readonly SpriteFont Font8 = Fonts.Arial8Bold;
        private readonly SpriteFont Font12 = Fonts.Arial12Bold;
        private readonly SpriteFont Font20 = Fonts.Arial20Bold;

        public ColonyScreen(GameScreen parent, Planet p, EmpireUIOverlay empUI) : base(parent)
        {
            P = p;
            empUI.empire.UpdateShipsWeCanBuild();
            eui = empUI;
            var theMenu1 = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            TitleBar = new Menu2(theMenu1);
            LeftColony = new ToggleButton(new Vector2(theMenu1.X + 25, theMenu1.Y + 24), ToggleButtonStyle.ArrowLeft);
            RightColony = new ToggleButton(new Vector2(theMenu1.X + theMenu1.Width - 39, theMenu1.Y + 24), ToggleButtonStyle.ArrowRight);
            TitlePos = new Vector2(theMenu1.X + theMenu1.Width / 2 - Fonts.Laserian14.MeasureString("Colony Overview").X / 2f, theMenu1.Y + theMenu1.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            var theMenu2 = new Rectangle(2, theMenu1.Y + theMenu1.Height + 5, theMenu1.Width, ScreenHeight - (theMenu1.Y + theMenu1.Height) - 7);
            LeftMenu = new Menu1(theMenu2);
            var theMenu3 = new Rectangle(theMenu1.X + theMenu1.Width + 10, theMenu1.Y, ScreenWidth / 3 - 15, ScreenHeight - theMenu1.Y - 2);
            RightMenu = new Menu1(theMenu3);
            var iconMoney = ResourceManager.Texture("NewUI/icon_money");
            MoneyRect = new Rectangle(theMenu2.X + theMenu2.Width - 75, theMenu2.Y + 20, iconMoney.Width, iconMoney.Height);
            close = new CloseButton(this, new Rectangle(theMenu3.X + theMenu3.Width - 52, theMenu3.Y + 22, 20, 20));
            var theMenu4 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            PlanetInfo = new Submenu(theMenu4);
            PlanetInfo.AddTab(Localizer.Token(326));
            var theMenu5 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pDescription = new Submenu(theMenu5);

            var laborPanel = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height + theMenu5.Height + 20, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pLabor = new Submenu(laborPanel);
            pLabor.AddTab(Localizer.Token(327));

            CreateSliders(laborPanel);

            var theMenu7 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height + theMenu5.Height + laborPanel.Height + 40, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pStorage = new Submenu(theMenu7);
            pStorage.AddTab(Localizer.Token(328));

            if (GlobalStats.HardcoreRuleset)
            {
                int num2 = (theMenu7.Width - 40) / 4;
                ResourceButtons.Add(new ThreeStateButton(p.FS, "Food", new Vector2(theMenu7.X + 20, theMenu7.Y + 30)));
                ResourceButtons.Add(new ThreeStateButton(p.PS, "Production", new Vector2(theMenu7.X + 20 + num2, theMenu7.Y + 30)));
                ResourceButtons.Add(new ThreeStateButton(Planet.GoodState.EXPORT, "Fissionables", new Vector2(theMenu7.X + 20 + num2 * 2, theMenu7.Y + 30)));
                ResourceButtons.Add(new ThreeStateButton(Planet.GoodState.EXPORT, "ReactorFuel", new Vector2(theMenu7.X + 20 + num2 * 3, theMenu7.Y + 30)));
            }
            else
            {
                FoodStorage = new ProgressBar(new Rectangle(theMenu7.X + 100, theMenu7.Y + 25 + (int)(0.330000013113022 * (theMenu7.Height - 25)), (int)(0.400000005960464 * theMenu7.Width), 18));
                FoodStorage.Max = p.Storage.Max;
                FoodStorage.Progress = p.FoodHere;
                FoodStorage.color = "green";
                foodDropDown = new DropDownMenu(new Rectangle(theMenu7.X + 100 + (int)(0.400000005960464 * theMenu7.Width) + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * theMenu7.Width), 18));
                foodDropDown.AddOption(Localizer.Token(329));
                foodDropDown.AddOption(Localizer.Token(330));
                foodDropDown.AddOption(Localizer.Token(331));
                foodDropDown.ActiveIndex = (int)p.FS;
                var iconStorageFood = ResourceManager.Texture("NewUI/icon_storage_food");
                FoodStorageIcon = new Rectangle(theMenu7.X + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageFood.Width, iconStorageFood.Height);
                ProdStorage = new ProgressBar(new Rectangle(theMenu7.X + 100, theMenu7.Y + 25 + (int)(0.660000026226044 * (theMenu7.Height - 25)), (int)(0.400000005960464 * theMenu7.Width), 18));
                ProdStorage.Max = p.Storage.Max;
                ProdStorage.Progress = p.ProdHere;
                var iconStorageProd = ResourceManager.Texture("NewUI/icon_storage_production");
                ProfStorageIcon = new Rectangle(theMenu7.X + 20, ProdStorage.pBar.Y + ProdStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageProd.Width, iconStorageFood.Height);
                prodDropDown = new DropDownMenu(new Rectangle(theMenu7.X + 100 + (int)(0.400000005960464 * theMenu7.Width) + 20, ProdStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * theMenu7.Width), 18));
                prodDropDown.AddOption(Localizer.Token(329));
                prodDropDown.AddOption(Localizer.Token(330));
                prodDropDown.AddOption(Localizer.Token(331));
                prodDropDown.ActiveIndex = (int)p.PS;
            }
            var theMenu8 = new Rectangle(theMenu2.X + 20 + theMenu4.Width + 20, theMenu4.Y, theMenu2.Width - 60 - theMenu4.Width, (int)(theMenu2.Height * 0.5));
            subColonyGrid = new Submenu(theMenu8);
            subColonyGrid.AddTab(Localizer.Token(332));
            var theMenu9 = new Rectangle(theMenu2.X + 20 + theMenu4.Width + 20, theMenu8.Y + theMenu8.Height + 20, theMenu2.Width - 60 - theMenu4.Width, theMenu2.Height - 20 - theMenu8.Height - 40);
            pFacilities = new Submenu(theMenu9);
            pFacilities.AddTab(Localizer.Token(333));

            launchTroops = Button(theMenu9.X + theMenu9.Width - 175, theMenu9.Y - 5, "Launch Troops", OnLaunchTroopsClicked);
            SendTroops = Button(theMenu9.X + theMenu9.Width - launchTroops.Rect.Width - 185,
                                theMenu9.Y - 5, "Send Troops", OnSendTroopsClicked);

            CommoditiesSL = new ScrollList(pFacilities, 40);
            var theMenu10 = new Rectangle(theMenu3.X + 20, theMenu3.Y + 20, theMenu3.Width - 40, (int)(0.5 * (theMenu3.Height - 60)));
            build = new Submenu(theMenu10);
            build.AddTab(Localizer.Token(334));
            buildSL = new ScrollList(build);
            PlayerDesignsToggle = new ToggleButton(
                new Vector2(build.Menu.X + build.Menu.Width - 270, build.Menu.Y),
                ToggleButtonStyle.Grid, "SelectionBox/icon_grid");

            PlayerDesignsToggle.Active = GlobalStats.ShowAllDesigns;
            if (p.HasShipyard)
                build.AddTab(Localizer.Token(335));
            if (p.AllowInfantry)
                build.AddTab(Localizer.Token(336));
            var theMenu11 = new Rectangle(theMenu3.X + 20, theMenu3.Y + 20 + 20 + theMenu10.Height, theMenu3.Width - 40, theMenu3.Height - 40 - theMenu10.Height - 20 - 3);
            queue = new Submenu(theMenu11);
            queue.AddTab(Localizer.Token(337));

            QSL = new ScrollList(queue, ListOptions.Draggable);

            PlanetIcon = new Rectangle(theMenu4.X + theMenu4.Width - 148, theMenu4.Y + (theMenu4.Height - 25) / 2 - 64 + 25, 128, 128);
            GridPos = new Rectangle(subColonyGrid.Menu.X + 10, subColonyGrid.Menu.Y + 30, subColonyGrid.Menu.Width - 20, subColonyGrid.Menu.Height - 35);
            int width = GridPos.Width / 7;
            int height = GridPos.Height / 5;
            foreach (PlanetGridSquare planetGridSquare in p.TilesList)
                planetGridSquare.ClickRect = new Rectangle(GridPos.X + planetGridSquare.x * width, GridPos.Y + planetGridSquare.y * height, width, height);
            PlanetName.Text = p.Name;
            PlanetName.MaxCharacters = 12;
            if (p.Owner != null)
            {
                DetailInfo = p.Description;
                var rectangle4 = new Rectangle(pDescription.Menu.X + 10, pDescription.Menu.Y + 30, 124, 148);
                var rectangle5 = new Rectangle(rectangle4.X + rectangle4.Width + 20, rectangle4.Y + rectangle4.Height - 15, (int)Fonts.Pirulen16.MeasureString(Localizer.Token(370)).X, Fonts.Pirulen16.LineSpacing);
                GovernorDropdown = new DropOptions<int>(this, new Rectangle(rectangle5.X + 30, rectangle5.Y + 30, 100, 18));
                GovernorDropdown.AddOption("--", 1);
                GovernorDropdown.AddOption(Localizer.Token(4064), 0);
                GovernorDropdown.AddOption(Localizer.Token(4065), 2);
                GovernorDropdown.AddOption(Localizer.Token(4066), 4);
                GovernorDropdown.AddOption(Localizer.Token(4067), 3);
                GovernorDropdown.AddOption(Localizer.Token(4068), 5);
                GovernorDropdown.AddOption(Localizer.Token(5087), 6);
                GovernorDropdown.ActiveIndex = GetIndex(p);

                P.colonyType = (Planet.ColonyType)GovernorDropdown.ActiveValue;

                // @todo add localization
                GovBuildings = new UICheckBox(this, rectangle5.X - 10, rectangle5.Y - Font12.LineSpacing * 2 + 15, 
                                            () => p.GovBuildings, Font12, "Governor manages buildings", 0);

                GovSliders = new UICheckBox(this, rectangle5.X - 10, rectangle5.Y - Font12.LineSpacing + 10,
                                          () => p.GovSliders, Font12, "Governor manages labor sliders", 0);
            }
            else
            {
                Empire.Universe.LookingAtPlanet = false;
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            ClickTimer += (float)GameTime.ElapsedGameTime.TotalSeconds;
            if (P.Owner == null)
                return;
            P.UpdateIncomes(false);
            LeftMenu.Draw();
            RightMenu.Draw();
            TitleBar.Draw(batch);
            LeftColony.Draw(ScreenManager);
            RightColony.Draw(ScreenManager);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(369), TitlePos, new Color(255, 239, 208));
            if (!GlobalStats.HardcoreRuleset)
            {
                FoodStorage.Max = P.Storage.Max;
                FoodStorage.Progress = P.FoodHere;
                ProdStorage.Max = P.Storage.Max;
                ProdStorage.Progress = P.ProdHere;
            }
            PlanetInfo.Draw(batch);
            pDescription.Draw(batch);
            pLabor.Draw(batch);
            pStorage.Draw(batch);
            subColonyGrid.Draw(batch);
            var destinationRectangle1 = new Rectangle(GridPos.X, GridPos.Y + 1, GridPos.Width - 4, GridPos.Height - 3);
            batch.Draw(ResourceManager.Texture("PlanetTiles/" + P.PlanetTileId), destinationRectangle1, Color.White);
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (!pgs.Habitable)
                    batch.FillRectangle(pgs.ClickRect, new Color(0, 0, 0, 200));
                batch.DrawRectangle(pgs.ClickRect, new Color(211, 211, 211, 70), 2f);
                if (pgs.building != null)
                {
                    Rectangle destinationRectangle2 = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    if(pgs.building.IsPlayerAdded)
                    {
                        batch.Draw(ResourceManager.Texture("Buildings/icon_" + pgs.building.Icon + "_64x64"), destinationRectangle2, Color.WhiteSmoke);
                    }
                    else
                    
                    batch.Draw(ResourceManager.Texture("Buildings/icon_" + pgs.building.Icon + "_64x64"), destinationRectangle2, Color.White);
                }
                else if (pgs.QItem != null)
                {
                    Rectangle destinationRectangle2 = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    batch.Draw(ResourceManager.Texture("Buildings/icon_" + pgs.QItem.Building.Icon + "_64x64"), destinationRectangle2, new Color(255, 255, 255, 128));
                }
                DrawPGSIcons(pgs);
            }
            foreach (PlanetGridSquare planetGridSquare in P.TilesList)
            {
                if (planetGridSquare.highlighted)
                    batch.DrawRectangle(planetGridSquare.ClickRect, Color.White, 2f);
            }
            if (ActiveBuildingEntry != null)
            {
                MouseState state2 = Mouse.GetState();
                var r = new Rectangle(state2.X, state2.Y, 48, 48);
                var building = ActiveBuildingEntry.Get<Building>();
                batch.Draw(ResourceManager.Texture($"Buildings/icon_{building.Icon}_48x48"), r, Color.White);
            }
            pFacilities.Draw(batch);
            launchTroops.Visible = P.Owner == Empire.Universe.player && P.TroopsHere.Count > 0;

            //fbedard: Display button
            if (P.Owner == Empire.Universe.player)
            {
                int troopsInvading = eui.empire.GetShips()
                    .Where(troop => troop.TroopList.Count > 0)
                    .Where(ai => ai.AI.State != AIState.Resupply)
                    .Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == P));
                if (troopsInvading > 0)
                    SendTroops.Text = "Landing: " + troopsInvading;
                else
                    SendTroops.Text = "Send Troops";
            }
            DrawDetailInfo(new Vector2(pFacilities.Menu.X + 15, pFacilities.Menu.Y + 35));
            build.Draw(batch);
            queue.Draw(batch);

            if (build.Tabs[0].Selected)
            {
                DrawBuildingsWeCanBuild(batch);
            }
            else if (build.Tabs[1].Selected)
            {
                if      (P.HasShipyard)   DrawBuildableShipsList(batch);
                else if (P.AllowInfantry) DrawBuildTroopsList(batch);
            }
            else if (P.AllowInfantry && build.Tabs[2].Selected)
            {
                DrawBuildTroopsList(batch);
            }

            DrawConstructionQueue(batch);

            buildSL.Draw(batch);
            Selector?.Draw(batch);

            DrawSliders(batch);


            batch.Draw(P.PlanetTexture, PlanetIcon, Color.White);
            float num5 = 80f;
            if (GlobalStats.IsGermanOrPolish)
                num5 += 20f;
            Vector2 vector2_2 = new Vector2(PlanetInfo.Menu.X + 20, PlanetInfo.Menu.Y + 45);
            P.Name = PlanetName.Text;
            PlanetName.Draw(Font20, batch, vector2_2, GameTime, new Color(255, 239, 208));
            EditNameButton = new Rectangle((int)(vector2_2.X + (double)Font20.MeasureString(P.Name).X + 12.0), (int)(vector2_2.Y + (double)(Font20.LineSpacing / 2) - ResourceManager.Texture("NewUI/icon_build_edit").Height / 2) - 2, ResourceManager.Texture("NewUI/icon_build_edit").Width, ResourceManager.Texture("NewUI/icon_build_edit").Height);
            if (EditHoverState == 0 && !PlanetName.HandlingInput)
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit"), EditNameButton, Color.White);
            else
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit_hover2"), EditNameButton, Color.White);
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 768)
                vector2_2.Y += Font20.LineSpacing * 2;
            else
                vector2_2.Y += Font20.LineSpacing;
            batch.DrawString(Font12, Localizer.Token(384) + ":", vector2_2, Color.Orange);
            Vector2 position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, P.CategoryName, position3, new Color(255, 239, 208));
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(385) + ":", vector2_2, Color.Orange);
            var color = new Color(255, 239, 208);
            batch.DrawString(Font12, P.PopulationString, position3, color);
            var rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(385) + ":").X, Font12.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(75);
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(386) + ":", vector2_2, Color.Orange);
            if (P.Fertility.AlmostEqual(P.MaxFertility))
                batch.DrawString(Font12, P.Fertility.String(), position3, color);
            else
            {
                Color fertColor = P.Fertility < P.MaxFertility ? Color.LightGreen : Color.Pink;
                batch.DrawString(Font12, $"{P.Fertility.String()} / {P.MaxFertility.String()}", position3, fertColor);
            }

            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(386) + ":").X, Font12.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(20);
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(387) + ":", vector2_2, Color.Orange);
            batch.DrawString(Font12, P.MineralRichness.String(), position3, new Color(255, 239, 208));
            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(387) + ":").X, Font12.LineSpacing);


            // The Doctor: For planet income breakdown

            string gIncome = Localizer.Token(6125);
            string gUpkeep = Localizer.Token(6126);
            string nIncome = Localizer.Token(6127);
            string nLosses = Localizer.Token(6129);

            float grossIncome = P.Money.GrossRevenue;
            float grossUpkeep = P.Money.Maintenance;
            float netIncome   = P.Money.NetRevenue;

            Vector2 positionGIncome = vector2_2;
            positionGIncome.X = vector2_2.X + 1;
            positionGIncome.Y = vector2_2.Y + 28;
            Vector2 positionGrossIncome = position3;
            positionGrossIncome.Y = position3.Y + 28;
            positionGrossIncome.X = position3.X + 1;

            batch.DrawString(Fonts.Arial10, gIncome + ":", positionGIncome, Color.LightGray);
            batch.DrawString(Fonts.Arial10, grossIncome.String(2) + " BC/Y", positionGrossIncome, Color.LightGray);

            Vector2 positionGUpkeep = positionGIncome;
            positionGUpkeep.Y = positionGIncome.Y + (Fonts.Arial12.LineSpacing);
            Vector2 positionGrossUpkeep = positionGrossIncome;
            positionGrossUpkeep.Y += (Fonts.Arial12.LineSpacing);

            batch.DrawString(Fonts.Arial10, gUpkeep + ":", positionGUpkeep, Color.LightGray);
            batch.DrawString(Fonts.Arial10, grossUpkeep.String(2) + " BC/Y", positionGrossUpkeep, Color.LightGray);

            Vector2 positionNIncome = positionGUpkeep;
            positionNIncome.X = positionGUpkeep.X - 1;
            positionNIncome.Y = positionGUpkeep.Y + (Fonts.Arial12.LineSpacing + 2);
            Vector2 positionNetIncome = positionGrossUpkeep;
            positionNetIncome.X = positionGrossUpkeep.X - 1;
            positionNetIncome.Y = positionGrossUpkeep.Y + (Fonts.Arial12.LineSpacing + 2);

            batch.DrawString(Fonts.Arial12, (netIncome > 0.0 ? nIncome : nLosses) + ":", positionNIncome, netIncome > 0.0 ? Color.LightGreen : Color.Salmon);
            batch.DrawString(Font12, netIncome.String(2) + " BC/Y", positionNetIncome, netIncome > 0.0 ? Color.LightGreen : Color.Salmon);

            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(21);

            var portrait = new Rectangle(pDescription.Menu.X + 10, pDescription.Menu.Y + 30, 124, 148);
            while (portrait.Bottom > pDescription.Menu.Bottom)
            {
                portrait.Height -= (int)(0.1 * portrait.Height);
                portrait.Width  -= (int)(0.1 * portrait.Width);
            }
            batch.Draw(ResourceManager.Texture($"Portraits/{P.Owner.data.PortraitName}"), portrait, Color.White);
            batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), portrait, Color.White);
            batch.DrawRectangle(portrait, Color.Orange);
            if (P.colonyType == Planet.ColonyType.Colony)
                batch.Draw(ResourceManager.Texture("NewUI/x_red"), portrait, Color.White);

            // WorldType
            // [dropdown]
            // ColonTypeInfoText
            var description = new Rectangle(portrait.Right + 15, portrait.Y,
                                            pDescription.Menu.Right - portrait.Right - 20,
                                            pDescription.Menu.Height - 60);

            var descCursor = new Vector2(description.X, description.Y);
            batch.DrawString(Font12, P.WorldType, descCursor, Color.White);
            descCursor.Y += Font12.LineSpacing + 5;

            GovernorDropdown.Pos = descCursor;
            GovernorDropdown.Reset();
            descCursor.Y += GovernorDropdown.Height + 5;

            string colonyTypeInfo = Font12.ParseText(P.ColonyTypeInfoText, description.Width);
            batch.DrawString(Font12, colonyTypeInfo, descCursor, Color.White);
            GovernorDropdown.Draw(batch); // draw dropdown on top of other text

            if (GlobalStats.HardcoreRuleset)
            {
                foreach (ThreeStateButton threeStateButton in ResourceButtons)
                    threeStateButton.Draw(ScreenManager, (int)P.GetGoodAmount(threeStateButton.Good));
            }
            else
            {
                FoodStorage.Progress = P.FoodHere;
                ProdStorage.Progress = P.ProdHere;
                if      (P.FS == Planet.GoodState.STORE)  foodDropDown.ActiveIndex = 0;
                else if (P.FS == Planet.GoodState.IMPORT) foodDropDown.ActiveIndex = 1;
                else if (P.FS == Planet.GoodState.EXPORT) foodDropDown.ActiveIndex = 2;
                if (P.NonCybernetic)
                {
                    FoodStorage.Draw(batch);
                    foodDropDown.Draw(batch);
                }
                else
                {
                    FoodStorage.DrawGrayed(batch);
                    foodDropDown.DrawGrayed(batch);
                }
                ProdStorage.Draw(batch);
                if      (P.PS == Planet.GoodState.STORE)  prodDropDown.ActiveIndex = 0;
                else if (P.PS == Planet.GoodState.IMPORT) prodDropDown.ActiveIndex = 1;
                else if (P.PS == Planet.GoodState.EXPORT) prodDropDown.ActiveIndex = 2;
                prodDropDown.Draw(batch);
                batch.Draw(ResourceManager.Texture("NewUI/icon_storage_food"), FoodStorageIcon, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/icon_storage_production"), ProfStorageIcon, Color.White);
            }

            base.Draw(batch);

            if (ScreenManager.NumScreens == 2)
                Popup = true;

            close.Draw(batch);

            if (FoodStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(73);
            if (ProfStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(74);
        }


        Color TextColor { get; } = new Color(255, 239, 208);

        void DrawText(ref Vector2 cursor, string text)
        {
            DrawText(ref cursor, text, Color.White);
        }

        void DrawText(ref Vector2 cursor, int tokenId, Color color)
        {
            DrawText(ref cursor, Localizer.Token(tokenId), color);
        }

        void DrawText(ref Vector2 cursor, string text, Color color)
        {
            ScreenManager.SpriteBatch.DrawString(Font12, text, cursor, color);
            cursor.Y += Font12.LineSpacing;
        }

        void DrawTitledLine(ref Vector2 cursor, int titleId, string text)
        {
            Vector2 textCursor = cursor;
            textCursor.X += 100f;

            ScreenManager.SpriteBatch.DrawString(Font12, Localizer.Token(titleId) +": ", cursor, TextColor);
            ScreenManager.SpriteBatch.DrawString(Font12, text, textCursor, TextColor);
            cursor.Y += Font12.LineSpacing;
        }

        void DrawMultiLine(ref Vector2 cursor, string text)
        {
            DrawMultiLine(ref cursor, text, TextColor);
        }

        string MultiLineFormat(string text)
        {
            return Font12.ParseText(text, pFacilities.Menu.Width - 40);
        }

        string MultiLineFormat(int token)
        {
            return MultiLineFormat(Localizer.Token(token));
        }

        void DrawMultiLine(ref Vector2 cursor, string text, Color color)
        {
            string multiline = MultiLineFormat(text);
            ScreenManager.SpriteBatch.DrawString(Font12, multiline, cursor, color);
            cursor.Y += (Font12.MeasureString(multiline).Y + Font12.LineSpacing);
        }

        void DrawCommoditiesArea(Vector2 bCursor)
        {
            ScreenManager.SpriteBatch.DrawString(Font12, MultiLineFormat(4097), bCursor, TextColor);
        }

        void DrawDetailInfo(Vector2 bCursor)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            if (pFacilities.Tabs.Count > 1 && pFacilities.Tabs[1].Selected)
            {
                DrawCommoditiesArea(bCursor);
                return;
            }
            Color color = Color.Wheat;
            switch (DetailInfo)
            {
                case Troop t:
                    spriteBatch.DrawString(Font20, t.DisplayNameEmpire(P.Owner), bCursor, TextColor);
                    bCursor.Y += Font20.LineSpacing + 2;
                    string strength = t.Strength < t.ActualStrengthMax ? t.Strength + "/" + t.ActualStrengthMax
                        : t.ActualStrengthMax.String(1);

                    DrawMultiLine(ref bCursor, t.Description);
                    DrawTitledLine(ref bCursor, 338, t.TargetType);
                    DrawTitledLine(ref bCursor, 339, strength);
                    DrawTitledLine(ref bCursor, 2218, t.NetHardAttack.ToString());
                    DrawTitledLine(ref bCursor, 2219, t.NetSoftAttack.ToString());
                    DrawTitledLine(ref bCursor, 6008, t.BoardingStrength.ToString());
                    DrawTitledLine(ref bCursor, 6023, t.Level.ToString());
                    break;

                case string _:
                    DrawMultiLine(ref bCursor, P.Description);
                    string desc = "";
                    if (P.IsCybernetic)  desc = Localizer.Token(2028);
                    else switch (P.FS)
                    {
                        case Planet.GoodState.EXPORT: desc = Localizer.Token(2025); break;
                        case Planet.GoodState.IMPORT: desc = Localizer.Token(2026); break;
                        case Planet.GoodState.STORE:  desc = Localizer.Token(2027); break;
                    }

                    DrawMultiLine(ref bCursor, desc);
                    desc = "";
                    switch (P.PS)
                    {
                        case Planet.GoodState.EXPORT: desc = Localizer.Token(345); break;
                        case Planet.GoodState.IMPORT: desc = Localizer.Token(346); break;
                        case Planet.GoodState.STORE:  desc = Localizer.Token(347); break;
                    }
                    DrawMultiLine(ref bCursor, desc);
                    if (P.IsStarving)
                        DrawMultiLine(ref bCursor, Localizer.Token(344), Color.LightPink);
                    DrawPlanetStat(ref bCursor, spriteBatch);
                    break;

                case PlanetGridSquare pgs:
                    switch (pgs.building)
                    {
                        case null when pgs.Habitable && pgs.Biosphere:
                            spriteBatch.DrawString(Font20, Localizer.Token(348), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            spriteBatch.DrawString(Font12, MultiLineFormat(349), bCursor, color);
                            return;
                        case null when pgs.Habitable:
                            spriteBatch.DrawString(Font20, Localizer.Token(350), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            spriteBatch.DrawString(Font12, MultiLineFormat(349), bCursor, color);
                            return;
                    }

                    if (!pgs.Habitable && pgs.building == null)
                    {
                        if (P.IsBarrenType)
                        {
                            spriteBatch.DrawString(Font20, Localizer.Token(351), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            spriteBatch.DrawString(Font12, MultiLineFormat(352), bCursor, color);
                            return;
                        }
                        spriteBatch.DrawString(Font20, Localizer.Token(351), bCursor, color);
                        bCursor.Y += Font20.LineSpacing + 5;
                        spriteBatch.DrawString(Font12, MultiLineFormat(353), bCursor, color);
                        return;
                    }

                    if (pgs.building == null)
                        return;

                    Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    spriteBatch.Draw(ResourceManager.Texture("Ground_UI/GC_Square Selection"), bRect, Color.White);
                    spriteBatch.DrawString(Font20, Localizer.Token(pgs.building.NameTranslationIndex), bCursor, color);
                    bCursor.Y   += Font20.LineSpacing + 5;
                    string buildingDescription  = MultiLineFormat(pgs.building.DescriptionIndex);
                    spriteBatch.DrawString(Font12, buildingDescription, bCursor, color);
                    bCursor.Y   += Font12.MeasureString(buildingDescription).Y + Font20.LineSpacing;
                    DrawSelectedBuildingInfo(ref bCursor, spriteBatch, pgs.building);
                    if (!pgs.building.Scrappable)
                        return;

                    bCursor.Y = bCursor.Y + (Font12.LineSpacing + 10);
                    spriteBatch.DrawString(Font12, "You may scrap this building by right clicking it", bCursor, Color.White);
                    break;

                case ScrollList.Entry entry:
                    var selectedBuilding = entry.Get<Building>();
                    spriteBatch.DrawString(Font20, selectedBuilding.Name, bCursor, color);
                    bCursor.Y += Font20.LineSpacing + 5;
                    string selectionText = MultiLineFormat(selectedBuilding.DescriptionIndex);
                    spriteBatch.DrawString(Font12, selectionText, bCursor, color);
                    bCursor.Y += Font12.MeasureString(selectionText).Y + Font20.LineSpacing;
                    DrawSelectedBuildingInfo(ref bCursor, spriteBatch, selectedBuilding);
                    break;
            }
        }

        void DrawPlanetStat(ref Vector2 cursor, SpriteBatch batch)
        {
            DrawBuildingInfo(ref cursor, batch, P.Food.NetYieldPerColonist, "NewUI/icon_food", "food per colonist allocated to Food Production after taxes");
            DrawBuildingInfo(ref cursor, batch, P.Food.NetFlatBonus, "NewUI/icon_food", "flat food added generated per turn after taxes");
            DrawBuildingInfo(ref cursor, batch, P.Prod.NetYieldPerColonist, "NewUI/icon_production", "production per colonist allocated to Industry after taxes");
            DrawBuildingInfo(ref cursor, batch, P.Prod.NetFlatBonus, "NewUI/icon_production", "flat production added generated per turn after taxes");
            DrawBuildingInfo(ref cursor, batch, P.Res.NetYieldPerColonist, "NewUI/icon_science", "research per colonist allocated to Science before taxes");
            DrawBuildingInfo(ref cursor, batch, P.Res.NetFlatBonus, "NewUI/icon_science", "flat research added generated per turn after taxes");
        }

        void DrawSelectedBuildingInfo(ref Vector2 bCursor, SpriteBatch batch, Building b)
        {
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatFoodAmount, "NewUI/icon_food", Localizer.Token(354));
            DrawBuildingInfo(ref bCursor, batch, b.PlusFoodPerColonist, "NewUI/icon_food", Localizer.Token(2042));
            DrawBuildingInfo(ref bCursor, batch, b.SensorRange, "NewUI/icon_sensors", Localizer.Token(6000), signs: false);
            DrawBuildingInfo(ref bCursor, batch, b.ProjectorRange, "NewUI/icon_projection", Localizer.Token(6001), signs: false);
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatProductionAmount, "NewUI/icon_production", Localizer.Token(355));
            DrawBuildingInfo(ref bCursor, batch, b.PlusProdPerColonist, "NewUI/icon_production", Localizer.Token(356));
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatPopulation / 1000, "NewUI/icon_population", Localizer.Token(2043));
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatResearchAmount, "NewUI/icon_science", Localizer.Token(357));
            DrawBuildingInfo(ref bCursor, batch, b.PlusResearchPerColonist, "NewUI/icon_science", Localizer.Token(358));
            DrawBuildingInfo(ref bCursor, batch, b.PlusTaxPercentage * 100, "NewUI/icon_money", Localizer.Token(359), percent: true);
            DrawBuildingInfo(ref bCursor, batch, -b.MinusFertilityOnBuild, "NewUI/icon_food", Localizer.Token(360));
            DrawBuildingInfo(ref bCursor, batch, b.PlanetaryShieldStrengthAdded, "NewUI/icon_planetshield", Localizer.Token(361));
            DrawBuildingInfo(ref bCursor, batch, b.CreditsPerColonist, "NewUI/icon_money", Localizer.Token(362));
            DrawBuildingInfo(ref bCursor, batch, b.PlusProdPerRichness, "NewUI/icon_production", Localizer.Token(363));
            DrawBuildingInfo(ref bCursor, batch, b.ShipRepair * 10 * P.Level, "NewUI/icon_queue_rushconstruction", Localizer.Token(6137));
            DrawBuildingInfo(ref bCursor, batch, b.CombatStrength, "Ground_UI/Ground_Attack", Localizer.Token(364));
            float maintenance = -(b.Maintenance + b.Maintenance * P.Owner.data.Traits.MaintMod);
            DrawBuildingInfo(ref bCursor, batch, maintenance, "NewUI/icon_money", Localizer.Token(365));
            if (b.TheWeapon != null)
            {
                DrawBuildingInfo(ref bCursor, batch, b.TheWeapon.Range, "UI/icon_offense", "Range", signs: false);
                DrawBuildingInfo(ref bCursor, batch, b.TheWeapon.DamageAmount, "UI/icon_offense", "Damage", signs: false);
                DrawBuildingInfo(ref bCursor, batch, b.TheWeapon.DamageAmount, "UI/icon_offense", "EMP Damage", signs: false);
                DrawBuildingInfo(ref bCursor, batch, b.TheWeapon.NetFireDelay, "UI/icon_offense", "Fire Delay", signs: false);
            }
        }

        void DrawBuildingInfo(ref Vector2 cursor, SpriteBatch batch, float value, string texture, 
                              string toolTip, bool percent = false, bool signs = true)
        {
            DrawBuildingInfo(ref cursor, batch, value, ResourceManager.Texture(texture), toolTip, percent, signs);
        }

        void DrawBuildingInfo(ref Vector2 cursor, SpriteBatch batch, float value, SubTexture texture, 
                              string toolTip, bool percent = false, bool signs = true)
        {
            if (value.AlmostEqual(0))
                return;

            var fIcon   = new Rectangle((int)cursor.X, (int)cursor.Y, texture.Width, texture.Height);
            var tCursor = new Vector2(cursor.X + fIcon.Width + 5f, cursor.Y + 3f);
            string plusOrMinus = "";
            Color color = Color.White;
            if (signs)
            {
                plusOrMinus = value < 0 ? "-" : "+";
                color = value < 0 ? Color.Pink : Color.LightGreen;
            }
            batch.Draw(texture, fIcon, Color.White);
            string suffix = percent ? "% " : " ";
            string text = string.Concat(plusOrMinus, Math.Abs(value).String(2), suffix, toolTip);
            batch.DrawString(Font12, text, tCursor, color);
            cursor.Y += Font12.LineSpacing + 10;
        }

        void DrawTroopLevel(Troop troop, Rectangle rect)
        {
            SpriteFont font = Font12;
            var levelRect   = new Rectangle(rect.X + 30, rect.Y + 22, font.LineSpacing, font.LineSpacing + 5);
            var pos         = new Vector2((rect.X + 15 + rect.Width / 2) - font.MeasureString(troop.Strength.String(1)).X / 2f,
                                         (1 + rect.Y + 5 + rect.Height / 2 - font.LineSpacing / 2));

            ScreenManager.SpriteBatch.FillRectangle(levelRect, new Color(0, 0, 0, 200));
            ScreenManager.SpriteBatch.DrawRectangle(levelRect, troop.GetOwner().EmpireColor);
            ScreenManager.SpriteBatch.DrawString(font, troop.Level.ToString(), pos, Color.Gold);
        }

        void DrawPGSIcons(PlanetGridSquare pgs)
        {
            if (pgs.Biosphere)
            {
                Rectangle biosphere = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y, 20, 20);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Buildings/icon_biosphere_48x48"), biosphere, Color.White);
            }
            if (pgs.TroopsHere.Count > 0)
            {
                Troop troop        = pgs.TroopsHere[0];
                pgs.TroopClickRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width - 48, pgs.ClickRect.Y, 48, 48);
                troop.DrawIcon(ScreenManager.SpriteBatch, pgs.TroopClickRect);
                if (troop.Level > 0)
                    DrawTroopLevel(troop, pgs.TroopClickRect);
            }
            float numFood = 0f;
            float numProd = 0f;
            float numRes  = 0f;
            if (pgs.building != null)
            {
                if (pgs.building.PlusFlatFoodAmount > 0f || pgs.building.PlusFoodPerColonist > 0f)
                {
                    numFood = numFood + pgs.building.PlusFoodPerColonist * P.PopulationBillion * P.Food.Percent;
                    numFood = numFood + pgs.building.PlusFlatFoodAmount;
                }
                if (pgs.building.PlusFlatProductionAmount > 0f || pgs.building.PlusProdPerColonist > 0f)
                {
                    numProd = numProd + pgs.building.PlusFlatProductionAmount;
                    numProd = numProd + pgs.building.PlusProdPerColonist * P.PopulationBillion * P.Prod.Percent;
                }
                if (pgs.building.PlusProdPerRichness > 0f)
                {
                    numProd = numProd + pgs.building.PlusProdPerRichness * P.MineralRichness;
                }
                if (pgs.building.PlusResearchPerColonist > 0f || pgs.building.PlusFlatResearchAmount > 0f)
                {
                    numRes = numRes + pgs.building.PlusResearchPerColonist * P.PopulationBillion * P.Res.Percent;
                    numRes = numRes + pgs.building.PlusFlatResearchAmount;
                }
            }
            float total = numFood + numProd + numRes;
            float totalSpace = pgs.ClickRect.Width - 30;
            float spacing = totalSpace / total;
            Rectangle rect = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y + pgs.ClickRect.Height - ResourceManager.Texture("NewUI/icon_food").Height, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
            for (int i = 0; (float)i < numFood; i++)
            {
                if (numFood - i <= 0f || numFood - i >= 1f)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), rect, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), new Vector2(rect.X, rect.Y), Color.White, 0f, Vector2.Zero, numFood - i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
            for (int i = 0; (float)i < numProd; i++)
            {
                if (numProd - i <= 0f || numProd - i >= 1f)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), rect, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), new Vector2(rect.X, rect.Y), Color.White, 0f, Vector2.Zero, numProd - i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
            for (int i = 0; (float)i < numRes; i++)
            {
                if (numRes - i <= 0f || numRes - i >= 1f)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), rect, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), new Vector2(rect.X, rect.Y), Color.White, 0f, Vector2.Zero, numRes - i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
        }

        public static int GetIndex(Planet p)
        {
            switch (p.colonyType)
            {
                case Planet.ColonyType.Colony: return 0;
                case Planet.ColonyType.Core: return 1;
                case Planet.ColonyType.Industrial: return 2;
                case Planet.ColonyType.Agricultural: return 3;
                case Planet.ColonyType.Research: return 4;
                case Planet.ColonyType.Military: return 5;
                case Planet.ColonyType.TradeHub: return 6;
            }
            return 0;
        }

        void HandleDetailInfo(InputState input)
        {
            DetailInfo = null;
            foreach (ScrollList.Entry e in buildSL.VisibleExpandedEntries)
            {
                if (e.CheckHover(input))
                {
                    if (e.Is<Building>())   DetailInfo = e; // @todo Why are we storing Entry here???
                    else if (e.Is<Troop>()) DetailInfo = e.item;
                }
            }
            if (DetailInfo == null)
                DetailInfo = P.Description;
        }

        public override bool HandleInput(InputState input)
        {
            pFacilities.HandleInputNoReset(input);

            if (HandleCycleColoniesLeftRight(input))
                return true;

            P.UpdateIncomes(false);
            HandleDetailInfo(input);
            buildSL.HandleInput(input);
            build.HandleInput(input);

            // AI specific
            if (P.Owner != EmpireManager.Player)
            {
                HandleDetailInfo(input);
                return true;
            }

            HandlePlanetNameChangeTextBox(input);

            GovernorDropdown.HandleInput(input);
            P.colonyType = (Planet.ColonyType)GovernorDropdown.ActiveValue;

            HandleSliders(input);

            if (P.HasShipyard && build.Tabs.Count > 1 && build.Tabs[1].Selected)
            {
                if (PlayerDesignsToggle.Rect.HitTest(input.CursorPosition))
                {
                    ToolTip.CreateTooltip(Localizer.Token(2225));
                }
                if (PlayerDesignsToggle.HandleInput(input) && !input.LeftMouseReleased)
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    GlobalStats.ShowAllDesigns = !GlobalStats.ShowAllDesigns;
                    PlayerDesignsToggle.Active = GlobalStats.ShowAllDesigns;
                    ResetLists();
                }
            }

            Selector = null;
            if (HandleTroopSelect(input))
                return true;

            HandleExportImportButtons(input);
            HandleConstructionQueueInput(input);
            HandleDragBuildingOntoTile(input);
            HandleBuildListClicks(input);

            if (Popup)
            {
                if (!input.RightMouseHeldUp)
                    return true;
                else
                    Popup = false;
            }
            return base.HandleInput(input);
        }

        bool HandleTroopSelect(InputState input)
        {
            ClickedTroop = false;
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (!pgs.ClickRect.HitTest(MousePos))
                {
                    pgs.highlighted = false;
                }
                else
                {
                    if (!pgs.highlighted)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_mouseover");
                    }

                    pgs.highlighted = true;
                }

                if (pgs.TroopsHere.Count <= 0 || !pgs.TroopClickRect.HitTest(MousePos))
                    continue;

                DetailInfo = pgs.TroopsHere[0];
                if (input.RightMouseClick && pgs.TroopsHere[0].GetOwner() == EmpireManager.Player)
                {
                    GameAudio.PlaySfxAsync("sd_troop_takeoff");
                    Ship.CreateTroopShipAtPoint(P.Owner.data.DefaultTroopShip, P.Owner, P.Center, pgs.TroopsHere[0]);
                    P.TroopsHere.Remove(pgs.TroopsHere[0]);
                    pgs.TroopsHere[0].SetPlanet(null);
                    pgs.TroopsHere.Clear();
                    ClickedTroop = true;
                    DetailInfo = null;
                }

                return true;
            }

            if (!ClickedTroop)
            {
                foreach (PlanetGridSquare pgs in P.TilesList)
                {
                    if (pgs.ClickRect.HitTest(input.CursorPosition))
                    {
                        DetailInfo = pgs;
                        var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32,
                            pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                        if (pgs.building != null && bRect.HitTest(input.CursorPosition) && Input.RightMouseClick)
                        {
                            if (pgs.building.Scrappable)
                            {
                                ToScrap = pgs.building;
                                string message = string.Concat("Do you wish to scrap ",
                                    Localizer.Token(pgs.building.NameTranslationIndex),
                                    "? Half of the building's construction cost will be recovered to your storage.");
                                var messageBox = new MessageBoxScreen(Empire.Universe, message);
                                messageBox.Accepted += ScrapAccepted;
                                ScreenManager.AddScreen(messageBox);
                            }

                            ClickedTroop = true;
                            return true;
                        }
                    }

                    if (pgs.TroopsHere.Count <= 0 || !pgs.TroopClickRect.HitTest(input.CursorPosition))
                        continue;

                    DetailInfo = pgs.TroopsHere;
                }
            }

            return false;
        }

        bool HandleCycleColoniesLeftRight(InputState input)
        {
            if      (RightColony.Rect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(Localizer.Token(2279));
            else if (LeftColony.Rect.HitTest(input.CursorPosition))  ToolTip.CreateTooltip(Localizer.Token(2280));

            bool canView = (Empire.Universe.Debug || P.Owner == EmpireManager.Player);
            if (!canView)
                return false;
           
            int change = 0;
            if (input.Right || RightColony.HandleInput(input) && input.LeftMouseClick)
                change = +1;
            else if (input.Left || LeftColony.HandleInput(input) && input.LeftMouseClick)
                change = -1;

            if (change != 0)
            {
                var planets = P.Owner.GetPlanets();
                int newIndex = planets.IndexOf(P) + change;
                if (newIndex >= planets.Count) newIndex = 0;
                else if (newIndex < 0)         newIndex = planets.Count-1;

                Planet nextOrPrevPlanet = planets[newIndex];
                if (nextOrPrevPlanet != P)
                {
                    Empire.Universe.workersPanel = new ColonyScreen(Empire.Universe, nextOrPrevPlanet, eui);
                }
                return true; // planet changed, ColonyScreen will be replaced
            }
            return false;
        }

        void HandlePlanetNameChangeTextBox(InputState input)
        {
            if (!EditNameButton.HitTest(input.CursorPosition))
            {
                EditHoverState = 0;
            }
            else
            {
                EditHoverState = 1;
                if (input.LeftMouseClick)
                {
                    PlanetName.HandlingInput = true;
                }
            }

            if (!PlanetName.HandlingInput)
            {
                GlobalStats.TakingInput = false;
                bool empty = true;
                string text = PlanetName.Text;
                int num = 0;
                while (num < text.Length)
                {
                    if (text[num] == ' ')
                    {
                        num++;
                    }
                    else
                    {
                        empty = false;
                        break;
                    }
                }

                if (empty)
                {
                    int ringnum = 1;
                    foreach (SolarSystem.Ring ring in P.ParentSystem.RingList)
                    {
                        if (ring.planet == P)
                        {
                            PlanetName.Text = string.Concat(P.ParentSystem.Name, " ",
                                RomanNumerals.ToRoman(ringnum));
                        }

                        ringnum++;
                    }
                }
            }
            else
            {
                GlobalStats.TakingInput = true;
                PlanetName.HandleTextInput(ref PlanetName.Text, input);
            }
        }

        void HandleExportImportButtons(InputState input)
        {
            if (!GlobalStats.HardcoreRuleset)
            {
                if (foodDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    foodDropDown.Toggle();
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    P.FS = (Planet.GoodState) ((int) P.FS + (int) Planet.GoodState.IMPORT);
                    if (P.FS > Planet.GoodState.EXPORT)
                        P.FS = Planet.GoodState.STORE;
                }

                if (prodDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    prodDropDown.Toggle();
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    P.PS = (Planet.GoodState) ((int) P.PS + (int) Planet.GoodState.IMPORT);
                    if (P.PS > Planet.GoodState.EXPORT)
                        P.PS = Planet.GoodState.STORE;
                }
            }
            else
            {
                foreach (ThreeStateButton b in ResourceButtons)
                    b.HandleInput(input, ScreenManager);
            }
        }

        void OnSendTroopsClicked(UIButton b)
        {
            Array<Ship> troopShips;
            using (eui.empire.GetShips().AcquireReadLock())
                troopShips = new Array<Ship>(eui.empire.GetShips()
                    .Where(troop => troop.TroopList.Count > 0
                                    && (troop.AI.State == AIState.AwaitingOrders || troop.AI.State == AIState.Orbit)
                                    && troop.fleet == null && !troop.InCombat)
                    .OrderBy(distance => Vector2.Distance(distance.Center, P.Center)));

            Array<Planet> planetTroops = new Array<Planet>(eui.empire.GetPlanets()
                .Where(troops => troops.TroopsHere.Count > 1).OrderBy(distance => Vector2.Distance(distance.Center, P.Center))
                .Where(Name => Name.Name != P.Name));

            if (troopShips.Count > 0)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                troopShips.First().AI.OrderRebase(P, true);
            }
            else if (planetTroops.Count > 0)
            {
                var troops = planetTroops.First().TroopsHere;
                using (troops.AcquireWriteLock())
                {
                    Ship troop = troops.First().Launch();
                    if (troop != null)
                    {
                        GameAudio.PlaySfxAsync("echo_affirm");
                        troop.AI.OrderRebase(P, true);
                    }
                }
            }
            else
            {
                GameAudio.PlaySfxAsync("blip_click");
            }
        }

        void OnLaunchTroopsClicked(UIButton b)
        {
            bool play = false;
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != EmpireManager.Player)
                    continue;

                play = true;
                Ship.CreateTroopShipAtPoint(P.Owner.data.DefaultTroopShip, P.Owner, P.Center, pgs.TroopsHere[0]);
                P.TroopsHere.Remove(pgs.TroopsHere[0]);
                pgs.TroopsHere[0].SetPlanet(null);
                pgs.TroopsHere.Clear();
                ClickedTroop = true;
                DetailInfo = null;
            }

            if (play)
            {
                GameAudio.PlaySfxAsync("sd_troop_takeoff");
            }
        }

        public void ResetLists()
        {
            Reset = true;
        }

        void ScrapAccepted(object sender, EventArgs e)
        {
            ToScrap?.ScrapBuilding(P);
            Update(0f);
        }

        public override void Update(float elapsedTime)
        {
            P.UpdateIncomes(false);
            if (!P.CanBuildInfantry())
            {
                bool remove = false;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(336))
                    {
                        continue;
                    }
                    remove = true;
                }
                if (remove)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(334));
                    if (P.HasShipyard)
                    {
                        build.AddTab(Localizer.Token(335));
                    }
                }
            }
            else
            {
                bool add = true;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(336))
                        continue;
                    add = false;
                    foreach (ScrollList.Entry entry in buildSL.VisibleEntries)
                        if (entry.TryGet(out Troop troop))
                            troop.Update(elapsedTime);
                }
                if (add)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(334));
                    if (P.HasShipyard)
                    {
                        build.AddTab(Localizer.Token(335));
                    }
                    build.AddTab(Localizer.Token(336));
                }
            }
            if (!P.HasShipyard)
            {
                bool remove = false;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(335))
                    {
                        continue;
                    }
                    remove = true;
                }
                if (remove)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(334));
                    if (P.AllowInfantry)
                    {
                        build.AddTab(Localizer.Token(336));
                    }
                }
            }
            else
            {
                bool add = true;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(335))
                    {
                        continue;
                    }
                    add = false;
                }
                if (add)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(334));
                    build.AddTab(Localizer.Token(335));
                    if (P.AllowInfantry)
                    {
                        build.AddTab(Localizer.Token(336));
                    }
                }
            }
        }

        void HandleSliders(InputState input)
        {
            Sliders.HandleInput(input);
            P.UpdateIncomes(false);
        }

        void CreateSliders(Rectangle laborPanel)
        {
            int sliderW = ((int)(laborPanel.Width * 0.6)).RoundUpToMultipleOf(10);
            int sliderX = laborPanel.X + 60;
            int sliderY = laborPanel.Y + 25;
            int slidersAreaH = laborPanel.Height - 25;
            int spacingY = (int)(0.25 * slidersAreaH);
            Sliders = new ColonySliderGroup(this, laborPanel);
            Sliders.Create(sliderX, sliderY, sliderW, spacingY);
            Sliders.SetPlanet(P);
        }
            
        void DrawSliders(SpriteBatch batch)
        {
            Sliders.Draw(batch);
        }
    }
}