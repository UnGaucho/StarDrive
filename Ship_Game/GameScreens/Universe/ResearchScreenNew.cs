using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ResearchScreenNew : GameScreen
    {
        public Camera2D camera = new Camera2D();

        private Map<string, Node> RootNodes = new Map<string, Node>(StringComparer.OrdinalIgnoreCase);
        public Map<string, Node> CompleteSubNodeTree = new Map<string, Node>(StringComparer.OrdinalIgnoreCase);
        public Map<string, Node> SubNodes = new Map<string, Node>(StringComparer.OrdinalIgnoreCase);

        private Vector2 Cursor = Vector2.Zero;

        private CloseButton close;

        private Menu2 MainMenu;

        public EmpireUIOverlay empireUI;

        private Vector2 MainMenuOffset;

        private Rectangle QueueContainer;

        public QueueComponent qcomponent;

        private string UIDCurrentRoot = "";

        private int ColumnOffset = 175;

        private int RowOffset = 100;

        private Array<Vector2> ClaimedSpots = new Array<Vector2>();

        private Vector2 cameraVelocity = Vector2.Zero;

        public bool RightClicked;

        private Vector2 StartDragPos = Vector2.Zero;

        private float ClickTimer;


        public ResearchScreenNew(GameScreen parent, EmpireUIOverlay empireUi) : base(parent)
        {
            empireUI = empireUi;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            batch.Begin();
            batch.FillRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.Black);
            MainMenu.Draw(batch);
            batch.End();

            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, camera.Transform);
            
            DrawConnectingLines(batch);

            foreach (KeyValuePair<string, Node> keyValuePair in RootNodes)
            {
                if (keyValuePair.Value is RootNode rootNode)
                    rootNode.Draw(ScreenManager.SpriteBatch);
            }

            foreach (KeyValuePair<string, Node> keyValuePair in SubNodes)
            {
                if (keyValuePair.Value is TreeNode treeNode)
                    treeNode.Draw(ScreenManager);
            }
            batch.End();

            batch.Begin();
            close.Draw(batch);
            qcomponent.Draw(batch);
            ToolTip.Draw(batch);
            batch.End();
        }

        static Vector2 CenterBetweenPoints(Vector2 left, Vector2 right)
        {
            return left.LerpTo(right, 0.5f).Rounded();
        }

        RootNode GetCurrentlySelectedRootNode()
        {
            foreach (Node node in RootNodes.Values)
                if (node is RootNode root && root.nodeState == NodeState.Press)
                    return root;
            return null;
        }

        void DrawConnectingLines(SpriteBatch batch)
        {
            RootNode rootNode = GetCurrentlySelectedRootNode();

            // Level 0 VERTICAL line coming straight from root nodes
            {
                Vector2 rootNodeRight = rootNode.RightPoint;
                var nextNodeLeft = new Vector2(MainMenuOffset.X + ColumnOffset, rootNodeRight.Y);
                Vector2 midPoint = CenterBetweenPoints(rootNode.RightPoint, nextNodeLeft);

                foreach (Technology.LeadsToTech leadsTo in rootNode.tech.Tech.LeadsTo)
                {
                    TechEntry techEntry = EmpireManager.Player.GetTechEntry(leadsTo.UID);
                    techEntry = techEntry.FindNextDiscoveredTech(EmpireManager.Player);
                    if (techEntry != null)
                    {
                        var treeNode = (TreeNode) SubNodes[techEntry.UID];
                        var midPointOther = new Vector2(midPoint.X, treeNode.BaseRect.CenterY() - 10);
                        batch.DrawResearchLineVertical(midPoint, midPointOther, techEntry.Unlocked);
                    }
                }

                bool firstTechComplete = false;
                foreach (Technology.LeadsToTech leadsTo in rootNode.tech.Tech.LeadsTo)
                {
                    TechEntry techEntry = EmpireManager.Player.GetTechEntry(leadsTo.UID);
                    techEntry = techEntry.FindNextDiscoveredTech(EmpireManager.Player);
                    if (techEntry != null)
                    {
                        if (techEntry.Unlocked)
                            firstTechComplete = true;
                        
                        var treeNode = (TreeNode)SubNodes[techEntry.UID];
                        var midPointOther = new Vector2(midPoint.X, treeNode.BaseRect.CenterY() - 10);
                        Vector2 RightPoint2 = midPointOther + new Vector2(rootNodeRight.Distance(nextNodeLeft) + 13f, 0.0f);
                        batch.DrawResearchLineHorizontalGradient(midPointOther, RightPoint2, techEntry.Unlocked);
                    }
                }

                batch.DrawResearchLineHorizontal(rootNodeRight, midPoint, firstTechComplete);
            }

            foreach (KeyValuePair<string, Node> keyValuePair in SubNodes)
            {
                if (!(keyValuePair.Value is TreeNode treeNode))
                    continue;

                var vector21 = new Vector2(treeNode.BaseRect.X + treeNode.BaseRect.Width - 25,
                                           treeNode.BaseRect.Y + treeNode.BaseRect.Height / 2 - 10);
                Vector2 vector22 = vector21 + new Vector2(ColumnOffset / 2f, 0.0f);
                vector22.Y = vector21.Y;

                foreach (Technology.LeadsToTech leadsTo in treeNode.tech.Tech.LeadsTo)
                {
                    TechEntry techEntry1 = EmpireManager.Player.GetTechEntry(leadsTo.UID);
                    techEntry1 = techEntry1.FindNextDiscoveredTech(EmpireManager.Player);
                    if (techEntry1 != null)
                    {
                        var treeNode1 = SubNodes[techEntry1.UID] as TreeNode;
                        var vector23 = new Vector2(vector22.X, treeNode1.BaseRect.CenterY() - 10);
                        batch.DrawResearchLineVertical(vector22, vector23, techEntry1.Unlocked);
                    }
                }
            }

            foreach (Node subNode in SubNodes.Values)
            {
                if (!(subNode is TreeNode node))
                    continue;

                Technology technology2 = node.tech.Tech;
                if (technology2.LeadsTo.Count <= 0)
                    continue;

                var leftPoint = node.RightPoint;
                Vector2 rightPoint = leftPoint + new Vector2(ColumnOffset / 2, 0.0f);
                bool complete1 = false;
                foreach (Technology.LeadsToTech leadsToTech2 in technology2.LeadsTo)
                {
                    TechEntry techEntry2 = EmpireManager.Player.GetTechEntry(leadsToTech2.UID);
                    techEntry2 = techEntry2.FindNextDiscoveredTech(EmpireManager.Player);
                    if (techEntry2 != null)
                    {
                        if (techEntry2.Unlocked)
                            complete1 = true;

                        var treeNode3 = (SubNodes[techEntry2.UID] as TreeNode);
                        var LeftPoint2 = new Vector2(rightPoint.X, treeNode3.BaseRect.CenterY() - 10);
                        Vector2 RightPoint2 = LeftPoint2 + new Vector2(leftPoint.Distance(rightPoint) + 13f, 0.0f);
                        batch.DrawResearchLineHorizontalGradient(LeftPoint2, RightPoint2, techEntry2.Unlocked);
                    }
                }

                batch.DrawResearchLineHorizontal(leftPoint, rightPoint, complete1);
            }

        }

        public override void ExitScreen()
        {
            GlobalStats.ResearchRootUIDToDisplay = UIDCurrentRoot;
            RightClicked = false;
            base.ExitScreen();
        }

        private int FindDeepestY()
        {
            int deepest = 0;
            foreach (KeyValuePair<string, Node> node in RootNodes)
            {
                if (node.Value.NodePosition.Y <= deepest)
                {
                    continue;
                }
                deepest = (int)node.Value.NodePosition.Y;
            }
            return deepest;
        }

        private int FindDeepestYSubNodes()
        {
            int deepest = 0;
            foreach (KeyValuePair<string, Node> node in SubNodes)
            {
                if (node.Value.NodePosition.Y <= deepest)
                {
                    continue;
                }
                deepest = (int)node.Value.NodePosition.Y;
            }
            return deepest;
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Escaped || input.ResearchExitScreen || close.HandleInput(input))
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }

            if (input.RightMouseHeldDown)
            {
                StartDragPos = input.CursorPosition;
                cameraVelocity = Vector2.Zero;
            }
            if (input.MouseCurr.RightButton != ButtonState.Pressed || input.MousePrev.RightButton != ButtonState.Pressed || RightClicked) 
            {
                cameraVelocity = Vector2.Zero;
                if (RightClicked) // fbedard: prevent screen scroll
                    StartDragPos = input.CursorPosition;
            }
            else
            {
                camera.Pos += (StartDragPos - input.CursorPosition);
                StartDragPos = input.CursorPosition;
            }

            cameraVelocity = cameraVelocity.Clamped(-10f, 10f);
            camera.Pos = camera.Pos.Clamped(ScreenCenter.X, ScreenCenter.Y, 3200f, 3200f);

            // unlock ALL techs
            if (input.IsCtrlKeyDown && input.KeyPressed(Keys.F1))
            {
                foreach (TechEntry techEntry in EmpireManager.Player.TechEntries)
                {
                    techEntry.Unlock(EmpireManager.Player);
                }
                ReloadContent();
                EmpireManager.Player.UpdateShipsWeCanBuild();
            }

            // Unlock only selected node for player 
            if (input.IsCtrlKeyDown && input.KeyPressed(Keys.F2))
            {
                if (qcomponent.CurrentResearch != null)
                {
                    qcomponent.CurrentResearch.Node.tech.Unlock(EmpireManager.Player);
                    EmpireManager.Player.ResearchTopic = "";
                    ReloadContent();
                    EmpireManager.Player.UpdateShipsWeCanBuild();
                }
                else
                {
                    GameAudio.NegativeClick();
                }
            }

            //Added by McShooterz: Cheat ot unlock non bonus tech 
            if (input.IsCtrlKeyDown  && input.KeyPressed(Keys.F3))
            {
                foreach (KeyValuePair<string, Technology> tech in ResourceManager.TechTree)
                {
                    UnlockTreeNoBonus(tech.Key);
                }
                EmpireManager.Player.UpdateShipsWeCanBuild();
                ReloadContent();
            }

            qcomponent.HandleInput(input);
            if (qcomponent.Visible && qcomponent.container.HitTest(input.CursorPosition))
                return true;

            foreach (Node tech in RootNodes.Values)
            {
                if (tech.HandleInput(input) && tech is RootNode root)
                {
                    PopulateNodesFromRoot(root);
                    GameAudio.ResearchSelect();
                }
            }

            RightClicked = false;
            foreach (KeyValuePair<string, Node> tech in SubNodes)
            {
                if (!(tech.Value as TreeNode).HandleInput(input, ScreenManager, camera))
                {
                    if ((tech.Value as TreeNode).screen.RightClicked)  //fbedard: popup open
                        RightClicked = true;
                    continue;
                }
                if (EmpireManager.Player.HasUnlocked(tech.Key))
                {
                    GameAudio.NegativeClick();
                }
                else if (EmpireManager.Player.HavePreReq(tech.Key))
                {
                    qcomponent.SetVisible();
                    qcomponent.AddToQueue(tech.Value as TreeNode);
                    GameAudio.ResearchSelect();
                }
                else if (EmpireManager.Player.HavePreReq(tech.Key))
                {
                    GameAudio.NegativeClick();
                }
                else
                {
                    qcomponent.SetVisible();
                    GameAudio.ResearchSelect();
                    var techToCheck = tech.Value.tech;
                    var techsToAdd = new Array<string>{ techToCheck.UID };
                    if (techToCheck.Tech.RootNode != 1)
                    {
                        while (!techToCheck.Unlocked)
                        {
                            var preReq = techToCheck.GetPreReq(EmpireManager.Player);
                            if (preReq == null)
                            {
                                break;
                            }                            
                            if (!preReq.Unlocked )
                            {
                                techsToAdd.Add(preReq.UID);
                            }
                            techToCheck = preReq;
                        }
                    }
                    techsToAdd.Reverse();
                    foreach (string toAdd in techsToAdd)
                    {
                        TechEntry techEntry = EmpireManager.Player.GetTechEntry(toAdd);
                        if (techEntry.Discovered) qcomponent.AddToQueue(SubNodes[toAdd] as TreeNode);
                    }
                }
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            camera              = new Camera2D();
            camera.Pos          = new Vector2(Viewport.Width / 2f, Viewport.Height / 2f);
            Rectangle main      = new Rectangle(0, 0, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth
                , ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
            MainMenu       = new Menu2(main);
            MainMenuOffset = new Vector2(main.X + 20, main.Y + 30);
            close          = new CloseButton(this, new Rectangle(main.X + main.Width - 40, main.Y + 20, 20, 20));
            QueueContainer = new Rectangle(main.X + main.Width - 355, main.Y + 40, 330, main.Height - 100);
            qcomponent     = new QueueComponent(ScreenManager, QueueContainer, this);

            RootNodes.Clear();
            CompleteSubNodeTree.Clear();
            SubNodes.Clear();

            if (ScreenWidth < 1600)
            {
                qcomponent.SetInvisible();
            }

            int numDiscoveredRoots = EmpireManager.Player.TechEntries.Count(t => t.IsRoot && t.Discovered);

            RowOffset = (main.Height - 40) / numDiscoveredRoots;
            MainMenuOffset.Y = main.Y + RowOffset / 3;
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight <= 720)
            {
                MainMenuOffset.Y = MainMenuOffset.Y + 8f;
            }

            foreach (TechEntry tech in EmpireManager.Player.TechEntries)
            {
                if (tech.IsRoot && tech.Discovered)
                {
                    Cursor.X = 0f;
                    Cursor.Y = FindDeepestY() + 1;
                    SetNode(tech);
                }
            }

            RowOffset = (main.Height - 40) / 6;
            foreach (KeyValuePair<string, Node> entry in RootNodes)
            {
                PopulateAllTechsFromRoot(entry.Value as RootNode);
            }
            PopulateNodesFromRoot(RootNodes[GlobalStats.ResearchRootUIDToDisplay] as RootNode);
            if (!string.IsNullOrEmpty(EmpireManager.Player.ResearchTopic))
            {
                if (CompleteSubNodeTree.TryGetValue(EmpireManager.Player.ResearchTopic, out Node resTopNode))
                {
                    qcomponent.LoadQueue(resTopNode as TreeNode);
                }
                else
                {
                    EmpireManager.Player.ResearchTopic = "";
                }
            }
            foreach (string uid in EmpireManager.Player.data.ResearchQueue)
            {
                qcomponent.LoadQueue(CompleteSubNodeTree[uid] as TreeNode);
            }
            base.LoadContent();
        }

        public void PopulateAllTechs(Node treenode)
        {
            Technology technology = treenode.tech.Tech;
            for (int i = 0; i < technology.LeadsTo.Count; i++)
            {
                TechEntry techEntry = EmpireManager.Player.GetTechEntry(technology.LeadsTo[i].UID);
                if (i != 0)
                {
                    Cursor.Y = FindDeepestYSubNodes() + 1;
                }
                else
                {
                    Cursor.Y = FindDeepestYSubNodes();
                }
                Cursor.X = treenode.NodePosition.X + 1f;
                var newNode = new TreeNode(MainMenuOffset + new Vector2(ColumnOffset * (int)Cursor.X, RowOffset * (int)Cursor.Y), techEntry, this)
                {
                    NodePosition = Cursor
                };
                if (EmpireManager.Player.HasUnlocked(techEntry))
                {
                    newNode.complete = true;
                }
                if (techEntry.Discovered)
                    CompleteSubNodeTree.Add(newNode.tech.UID, newNode);
                PopulateAllTechs(newNode);
            }
        }

        public void PopulateAllTechsFromRoot(RootNode Root)
        {
            foreach (KeyValuePair<string, Node> node in RootNodes)
            {
                (node.Value as RootNode).nodeState = NodeState.Normal;
            }
            Root.nodeState = NodeState.Press;
            Cursor = new Vector2(1f, 1f);
            Technology technology = ResourceManager.TechTree[Root.tech.UID];            
            for (int i = 0; i < technology.LeadsTo.Count; i++)
            {
                TechEntry techEntry = EmpireManager.Player.GetTechEntry(technology.LeadsTo[i].UID);

                var newNode = new TreeNode(MainMenuOffset + new Vector2(ColumnOffset * (int) Cursor.X,
                                           RowOffset * (int) Cursor.Y), techEntry, this)
                {
                    NodePosition = Cursor
                };
                if (EmpireManager.Player.HasUnlocked(techEntry))
                {
                    newNode.complete = true;
                }
                if (techEntry.Discovered)
                    CompleteSubNodeTree.Add(newNode.tech.UID, newNode);
                PopulateAllTechs(newNode);
            }
        }

        public void PopulateNodesFromRoot(RootNode Root)
        {
            UIDCurrentRoot = Root.tech.UID;
            SubNodes.Clear();
            int Rows = 1;
            int Cols = CalculateTreeDimensionsFromRoot(Root.tech.UID, ref Rows, 0, 0);
            if (Rows < 9)
                RowOffset = (MainMenu.Menu.Height - 40) / Rows;
            else
                RowOffset = (MainMenu.Menu.Height - 40) / 9;
            if (Cols > 0 && Cols < 9)
                ColumnOffset = (MainMenu.Menu.Width - 350) / Cols + 1;
            else
                ColumnOffset = 165;
            foreach (KeyValuePair<string, Node> node in RootNodes)
            {
                (node.Value as RootNode).nodeState = NodeState.Normal;
            }
            Root.nodeState = NodeState.Press;
            ClaimedSpots.Clear();
            Cursor = new Vector2(1f, 1f);
            bool first = true;
            Technology technology = ResourceManager.TechTree[Root.tech.UID];
            for (int i = 0; i < technology.LeadsTo.Count; i++)
            {
                TechEntry techEntry = EmpireManager.Player.GetTechEntry(technology.LeadsTo[i].UID)
                                                   .FindNextDiscoveredTech(EmpireManager.Player);
                if (techEntry != null)
                {
                    if (!first)
                    {
                        Cursor.Y = FindDeepestYSubNodes() + 1;
                    }
                    else
                    {
                        Cursor.Y = FindDeepestYSubNodes();
                        first = false;
                    }
                    Cursor.X = Root.NodePosition.X + 1f;
                    TechEntry te = techEntry;
                    TreeNode newNode = new TreeNode(MainMenuOffset + new Vector2(ColumnOffset * (int)Cursor.X, 
                        RowOffset * (int)Cursor.Y), te, this)
                    {
                        NodePosition = Cursor
                    };
                    if (EmpireManager.Player.HasUnlocked(te))
                    {
                        newNode.complete = true;
                    }
                    
                        SubNodes.Add(newNode.tech.UID, newNode);
                    PopulateNodesFromSubNode(newNode);
                }
            }
        }

        public void PopulateNodesFromSubNode(Node treenode)
        {
            Vector2 Position = new Vector2(Cursor.X, Cursor.Y);
            bool SeatTaken = false;
            foreach (Vector2 v in ClaimedSpots)
            {
                if (v.X != Position.X || v.Y != Position.Y)
                {
                    continue;
                }
                SeatTaken = true;
            }
            if (SeatTaken)
            {
                Cursor.Y = Cursor.Y + 1f;
            }
            else if (treenode.tech.Discovered)
            {
                ClaimedSpots.Add(Position);
            }

            Technology technology = ResourceManager.TechTree[treenode.tech.UID];
            for (int i = 0; i < technology.LeadsTo.Count; i++)
            {
                TechEntry techEntry = EmpireManager.Player.GetTechEntry(technology.LeadsTo[i].UID);
                if (i != 0)
                {
                    Cursor.Y = FindDeepestYSubNodes() + 1;
                }
                else
                {
                    Cursor.Y = FindDeepestYSubNodes();
                }
                Cursor.X = treenode.NodePosition.X + 1f;
                float x = ColumnOffset * (int)Cursor.X;
                float y = RowOffset * (int)Cursor.Y;
                var newNode = new TreeNode(MainMenuOffset + new Vector2(x, y), techEntry, this)
                {
                    NodePosition = Cursor
                };
                if (EmpireManager.Player.HasUnlocked(techEntry))
                {
                    newNode.complete = true;
                }
                if (techEntry.Discovered)
                    SubNodes.Add(newNode.tech.UID, newNode);
                PopulateNodesFromSubNode(newNode);
            }
        }

        private void SetNode(TechEntry tech)
        {
            var position = new Vector2(Cursor.X, Cursor.Y);
            bool seatTaken = false;
            foreach (Vector2 v in ClaimedSpots)
            {
                if (v.AlmostEqual(position))
                    seatTaken = true;
            }
            if (seatTaken)
            {
                Cursor.Y = Cursor.Y + 1f;
            }
            else
            {
                ClaimedSpots.Add(position);
            }
            RootNode newNode = new RootNode(MainMenuOffset + new Vector2(ColumnOffset * (int)Cursor.X, RowOffset * ((int)Cursor.Y - 1)), tech)
            {
                NodePosition = Cursor
            };
            if (EmpireManager.Player.HasUnlocked(tech))
            {
                newNode.isResearched = true;
            }
            RootNodes.Add(tech.UID, newNode);
        }

        private void UnlockTree(string key)
        {
            if (!EmpireManager.Player.HasDiscovered(key) || !EmpireManager.Player.HavePreReq(key))
                return;

            /* Fat Bastard: The line below was missing the "if" statement. without the if to check if the tech is unlocked already,
                it will unlock bonus techs again and again, giving you massive amount of bonuses when you press the Ctrl F2.
                I added the same if to the UnlockTreeNoBonus method below 
                This code is called only when cheating and not in a regular game */

            if (!EmpireManager.Player.HasUnlocked(key))
                EmpireManager.Player.UnlockTech(key);

            Technology techd = ResourceManager.TechTree[key];
            foreach (Technology.LeadsToTech tech in techd.LeadsTo)
                UnlockTree(tech.UID);
        }

        private void UnlockTreeNoBonus(string key)
        {
            if (EmpireManager.Player.HasDiscovered(key) && EmpireManager.Player.HavePreReq(key))
            {
                Technology t = ResourceManager.TechTree[key];
                if (t.BonusUnlocked.Count == 0 && !EmpireManager.Player.HasUnlocked(key))
                    EmpireManager.Player.UnlockTech(key);
                foreach (Technology.LeadsToTech tech in t.LeadsTo)
                    UnlockTreeNoBonus(tech.UID);
            }
        }

        //Added by McShooterz: find size of tech tree before it is built
        private static int CalculateTreeDimensionsFromRoot(string UID, ref int rows, int cols, int colmax)
        {
            int max = 0;
            int rowCount = 0;
            cols++;
            if (cols > colmax)
                colmax = cols;
            Technology technology = ResourceManager.TechTree[UID];
            if (technology.LeadsTo.Count > 1)
            {
                foreach (Technology.LeadsToTech tech in technology.LeadsTo)
                {
                    if (EmpireManager.Player.HasDiscovered(tech.UID))
                        rowCount++;
                }
                if (rowCount > 1)
                    rows += rowCount - 1;
            }
            foreach (Technology.LeadsToTech tech in technology.LeadsTo)
            {
                if (EmpireManager.Player.HasDiscovered(tech.UID))
                {
                    max = CalculateTreeDimensionsFromRoot(tech.UID, ref rows, cols, colmax);
                    if (max > colmax)
                        colmax = max;
                }
                else
                {
                    CalculateTreeDimensionsFromRoot(tech.UID, ref rows, cols, colmax);
                }
            }
            return colmax;
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            ResearchScreenNew clickTimer = this;
            clickTimer.ClickTimer = clickTimer.ClickTimer + elapsedTime;
            var tech = qcomponent?.CurrentResearch;
            if (tech != null && tech.Node.tech.Progress >= tech.Node.tech.TechCost)
                qcomponent.CurrentResearch.Node.complete = true;
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public class UnlockItem
        {
            public string Type;

            public string HullUnlocked;

            public string privateName;

            public Building building;

            public ShipModule module;

            public Troop troop;

            public string Description;
        }
    }
}