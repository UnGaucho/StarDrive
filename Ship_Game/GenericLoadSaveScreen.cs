﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Ship_Game
{
    public class GenericLoadSaveScreen : GameScreen, IDisposable
    {
        protected Vector2 Cursor = Vector2.Zero;

        //private UniverseScreen screen;

        protected List<UIButton> Buttons = new List<UIButton>();

        //private Submenu subSave;

        protected Rectangle Window;

        protected Menu1 SaveMenu;

        protected Submenu NameSave;

        protected Submenu AllSaves;

        protected Vector2 TitlePosition;

        protected Vector2 EnternamePos;

        protected UITextEntry EnterNameArea;

        protected ScrollList SavesSL;

        protected UIButton DoBtn;

        public enum SLMode { Load, Save };

        protected SLMode mode;

        protected string InitText = "";

        protected string TitleText = "";

        protected string OverWriteText = "";

        protected CloseButton close;

        protected MouseState currentMouse;

        protected MouseState previousMouse;

        protected Selector selector;

        //private float transitionElapsedTime;

        //adding for thread safe Dispose because class uses unmanaged resources 
        protected bool disposed;

        public GenericLoadSaveScreen(SLMode mode, string InitText, string TitleText, string OverWriteText)
        {
            //this.screen = screen;
            this.mode = mode;
            this.InitText = InitText;
            this.TitleText = TitleText;
            this.OverWriteText = OverWriteText;
            base.IsPopup = true;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GenericLoadSaveScreen() { Dispose(false); }

        protected void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.SavesSL != null)
                        this.SavesSL.Dispose();

                }
                this.SavesSL = null;
                this.disposed = true;
            }
        }

        public virtual void DoSave()
        {
            //SavedGame savedGame = new SavedGame(this.screen, this.EnterNameArea.Text);
            //this.ExitScreen();
        }

        protected virtual FileHeader GetFileHeader(ScrollList.Entry e)  // Overriddem in subclasses to display information
        {
            FileHeader data = new FileHeader();

            return data;
        }

        public override void Draw(GameTime gameTime)
        {
            base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
            base.ScreenManager.SpriteBatch.Begin();
            this.SaveMenu.Draw();
            this.NameSave.Draw();
            this.AllSaves.Draw();
            Vector2 bCursor = new Vector2((float)(this.AllSaves.Menu.X + 20), (float)(this.AllSaves.Menu.Y + 20));
            for (int i = this.SavesSL.indexAtTop; i < this.SavesSL.Entries.Count && i < this.SavesSL.indexAtTop + this.SavesSL.entriesToDisplay; i++)
            {
                ScrollList.Entry e = this.SavesSL.Entries[i];
                //HeaderData data = e.item as HeaderData;
                FileHeader data = this.GetFileHeader(e);
                bCursor.Y = (float)e.clickRect.Y;
                base.ScreenManager.SpriteBatch.Draw(data.icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, data.FileName, tCursor, Color.Orange);
                tCursor.Y = tCursor.Y + (float)Fonts.Arial20Bold.LineSpacing;
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, data.Info, tCursor, Color.White);
                tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, data.ExtraInfo, tCursor, Color.White);
            }
            this.SavesSL.Draw(base.ScreenManager.SpriteBatch);
            this.EnterNameArea.Draw(Fonts.Arial12Bold, base.ScreenManager.SpriteBatch, this.EnternamePos, gameTime, (this.EnterNameArea.Hover ? Color.White : Color.Orange));
            foreach (UIButton b in this.Buttons)
            {
                b.Draw(base.ScreenManager.SpriteBatch);
            }
            if (this.selector != null)
            {
                this.selector.Draw();
            }
            this.close.Draw(base.ScreenManager);
            base.ScreenManager.SpriteBatch.End();
        }

        public override void ExitScreen()
        {
            base.ExitScreen();
        }


        protected virtual void Load()
        {

        }


        protected virtual void SwitchFile(ScrollList.Entry e)
        {

        }


        public override void HandleInput(InputState input)
        {
            this.currentMouse = input.CurrentMouseState;
            Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
            this.SavesSL.HandleInput(input);
            this.selector = null;
            for (int i = this.SavesSL.indexAtTop; i < this.SavesSL.Entries.Count && i < this.SavesSL.indexAtTop + this.SavesSL.entriesToDisplay; i++)
            {
                ScrollList.Entry e = this.SavesSL.Entries[i];
                if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos))
                {
                    e.clickRectHover = 0;
                }
                else
                {
                    if (e.clickRectHover == 0)
                    {
                        AudioManager.PlayCue("sd_ui_mouseover");
                    }
                    e.clickRectHover = 1;
                    this.selector = new Selector(base.ScreenManager, e.clickRect);
                    if (input.InGameSelect)
                    {
                        this.SwitchFile(e);
                    }
                }
            }
            if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input))
            {
                this.ExitScreen();
            }
            foreach (UIButton b in this.Buttons)
            {
                if (!HelperFunctions.CheckIntersection(b.Rect, MousePos))
                {
                    b.State = UIButton.PressState.Normal;
                }
                else
                {
                    b.State = UIButton.PressState.Hover;
                    if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
                    {
                        b.State = UIButton.PressState.Pressed;
                    }
                    if (this.currentMouse.LeftButton != ButtonState.Released || this.previousMouse.LeftButton != ButtonState.Pressed)
                    {
                        continue;
                    }
                    string text = b.Launches;
                    if (text == null)
                    {
                        continue;
                    }
                    if (text == "DoBtn")
                    {
                        AudioManager.PlayCue("echo_affirm");
                        if(mode == SLMode.Save)
                            this.TrySave();
                        else if (mode == SLMode.Load)
                            this.Load();
                    }
                }
            }
            if (SLMode.Save == this.mode)       // Only check when saving
            {
                if (!HelperFunctions.CheckIntersection(this.EnterNameArea.ClickableArea, MousePos))
                {
                    this.EnterNameArea.Hover = false;
                }
                else
                {
                    this.EnterNameArea.Hover = true;
                    if (this.currentMouse.LeftButton == ButtonState.Released && this.previousMouse.LeftButton == ButtonState.Pressed)
                    {
                        this.EnterNameArea.HandlingInput = true;
                        this.EnterNameArea.Text = "";
                    }
                }
                if (this.EnterNameArea.HandlingInput)
                {
                    this.EnterNameArea.HandleTextInput(ref this.EnterNameArea.Text);
                    if (input.CurrentKeyboardState.IsKeyDown(Keys.Enter))
                    {
                        this.EnterNameArea.HandlingInput = false;
                    }
                }
            }
            this.previousMouse = input.LastMouseState;
            base.HandleInput(input);
        }

        protected virtual void SetSavesSL()        // To be overridden in subclasses
        {

        }

        public override void LoadContent()
        {
            this.Window = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 300, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 600, 600);
            this.SaveMenu = new Menu1(base.ScreenManager, this.Window);
            this.close = new CloseButton(new Rectangle(this.Window.X + this.Window.Width - 35, this.Window.Y + 10, 20, 20));
            Rectangle sub = new Rectangle(this.Window.X + 20, this.Window.Y + 20, this.Window.Width - 40, 80);
            this.NameSave = new Submenu(base.ScreenManager, sub);
            this.NameSave.AddTab(this.TitleText);
            Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
            this.TitlePosition = new Vector2((float)(sub.X + 20), (float)(sub.Y + 45));
            Rectangle scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, this.Window.Height - sub.Height - 50);
            this.AllSaves = new Submenu(base.ScreenManager, scrollList);
            this.AllSaves.AddTab("All Saves");
            this.SavesSL = new ScrollList(this.AllSaves, 55);

            this.SetSavesSL();

            this.SavesSL.indexAtTop = 0;
            this.EnternamePos = this.TitlePosition;
            this.EnterNameArea = new UITextEntry();

            EnterNameArea.Text = this.InitText;
            EnterNameArea.ClickableArea = new Rectangle((int)this.EnternamePos.X, (int)this.EnternamePos.Y - 2, (int)Fonts.Arial20Bold.MeasureString(this.EnterNameArea.Text).X + 20, Fonts.Arial20Bold.LineSpacing);

            this.DoBtn = new UIButton()
            {
                Rect = new Rectangle(sub.X + sub.Width - 88, this.EnterNameArea.ClickableArea.Y - 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
                NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
                HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
                Text = (mode == SLMode.Save ? "Save" : "Load"),
                Launches = "DoBtn"
            };
            this.Buttons.Add(this.DoBtn);
            Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height + 15);
            base.LoadContent();
            base.LoadContent();
        }

        private void OverWriteAccepted(object sender, EventArgs e)
        {
            this.DoSave();
        }

        protected virtual bool CheckOverWrite()
        {
            /*foreach (ScrollList.Entry entry in this.SavesSL.Entries)
            {
                if (this.EnterNameArea.Text == (entry.item as string))
                {
                    return false;
                }
            }*/

            return true;
        }

        private void TrySave()
        {
            bool SaveOK = this.CheckOverWrite();
            
            if (SaveOK)
            {
                this.DoSave();
            }
            else
            {
                MessageBoxScreen messageBox = new MessageBoxScreen(this.OverWriteText);
                messageBox.Accepted += new EventHandler<EventArgs>(this.OverWriteAccepted);
                base.ScreenManager.AddScreen(messageBox);
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        protected class FileHeader
        {
            public string FileName;
            public string Info;
            public string ExtraInfo;
            public Texture2D icon;

            public FileHeader()
            {
                this.icon = ResourceManager.TextureDict["ShipIcons/Wisp"];
                this.FileName = "";
                this.Info = "";
                this.ExtraInfo = "";
            }
        }
    }
}