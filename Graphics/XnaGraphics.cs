using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    /// <summary>
    /// Implements XNA Specific Graphics pipeline
    /// </summary>
    public class XnaGraphics : Graphics
    {
        protected GraphicsDevice Device;
        protected SpriteBatch Batch;
        public override bool IsInitialized => Batch != null;

        public XnaGraphics(GraphicsDevice device)
        {
            Device = device;
        }

        public override void ResetDevice()
        {
            Batch = new SpriteBatch(Device);
        }

        public override void Clear()
        {
            Device.Clear(Color.Black);
        }

        public override void Begin()
        {
            Batch.Begin();
        }

        public override void End()
        {
            Batch.End();
        }

        [Conditional("DEBUG")]
        static void CheckSubTextureDisposed(SubTexture texture)
        {
            if (texture.Texture.IsDisposed)
                throw new ObjectDisposedException($"SubTexture '{texture.Name}' in Texture2D '{texture.Texture.Name}'");
        }

        public override void Draw(SubTexture texture, in FRect rect, FColor color)
        {
            CheckSubTextureDisposed(texture);
            var r = new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
            Batch.Draw(texture.Texture, r, texture.Rect, color.XnaColor);
        }
    }
}
