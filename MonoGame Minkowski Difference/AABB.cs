using Microsoft.Xna.Framework;
using MonoGame_Minkowski_Difference.Extensions;
using System;

namespace MonoGame_Minkowski_Difference
{
    public class AABB
    {
        public Vector2 Center;
        public Vector2 Extents { get; set; }

        public Vector2 Min => new Vector2(Center.X - Extents.X, Center.Y - Extents.Y);
        public Vector2 Max => new Vector2(Center.X + Extents.X, Center.Y + Extents.Y);
        public Vector2 Size => Extents * 2;

        public Vector2 Velocity;
        public Vector2 Acceleration;

        public AABB(Vector2 center, Vector2 extent, Vector2 ?velocity, Vector2 ?acceleration)
        {
            Center = center;
            Extents = extent;
            Velocity = velocity ?? Vector2.Zero;
            Acceleration = acceleration ?? Vector2.Zero;
        }

        public AABB(Vector2 center, Vector2 extent) : this(center, extent, null, null) { }

        public AABB MinkowskiDifference(AABB other)
        {
            var topLeft = Min - other.Max;
            var fullSize = Size + other.Size;
            return new AABB(topLeft + fullSize / 2, fullSize / 2);
        }

        public Vector2 ClosestPointOnBoundsToPoint(Vector2 point)
        {
            var minDist = Math.Abs(point.X - Min.X);
            var boundsPoint = new Vector2(Min.X, point.Y);
            if (Math.Abs(Max.X - point.X) < minDist)
            {
                minDist = Math.Abs(Max.X - point.X);
                boundsPoint = new Vector2(Max.X, point.Y);
            }
            if (Math.Abs(Max.Y - point.Y) < minDist)
            {
                minDist = Math.Abs(Max.Y - point.Y);
                boundsPoint = new Vector2(point.X, Max.Y);
            }
            if (Math.Abs(Min.Y - point.Y) < minDist)
            {
                minDist = Math.Abs(Min.Y - point.Y);
                boundsPoint = new Vector2(point.X, Min.Y);
            }
            return boundsPoint;
        }

        private float GetRayIntersectionFractionOfFirstRay(Vector2 originA, Vector2 endA, Vector2 originB, Vector2 endB)
        {
            var r = endA - originA;
            var s = endB - originB;
            
            var numerator = (originB - originA).Cross(r);
            var denominator = r.Cross(s);

            if (Math.Abs(numerator) < 0.001 && Math.Abs(denominator) < 0.001)
            {
                // the lines are co-linear
                // todo: calculate intersection point
                return float.PositiveInfinity;
            }
            if (Math.Abs(denominator) < 0.001)
            {
                // lines are parellel
                return float.PositiveInfinity;
            }

            var u = numerator / denominator;
            var t = (originB - originA).Cross(s) / denominator;
            if ((t >= 0) && (t <= 1) && (u >= 0) && (u <= 1))
            {
                return t;
            }

            return float.PositiveInfinity;
        }

        public float GetRayIntersectionFraction(Vector2 origin, Vector2 direction)
        {
            var end = origin + direction;

            // for each of the AABB's four edges
            // calculate the minimum fraction of "direction"
            // in order to find where the ray FIRST itnersects
            // the AABB (if it ever does)
            var minT = GetRayIntersectionFractionOfFirstRay(origin, end, new Vector2(Min.X, Min.Y), new Vector2(Min.X, Max.Y));

            var x = GetRayIntersectionFractionOfFirstRay(origin, end, new Vector2(Min.X, Max.Y), new Vector2(Max.X, Max.Y));
            if (x < minT)
                minT = x;

            x = GetRayIntersectionFractionOfFirstRay(origin, end, new Vector2(Max.X, Max.Y), new Vector2(Max.X, Min.Y));
            if (x < minT)
                minT = x;

            x = GetRayIntersectionFractionOfFirstRay(origin, end, new Vector2(Max.X, Min.Y), new Vector2(Min.X, Min.Y));
            if (x < minT)
                minT = x;

            return minT;
        }
    }
}
