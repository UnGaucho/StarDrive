﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    // A simplified dummy game setup
    // for minimal testing
    public class GameDummy : Game
    {
        public GraphicsDeviceManager Graphics;
        public new GameContentManager Content { get; }

        public GameDummy(int width=800, int height=600, bool show=false)
        {
            base.Content = Content = new GameContentManager(Services, "Game");

            Graphics = new GraphicsDeviceManager(this)
            {
                MinimumPixelShaderProfile = ShaderProfile.PS_2_0,
                MinimumVertexShaderProfile = ShaderProfile.VS_2_0,
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
                PreferMultiSampling = false
            };

            Graphics.PreferredBackBufferWidth = width;
            Graphics.PreferredBackBufferHeight = height;
            Graphics.SynchronizeWithVerticalRetrace = false;
            if (Graphics.IsFullScreen)
                Graphics.ToggleFullScreen();
            Graphics.ApplyChanges();

            var form = (Form)Control.FromHandle(Window.Handle);
            form.Visible = show;
        }

        public void Create()
        {
            var manager = Services.GetService(typeof(IGraphicsDeviceManager)) as IGraphicsDeviceManager;
            manager?.CreateDevice();
            base.Initialize();
        }
    }
}