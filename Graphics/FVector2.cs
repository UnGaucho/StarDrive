using System;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    /// <summary>
    /// This is equivalent to XNA Vector2
    /// Just using the F prefix here to differentiate.
    /// </summary>
    public struct FVector2
    {
        public float X;
        public float Y;

        public FVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator FVector2(in Vector2 xnaVector)
        {
            return new FVector2(xnaVector.X, xnaVector.Y);
        }
    }
}
