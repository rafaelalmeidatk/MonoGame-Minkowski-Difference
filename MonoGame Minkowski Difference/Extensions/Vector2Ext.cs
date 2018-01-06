using Microsoft.Xna.Framework;

namespace MonoGame_Minkowski_Difference.Extensions
{
    public static class Vector2Ext
    {
        public static float Cross(this Vector2 self, Vector2 other)
        {
            return self.X * other.Y - self.Y * other.X;
        }

        public static Vector2 Tangent(this Vector2 self)
        {
            return new Vector2(-self.Y, self.X);
        }
    }
}
