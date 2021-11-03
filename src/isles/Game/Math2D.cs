// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Xna.Framework;

namespace Isles.Engine
{
    /// <summary>
    /// Helper class for 2D math and geometries
    ///
    /// Thanks for the code from Mat Buckland (fup@ai-junkie.com).
    /// </summary>
    public static class Math2D
    {
        /// <summary>
        /// Default epsilion used all over Math2D.
        /// </summary>
        public const float Epsilon = float.Epsilon;

        /// <summary>
        /// Test to see if two float equals using epslion.
        /// </summary>
        public static bool FloatEquals(float n1, float n2)
        {
            return ((n1 - n2) < Epsilon) && ((n2 - n1) < Epsilon);
        }

        /// <summary>
        /// Transform a world point p to local space specified by position and rotation.
        /// </summary>
        public static Vector2 WorldToLocal(Vector2 p, Vector2 translation, float rotation)
        {
            Vector2 v;

            // Apply translation
            p -= translation;

            // Apply rotation
            var sin = (float)Math.Sin(-rotation);
            var cos = (float)Math.Cos(-rotation);

            v.X = p.X * cos - p.Y * sin;
            v.Y = p.X * sin + p.Y * cos;

            return v;
        }

        /// <summary>
        /// Transform a local point p to world space specified by position and rotation.
        /// </summary>
        public static Vector2 LocalToWorld(Vector2 p, Vector2 translation, float rotation)
        {
            Vector2 v;

            // Apply rotation
            var sin = (float)Math.Sin(rotation);
            var cos = (float)Math.Cos(rotation);

            v.X = p.X * cos - p.Y * sin;
            v.Y = p.X * sin + p.Y * cos;

            // Apply translation
            return v + translation;
        }

        /// <summary>
        /// Gets the nearest distance from point P to the specified rectangle.
        /// </summary>
        public static float DistanceToRectangle(Vector2 min, Vector2 max,
                                                Vector2 translation, float rotation, Vector2 pWorld)
        {
            Vector2 p = WorldToLocal(pWorld, translation, rotation);

            if (p.X > max.X)
            {
                if (p.Y > max.Y)
                {
                    Vector2 edge = p - max;
                    return edge.Length();
                }

                if (p.Y < min.Y)
                {
                    Vector2 edge;
                    edge.X = max.X;
                    edge.Y = min.Y;
                    edge -= p;
                    return edge.Length();
                }

                return p.X - max.X;
            }

            if (p.X < min.X)
            {
                if (p.Y > max.Y)
                {
                    Vector2 edge;
                    edge.X = min.X;
                    edge.Y = max.Y;
                    edge -= p;
                    return edge.Length();
                }

                if (p.Y < min.Y)
                {
                    Vector2 edge = min - p;
                    return edge.Length();
                }

                return min.X - p.X;
            }

            if (p.Y > max.Y)
            {
                return p.Y - max.Y;
            }

            if (p.Y < min.Y)
            {
                return min.Y - p.Y;
            }

            // Inside the rectangle
            return 0;
        }

        /// <summary>
        /// Tests to see if a point is in a circle.
        /// </summary>
        public static bool PointInCircle(Vector2 p, Vector2 c, float r)
        {
            return Vector2.DistanceSquared(p, c) < r * r;
        }

        /// <summary>
        /// Tests to see if a rectangle contains a point.
        /// Note that min should be smaller than max.
        /// </summary>
        public static bool PointInRectangle(
            Vector2 p, Vector2 min, Vector2 max)
        {
            return p.X > min.X && p.X < max.X && p.Y > min.Y && p.Y < max.Y;
        }

        /// <summary>
        /// Tests to see if a rectangle contains a point.
        /// v1 and v2 are in local space relative to position and rotation.
        /// </summary>
        public static bool PointInRectangle(
            Vector2 p, Vector2 min, Vector2 max, Vector2 position, float rotation)
        {
            // Transform p to local space
            Vector2 pLocal = WorldToLocal(p, position, rotation);

            return PointInRectangle(pLocal, min, max);
        }
    }

    public enum OutlineType
    {
        Empty,
        Circle,
        Rectangle,
    }

    /// <summary>
    /// Represents a 2D shape that you can do collision detection.
    /// </summary>
    public sealed class Outline
    {
        /// <summary>
        /// Gets the outline type.
        /// </summary>
        public OutlineType Type { get; private set; }

        /// <summary>
        /// Gets the position of the outline.
        /// </summary>
        public Vector2 Position => position;

        private Vector2 position;

        /// <summary>
        /// Gets the radius of the circle.
        /// </summary>
        public float Radius { get; private set; }

        /// <summary>
        /// Gets the rotation of the rectangle, in radius.
        /// </summary>
        public float Rotation { get; private set; }

        /// <summary>
        /// Gets the min point of the rectangle.
        /// </summary>
        public Vector2 Min => min;

        private Vector2 min;

        /// <summary>
        /// Gets the min point of the rectangle.
        /// </summary>
        public Vector2 Max => max;

        private Vector2 max;

        /// <summary>
        /// Gets the area of the outline.
        /// </summary>
        public float Area
        {
            get
            {
                if (Type == OutlineType.Circle)
                {
                    return (float)Math.PI * Radius * Radius;
                }
                else if (Type == OutlineType.Rectangle)
                {
                    Vector2 c = max - min;
                    return c.X * c.Y;
                }
                else
                {
                    return 0;
                }
            }
        }

        public Outline()
        {
        }

        /// <summary>
        /// Creates a new circle outline.
        /// </summary>
        public Outline(Vector2 position, float radius)
        {
            SetCircle(position, radius);
        }

        /// <summary>
        /// Creates a new rectangle outline.
        /// </summary>
        public Outline(Vector2 min, Vector2 max, Vector2 position, float rotation)
        {
            SetRectangle(min, max, position, rotation);
        }

        /// <summary>
        /// Setup a circle outline.
        /// </summary>
        public void SetCircle(Vector2 position, float radius)
        {
            Type = OutlineType.Circle;
            this.position = position;
            Radius = radius;
        }

        /// <summary>
        /// Setup a rectangle outline.
        /// </summary>
        public void SetRectangle(Vector2 min, Vector2 max, Vector2 position, float rotation)
        {
            Type = OutlineType.Rectangle;
            this.min = min;
            this.max = max;
            this.position = position;
            Rotation = rotation;
        }

        /// <summary>
        /// Tests to see if the outline intersects with the specified point.
        /// </summary>
        public bool Overlaps(Vector2 point)
        {
            if (Type == OutlineType.Empty)
            {
                return false;
            }

            if (Type == OutlineType.Circle)
            {
                return Math2D.PointInCircle(point, position, Radius);
            }

            return Type == OutlineType.Rectangle && Math2D.PointInRectangle(point, min, max, position, Rotation);
        }

        /// <summary>
        /// Gets the distance from the outline to the point.
        /// </summary>
        public float DistanceTo(Vector2 point)
        {
            if (Type == OutlineType.Empty)
            {
                return 0;
            }

            if (Type == OutlineType.Circle)
            {
                var distance = Vector2.Subtract(point, position).Length() - Radius;
                return distance < 0 ? 0 : distance;
            }

            return Type == OutlineType.Rectangle ? Math2D.DistanceToRectangle(min, max, position, Rotation, point) : 0;
        }

        /// <summary>
        /// To override the operator "*". Enlarge the outline by scaler n.
        /// </summary>
        public static Outline operator *(Outline t, float n)
        {
            Outline outlineRet;
            if (t.Type == OutlineType.Circle)
            {
                outlineRet = new Outline(t.position, t.Radius * n);
            }
            else
            {
                outlineRet = t.Type == OutlineType.Rectangle ? new Outline(t.min * n, t.max * n, t.position, t.Rotation) : null;
            }

            return outlineRet;
        }

        /// <summary>
        /// Enlarge the outline using addition.
        /// </summary>
        public static Outline operator +(Outline outline, float n)
        {
            if (outline.Type == OutlineType.Circle)
            {
                return new Outline(outline.position, outline.Radius + n);
            }

            if (outline.Type == OutlineType.Rectangle)
            {
                Vector2 min, max;

                min.X = outline.min.X - n;
                min.Y = outline.min.Y - n;
                max.X = outline.max.X + n;
                max.Y = outline.max.Y + n;

                if (min.X > max.X)
                {
                    min.X = max.X;
                }

                if (min.Y > max.Y)
                {
                    min.Y = max.Y;
                }

                return new Outline(min, max, outline.position, outline.Rotation);
            }

            return new Outline();
        }
    }
}
