using System;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    /// <summary>
    /// This is equivalent to XNA Color class
    /// Using F prefix here to differentiate
    /// </summary>
    public struct FColor
    {
        /// <summary>
        /// TODO: Currently just reuse XNA implementation to save some time
        /// but eventually should replace with a custom implementation
        /// </summary>
        public Color XnaColor;

        public FColor(Color xnaColor)
        {
            XnaColor = xnaColor;
        }

        public static implicit operator FColor(Color xnaColor)
        {
            return new FColor(xnaColor);
        }
    }
}
