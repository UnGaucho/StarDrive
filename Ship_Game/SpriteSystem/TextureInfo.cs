using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Texture;

namespace Ship_Game.SpriteSystem
{
    public class TextureInfo
    {
        public string Name;
        public string Type; // xnb, png, dds, ...
        public int X, Y;
        public int Width;
        public int Height;
        public Texture2D Texture;
        public bool NoPack; // This texture should not be packed

        public override string ToString() => $"X:{X} Y:{Y} W:{Width} H:{Height} Name:{Name} Type:{Type} Format:{Texture?.Format.ToString() ?? ""}";

        // @note this will destroy Texture after transferring it to atlas
        public void TransferTextureToAtlas(Color[] atlas, int atlasWidth, int atlasHeight)
        {
            if (Texture == null)
            {
                Log.Error($"TextureData Texture2D ref already disposed: {Name}.{Type}. "
                          +"Filling atlas rectangle with RED.");
                ImageUtils.FillPixels(atlas, atlasWidth, atlasHeight, X, Y, Color.Red, Width, Height);
                return;
            }

            Color[] colorData;
            SurfaceFormat format = Texture.Format;
            if (format == SurfaceFormat.Dxt5)
            {
                colorData = ImageUtils.DecompressDxt5(Texture);
            }
            else if (format == SurfaceFormat.Dxt1)
            {
                colorData = ImageUtils.DecompressDxt1(Texture);
            }
            else if (format == SurfaceFormat.Color)
            {
                colorData = new Color[Texture.Width * Texture.Height];
                Texture.GetData(colorData);
            }
            else
            {
                Log.Error($"Unsupported format '{format}' from texture '{Name}.{Type}': "
                          +"Ensure you are using RGBA32 textures. Filling atlas rectangle with RED.");
                ImageUtils.FillPixels(atlas, atlasWidth, atlasHeight, X, Y, Color.Red, Width, Height);
                return;
            }

            ImageUtils.CopyPixelsWithPadding(atlas, atlasWidth, atlasHeight, X, Y, colorData, Width, Height);
        }

        public void DisposeTexture()
        {
            Texture.Dispose(); // save some memory
            Texture = null;
        }

        public void SaveAsPng(string filename)
        {
            Texture.Save(filename, ImageFileFormat.Png);
        }

        public void SaveAsDds(string filename)
        {
            SurfaceFormat format = Texture.Format;
            if (format == SurfaceFormat.Dxt5 || format == SurfaceFormat.Dxt1)
            {
                Texture.Save(filename, ImageFileFormat.Dds); // already compressed
            }
            else if (format == SurfaceFormat.Color)
            {
                var colorData = new Color[Texture.Width * Texture.Height];
                Texture.GetData(colorData);
                ImageUtils.SaveAsDds(filename, Width, Height, colorData);
            }
            else
            {
                Log.Error($"Unsupported texture format: {Texture.Format}");
            }
        }
    }
}
