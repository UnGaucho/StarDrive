using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class ResearchQueueUIComponent : UIPanel
    {
        readonly ResearchScreenNew Screen;

        readonly Submenu CurrentResearchPanel;
        readonly Submenu ResearchQueuePanel;
        readonly UIPanel TimeLeft;
        readonly UILabel TimeLeftLabel;

        ResearchQItem CurrentResearch;
        readonly ScrollList QSL;
        readonly UIButton BtnShowQueue;

        public ResearchQueueUIComponent(ResearchScreenNew screen, in Rectangle container)  : base(screen, container, Color.Black)
        {
            Screen = screen;

            BtnShowQueue = Button(ButtonStyle.DanButtonBlue, 
                new Vector2(container.Right - 192, screen.ScreenHeight - 55), "", OnBtnShowQueuePressed);
            BtnShowQueue.TextAlign = ButtonTextAlign.Left;

            var current = new Rectangle(container.X, container.Y, container.Width, 150);
            var timeLeftRect = new Rectangle(current.X + current.Width - 119, current.Y + current.Height - 24, 111, 20);
            TimeLeft = Panel(timeLeftRect, Color.White, ResourceManager.Texture("ResearchMenu/timeleft"));
            
            var labelPos = new Vector2(TimeLeft.X + 26,
                                       TimeLeft.Y + TimeLeft.Height / 2 - Fonts.Verdana14Bold.LineSpacing / 2);
            TimeLeftLabel = TimeLeft.Label(labelPos, "", Fonts.Verdana14Bold, new Color(205, 229, 255));

            CurrentResearchPanel = Add(new Submenu(current, SubmenuStyle.Blue));
            CurrentResearchPanel.AddTab(Localizer.Token(1405));
            
            var queue = new Rectangle(current.X, current.Y + 165, container.Width, container.Height - 165);
            ResearchQueuePanel = Add(new Submenu(queue, SubmenuStyle.Blue));
            ResearchQueuePanel.AddTab(Localizer.Token(1404));

            QSL = Add(new ScrollList(ResearchQueuePanel, 125, ListControls.All, ListStyle.Blue) { AutoManageItems = true });
            ReloadResearchQueue();
        }

        void OnBtnShowQueuePressed(UIButton button)
        {
            SetQueueVisible(!QSL.Visible);
        }

        void SetQueueVisible(bool visible)
        {
            BtnShowQueue.Visible = (CurrentResearch != null);

            if (CurrentResearch != null)
            {
                TimeLeft.Visible = visible;
                CurrentResearch.Visible = visible;
            }
            else
            {
                TimeLeft.Visible = false;
            }

            QSL.Visible = visible;
            CurrentResearchPanel.Visible = visible;
            ResearchQueuePanel.Visible = visible;
            BtnShowQueue.Text = Localizer.Token(QSL.Visible ? 2136 : 2135);
        }

        public override bool HandleInput(InputState input)
        {
            if ((QSL.Visible && input.RightMouseClick && ResearchQueuePanel.HitTest(input.CursorPosition))
                || input.Escaped)
            {
                Screen.ExitScreen();
                return true;
            }

            if (CurrentResearch != null && CurrentResearch.HandleInput(input))
                return true;

            return base.HandleInput(input);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
        }
        
        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            if (QSL.Visible && CurrentResearch != null)
            {
                CurrentResearch.Draw(batch);

                float remaining = CurrentResearch.Tech.TechCost - CurrentResearch.Tech.Progress;
                float numTurns = (float)Math.Ceiling(remaining / (0.01f + EmpireManager.Player.Research.NetResearch));
                TimeLeftLabel.Text = (numTurns > 999f) ? ">999 turns" : numTurns.String(0)+" turns";
            }
        }

        ResearchQItem CreateQueueItem(TreeNode node)
        {
            var defaultPos = new Vector2(CurrentResearchPanel.X + 5, CurrentResearchPanel.Y + 30);
            return new ResearchQItem(Screen, node, defaultPos);
        }

        public void AddToResearchQueue(TreeNode node)
        {
            if (EmpireManager.Player.Research.AddToQueue(node.Entry.UID))
            {
                if (CurrentResearch == null)
                    CurrentResearch = CreateQueueItem(node);
                else
                    QSL.AddItem(CreateQueueItem(node));

                SetQueueVisible(true);
            }
        }

        public void ReloadResearchQueue()
        {
            CurrentResearch = EmpireManager.Player.Research.HasTopic
                            ? CreateQueueItem((TreeNode)Screen.AllTechNodes[EmpireManager.Player.Research.Topic])
                            : null;

            Array<string> techIds = EmpireManager.Player.Research.Queue;
            var items = new Array<ResearchQItem>();
            for (int i = 1; i < techIds.Count; ++i)
            {
                items.Add(CreateQueueItem((TreeNode)Screen.AllTechNodes[techIds[i]]));
            }
            QSL.SetItems(items);

            SetQueueVisible(EmpireManager.Player.Research.HasTopic);
        }
    }
}