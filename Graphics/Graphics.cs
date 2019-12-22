using System;
using System.Collections.Generic;

namespace Ship_Game
{
    /// <summary>
    /// Encapsulates all graphics related
    /// properties
    /// </summary>
    public abstract class Graphics
    {
        /// <summary>
        /// TRUE if graphics is initialized and in a valid
        /// state for Drawing
        /// </summary>
        public abstract bool IsInitialized { get; }

        protected Graphics()
        {
        }

        /// <summary>
        /// This should reset any device resources
        /// such as SpriteBatches and should be called when the graphics
        /// device itself is reset.
        /// </summary>
        public abstract void ResetDevice();

        /// <summary>
        /// Clears the screen to black to start rendering
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Begin SpriteBatch
        /// </summary>
        public abstract void Begin();

        /// <summary>
        /// Flush and Draw all SpriteBatch items
        /// </summary>
        public abstract void End();

        /// <summary>
        /// Base implementation of Draw
        /// </summary>
        /// <param name="texture">Texture to draw</param>
        /// <param name="rect">Screen rectangle where to draw the texture</param>
        /// <param name="color">Color multiply, default should be White</param>
        public abstract void Draw(SubTexture texture, in FRect rect, FColor color);

        // Draws at screen position using texture Width/Height
        public void Draw(SubTexture texture, FVector2 position, FColor color)
        {
            var rect = new FRect(position.X, position.Y, texture.Width, texture.Height);
            Draw(texture, rect, color);
        }

    }
}
