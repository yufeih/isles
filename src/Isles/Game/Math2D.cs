//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;

namespace Isles.Engine
{
    #region Math2D
    /// <summary>
    /// Helper class for 2D math and geometries
    /// 
    /// Thanks for the code from Mat Buckland (fup@ai-junkie.com)
    /// </summary>
    public static class Math2D
    {
        #region 2D Geometry
        /// <summary>
        /// Default epsilion used all over Math2D
        /// </summary>
        public const float Epsilon = float.Epsilon;

        /// <summary>
        /// Test to see if two float equals using epslion
        /// </summary>
        public static bool FloatEquals(float n1, float n2)
        {
            return ((n1 - n2) < Epsilon) && ((n2 - n1) < Epsilon);
        }

        /// <summary>
        /// Test to see if a float equals zero using epslion
        /// </summary>
        public static bool FloatEqualsZero(float n)
        {
            return (n < Epsilon) && (n > -Epsilon);
        }

        /// <summary>
        /// Transform a world point p to local space specified by position and rotation
        /// </summary>
        /// <returns></returns>
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
        /// Transform a local point p to world space specified by position and rotation
        /// </summary>
        /// <returns></returns>
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
        /// given a plane and a ray this function determins how far along the ray 
        /// an interestion occurs. Returns null if the ray is parallel
        /// </summary>
        public static float? RayPlaneIntersects(
            Vector2 rayOrigin, Vector2 rayDirection, Vector2 planePoint, Vector2 planeNormal)
        {
            var d = Vector2.Dot(planeNormal, planePoint);
            var numer = Vector2.Dot(planeNormal, rayOrigin) - d;
            var denom = Vector2.Dot(planeNormal, rayDirection);

            // Normal is parallel to vector
            return FloatEqualsZero(denom) ? null : (float?)-(numer / denom);
        }

        /// <summary>
        /// Span relation
        /// </summary>
        public enum SpanType
        {
            Local, Front, Back
        }

        /// <summary>
        /// Gets the relation between a point and a plane
        /// </summary>
        public static SpanType PointLineRelation(
            Vector2 point, Vector2 planePoint, Vector2 planeNormal)
        {
            var d = Vector2.Dot(planeNormal, planePoint - point);

            if (d < -Epsilon)
            {
                return SpanType.Front;
            }

            return d > Epsilon ? SpanType.Back : SpanType.Local;
        }

        /// <summary>
        /// Test to see if a ray intersects a circle
        /// </summary>
        public static float? RayCircleIntersectionTest(
            Vector2 rayOrigin, Vector2 rayHeading, Vector2 circle, float radius)
        {
            Vector2 toCircle = circle - rayOrigin;

            var length = toCircle.Length();
            var v = Vector2.Dot(toCircle, rayHeading);
            var d = radius * radius - (length * length - v * v);

            // No intersection, return null
            return d < 0 ? null : (float?)(v - (float)Math.Sqrt(d));
        }

        /// <summary>
        /// Whether a ray intersects a circle
        /// </summary>
        public static bool RayCircleIntersects(
            Vector2 rayOrigin, Vector2 rayHeading, Vector2 circle, float radius)
        {
            Vector2 toCircle = circle - rayOrigin;

            var length = toCircle.Length();
            var v = Vector2.Dot(toCircle, rayHeading);
            var d = radius * radius - (length * length - v * v);

            return d < 0;
        }

        /// <summary>
        /// Given a point P and a circle of radius R centered at C this function
        /// determines the two points on the circle that intersect with the 
        /// tangents from P to the circle. Returns false if P is within the circle.
        /// thanks to Dave Eberly for this one.
        /// </summary>
        public static bool GetTangentPoints(
            Vector2 C, float R, Vector2 P, ref Vector2 T1, ref Vector2 T2)
        {
            Vector2 PmC = P - C;

            var sqrLen = PmC.LengthSquared();
            var rSqr = R * R;

            // P is inside or on the circle
            if (sqrLen <= rSqr)
            {
                return false;
            }

            var InvSqrLen = 1.0f / sqrLen;
            var Root = (float)Math.Sqrt(Math.Abs(sqrLen - rSqr));

            T1.X = C.X + R * (R * PmC.X - PmC.Y * Root) * InvSqrLen;
            T1.Y = C.Y + R * (R * PmC.Y + PmC.X * Root) * InvSqrLen;
            T2.X = C.X + R * (R * PmC.X + PmC.Y * Root) * InvSqrLen;
            T2.Y = C.Y + R * (R * PmC.Y - PmC.X * Root) * InvSqrLen;

            return true;
        }

        /// <summary>
        /// given a line segment AB and a point P, this function returns the
        /// shortest distance between a point on AB and P.
        /// </summary>
        /// <returns></returns>
        public static float DistanceToLineSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            //if the angle is obtuse between PA and AB is obtuse then the closest
            //vertex must be a
            var dotA = (p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y);

            if (dotA <= 0)
            {
                return Vector2.Distance(a, p);
            }

            //if the angle is obtuse between PB and AB is obtuse then the closest
            //vertex must be b
            var dotB = (p.X - b.X) * (a.X - b.X) + (p.Y - b.Y) * (a.Y - b.Y);

            if (dotB <= 0)
            {
                return Vector2.Distance(b, p);
            }

            //calculate the point along AB that is the closest to p
            Vector2 Point = a + ((b - a) * dotA) / (dotA + dotB);

            //calculate the distance p-Point
            return Vector2.Distance(p, Point);
        }

        /// <summary>
        /// given a line segment AB and a point P, this function returns the
        /// shortest distance squared between a point on AB and P.
        /// </summary>
        /// <returns></returns>
        public static float DistanceToLineSegmentSquared(Vector2 a, Vector2 b, Vector2 p)
        {
            //if the angle is obtuse between PA and AB is obtuse then the closest
            //vertex must be a
            var dotA = (p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y);

            if (dotA <= 0)
            {
                return Vector2.DistanceSquared(a, p);
            }

            //if the angle is obtuse between PB and AB is obtuse then the closest
            //vertex must be b
            var dotB = (p.X - b.X) * (a.X - b.X) + (p.Y - b.Y) * (a.Y - b.Y);

            if (dotB <= 0)
            {
                return Vector2.DistanceSquared(b, p);
            }

            //calculate the point along AB that is the closest to p
            Vector2 Point = a + ((b - a) * dotA) / (dotA + dotB);

            //calculate the distance p-Point
            return Vector2.DistanceSquared(p, Point);
        }

        /// <summary>
        /// Gets the nearest distance from point P to the specified rectangle
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
        /// Given 2 lines in 2D space AB, CD this returns true if an 
        /// intersection occurs.
        /// </summary>
        public static bool LineSegmentIntersects(
            Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var rTop = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            var sTop = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);

            var Bot = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            if (Bot == 0)//parallel
            {
                return (FloatEqualsZero(rTop) && FloatEqualsZero(sTop));
            }

            var invBot = 1.0f / Bot;
            var r = rTop * invBot;
            var s = sTop * invBot;

            if ((r > 0) && (r < 1) && (s > 0) && (s < 1))
            {
                //lines intersect
                return true;
            }

            //lines do not intersect
            return false;
        }

        /// <summary>
        /// Given 2 lines in 2D space AB, CD this returns true if an 
        /// intersection occurs and sets dist to the distance the intersection
        /// occurs along AB. Also sets the 2d vector point to the point of
        /// intersection
        /// </summary>
        public static float? LineSegmentIntersectionTest(
            Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var rTop = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            var sTop = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);

            var Bot = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            if (Bot == 0)//parallel
            {
                return FloatEqualsZero(rTop) && FloatEqualsZero(sTop) ? 0 : (float?)null;
            }

            var invBot = 1.0f / Bot;
            var r = rTop * invBot;
            var s = sTop * invBot;

            if ((r > 0) && (r < 1) && (s > 0) && (s < 1))
            {
                //lines intersect
                return Vector2.Distance(a, b) * r;
            }

            //lines do not intersect
            return null;
        }

        /// <summary>
        /// Given 2 lines in 2D space AB, CD this returns true if an 
        /// intersection occurs and sets dist to the distance the intersection
        /// occurs along AB. Also sets the 2d vector point to the point of
        /// intersection
        /// </summary>
        public static bool LineSegmentIntersectionTest(
            Vector2 a, Vector2 b, Vector2 c, Vector2 d, ref float distance, ref Vector2 point)
        {
            var rTop = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            var sTop = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);

            var Bot = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            if (Bot == 0)//parallel
            {
                if (FloatEqualsZero(rTop) && FloatEqualsZero(sTop))
                {
                    distance = 0;
                    point = a;
                    return true;
                }

                return false;
            }

            var invBot = 1.0f / Bot;
            var r = rTop * invBot;
            var s = sTop * invBot;

            if ((r > 0) && (r < 1) && (s > 0) && (s < 1))
            {
                //lines intersect
                distance = Vector2.Distance(a, b) * r;
                point = a + r * (b - a);
                return true;
            }

            //lines do not intersect
            return false;
        }

        /// <summary>
        /// Tests two polygons for intersection.
        /// </summary>
        /// <remarks>This algorithm does not check for enclosure</remarks>
        public static bool PolygonIntersects(Vector2[] object1, Vector2[] object2)
        {
            // Test each line segment of object1 against each segment of object2
            for (var i = 0; i < object1.Length - 1; i++)
            {
                for (var j = 0; j < object2.Length - 1; j++)
                {
                    if (LineSegmentIntersects(
                        object2[j], object2[j + 1], object1[i], object1[i + 1]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tests to see if a polygon and a line segment intersects
        /// </summary>
        /// <remarks>This algorithm does not check for enclosure</remarks>
        public static bool PolygonSegmentIntersects(Vector2[] polygon, Vector2 a, Vector2 b)
        {
            for (var i = 0; i < polygon.Length - 1; i++)
            {
                if (LineSegmentIntersects(
                    polygon[i], polygon[i + 1], a, b))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tests to see if two circle overlaps
        /// </summary>
        /// <returns></returns>
        public static ContainmentType CircleIntersects(
            Vector2 c1, float r1, Vector2 c2, float r2)
        {
            var distBetweenCenters = Vector2.Distance(c2, c1);

            if ((distBetweenCenters < (r1 + r2)))
            {
                return distBetweenCenters < Math.Abs(r1 - r2) ? ContainmentType.Contains : ContainmentType.Intersects;
            }

            return ContainmentType.Disjoint;
        }

        /// <summary>
        /// Given two circles this function calculates the intersection points
        /// of any overlap. This function assumes that the two circles overlaps.
        /// 
        /// see http://astronomy.swin.edu.au/~pbourke/geometry/2circle/
        /// </summary>
        /// <returns></returns>
        public static void CircleIntersectionPoints(
            Vector2 v1, float r1, Vector2 v2, float r2, out Vector2 p1, out Vector2 p2)
        {
            //calculate the distance between the circle centers
            var d = Math.Sqrt((v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y));

            //Now calculate the distance from the center of each circle to the center
            //of the line which connects the intersection points.
            var a = (r1 - r2 + (d * d)) / (2 * d);
            var b = (r2 - r1 + (d * d)) / (2 * d);

            //MAYBE A TEST FOR EXACT OVERLAP? 

            //calculate the point P2 which is the center of the line which 
            //connects the intersection points
            double p2X, p2Y;

            p2X = v1.X + a * (v2.X - v1.X) / d;
            p2Y = v1.Y + a * (v2.Y - v1.Y) / d;

            //calculate first point
            var h1 = Math.Sqrt((r1 * r1) - (a * a));

            p1.X = (float)(p2X - h1 * (v2.Y - v1.Y) / d);
            p1.Y = (float)(p2Y + h1 * (v2.X - v1.X) / d);

            //calculate second point
            var h2 = Math.Sqrt((r2 * r2) - (a * a));

            p2.X = (float)(p2X + h2 * (v2.Y - v1.Y) / d);
            p2.Y = (float)(p2Y - h2 * (v2.X - v1.X) / d);
        }

        /// <summary>
        /// Tests to see if a point is in a circle
        /// </summary>
        public static bool PointInCircle(Vector2 p, Vector2 c, float r)
        {
            return Vector2.DistanceSquared(p, c) < r * r;
        }

        /// <summary>
        /// Returns true if the line segemnt AB intersects with a circle at
        /// position P with radius r
        /// </summary>
        /// <returns></returns>
        public static ContainmentType LineSegmentCircleIntersects(
            Vector2 a, Vector2 b, Vector2 c, float r)
        {
            // First determine the distance from the center of the circle to
            // the line segment (working in distance squared space)
            var distToLineSq = DistanceToLineSegmentSquared(a, b, c);

            if (distToLineSq < r * r)
            {
                return PointInCircle(a, c, r) &&
                    PointInCircle(b, c, r)
                    ? ContainmentType.Contains
                    : ContainmentType.Intersects;
            }

            return ContainmentType.Disjoint;
        }

        /// <summary>
        /// Given a line segment AB and a circle position and radius, this function
        /// determines if there is an intersection and stores the position of the 
        /// closest intersection in the reference IntersectionPoint.
        /// 
        /// returns null if no intersection point is found
        /// </summary>
        /// <returns></returns>
        public static Vector2? LineSegmentCircleClosestIntersectionPoint(
            Vector2 a, Vector2 b, Vector2 c, float r)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests to see if a rectangle contains a point.
        /// Note that min should be smaller than max.
        /// </summary>
        /// <returns></returns>
        public static bool PointInRectangle(
            Vector2 p, Vector2 min, Vector2 max)
        {
            return (p.X > min.X && p.X < max.X && p.Y > min.Y && p.Y < max.Y);
        }

        /// <summary>
        /// Tests to see if a rectangle contains a point. 
        /// v1 and v2 are in local space relative to position and rotation
        /// </summary>
        /// <returns></returns>
        public static bool PointInRectangle(
            Vector2 p, Vector2 min, Vector2 max, Vector2 position, float rotation)
        {
            // Transform p to local space
            Vector2 pLocal = WorldToLocal(p, position, rotation);

            return PointInRectangle(pLocal, min, max);
        }

        /// <summary>
        /// Tests to see if a rectangle and a line segment intersects.
        /// </summary>
        /// <returns></returns>
        public static ContainmentType LineSegmentRectangleIntersects(
            Vector2 a, Vector2 b, Vector2 min, Vector2 max)
        {
            Vector2 v1, v2;

            v1.X = min.X;
            v1.Y = max.Y;
            v2.X = max.X;
            v2.Y = min.Y;

            // Test to see if the line segment intersects
            // 4 rectangle edges

            if (LineSegmentIntersects(a, b, min, v1))
            {
                return ContainmentType.Intersects;
            }

            if (LineSegmentIntersects(a, b, v1, max))
            {
                return ContainmentType.Intersects;
            }

            if (LineSegmentIntersects(a, b, max, v2))
            {
                return ContainmentType.Intersects;
            }

            if (LineSegmentIntersects(a, b, v2, min))
            {
                return ContainmentType.Intersects;
            }

            // Contains
            if (PointInRectangle(a, min, max))
            {
                return ContainmentType.Contains;
            }

            // No intersection
            return ContainmentType.Disjoint;
        }

        /// <summary>
        /// Returns true if two rectangles intersect.
        /// This algorithm does not check for enclosure
        /// </summary>
        /// <returns></returns>
        public static ContainmentType RectangleIntersects(
            Vector2 min1, Vector2 max1, Vector2 position1, float rotation1,
            Vector2 min2, Vector2 max2, Vector2 position2, float rotation2)
        {
            // Compute 8 vertices of the two rectangle
            var rect1 = new Vector2[4];
            var rect2 = new Vector2[4];

            rect1[0] = min1;
            rect1[2] = max1;
            rect1[1].X = min1.X;
            rect1[1].Y = max1.Y;
            rect1[3].X = max1.X;
            rect1[3].Y = min1.Y;

            rect2[0] = min2;
            rect2[2] = max2;
            rect2[1].X = min2.X;
            rect2[1].Y = max2.Y;
            rect2[3].X = max2.X;
            rect2[3].Y = min2.Y;

            // Transform rectangle 2 to rectangle 1 local space
            for (var i = 0; i < 4; i++)
            {
                rect1[i] = LocalToWorld(rect1[i], position1, rotation1);
                rect2[i] = LocalToWorld(rect2[i], position2, rotation2);
            }

            // Polygon intersection test
            if (PolygonIntersects(rect1, rect2))
            {
                for (var i = 0; i < 4; i++)
                {
                    if (PointInRectangle(rect1[i], min2, max2, position2, rotation2))
                    {
                        return ContainmentType.Intersects;
                    }

                    if (PointInRectangle(rect2[i], min1, max1, position1, rotation1))
                    {
                        return ContainmentType.Intersects;
                    }
                }

                return ContainmentType.Contains;
            }

            // No intersection
            return ContainmentType.Disjoint;
        }

        /// <summary>
        /// Returns true if a rectangle and a circle intersects.
        /// This algorithm does not check for enclosure.
        /// </summary>
        public static ContainmentType RectangleCircleIntersects(
            Vector2 min, Vector2 max, Vector2 rectanglePosition, float rotation,
            Vector2 circlePosition, float circleRadius)
        {
            // Compute 8 vertices of the two rectangle
            var rect = new Vector2[4];

            rect[0] = min;
            rect[2] = max;
            rect[1].X = min.X;
            rect[1].Y = max.Y;
            rect[3].X = max.X;
            rect[3].Y = min.Y;

            for (var i = 0; i < 4; i++)
            {
                // Transform to world space
                rect[i] = LocalToWorld(rect[i], rectanglePosition, rotation);
            }

            if (LineSegmentCircleIntersects(
                rect[0], rect[1], circlePosition, circleRadius) != ContainmentType.Disjoint)
            {
                return ContainmentType.Intersects;
            }

            if (LineSegmentCircleIntersects(
                rect[1], rect[2], circlePosition, circleRadius) != ContainmentType.Disjoint)
            {
                return ContainmentType.Intersects;
            }

            if (LineSegmentCircleIntersects(
                rect[2], rect[3], circlePosition, circleRadius) != ContainmentType.Disjoint)
            {
                return ContainmentType.Intersects;
            }

            return LineSegmentCircleIntersects(
                rect[3], rect[0], circlePosition, circleRadius) != ContainmentType.Disjoint
                ? ContainmentType.Intersects
                : ContainmentType.Disjoint;
        }
        #endregion

        #region Test
        /// <summary>
        /// Test cases for Math2D class, too few of them
        /// </summary>
        public static void Test()
        {
            var min = new Vector2(-10, -30);
            var max = new Vector2(10, 30);

            for (float theta = 0; theta < 2; theta += 0.2f)
            {
                System.Diagnostics.Debug.Assert(
                    RectangleIntersects(min, max, Vector2.Zero, theta,
                                        min, max, Vector2.Zero, 0) != ContainmentType.Disjoint);
            }
        }
        #endregion
    }
    #endregion
    
    #region Outline
    /// <summary>
    /// Types of outline
    /// </summary>
    public enum OutlineType
    {
        Empty, Circle, Rectangle
    }

    /// <summary>
    /// Represents a 2D shape that you can do collision detection
    /// </summary>
    public sealed class Outline
    {
        private readonly Random random = new();

        /// <summary>
        /// Gets the outline type
        /// </summary>
        public OutlineType Type => type;

        private OutlineType type;

        /// <summary>
        /// Gets the position of the outline
        /// </summary>
        public Vector2 Position => position;

        private Vector2 position;

        /// <summary>
        /// Gets the radius of the circle
        /// </summary>
        public float Radius => radius;

        private float radius;

        /// <summary>
        /// Gets the rotation of the rectangle, in radius
        /// </summary>
        public float Rotation => rotation;

        private float rotation;

        /// <summary>
        /// Gets the min point of the rectangle
        /// </summary>
        public Vector2 Min => min;

        private Vector2 min;

        /// <summary>
        /// Gets the min point of the rectangle
        /// </summary>
        public Vector2 Max => max;

        private Vector2 max;

        /// <summary>
        /// Gets the area of the outline
        /// </summary>
        public float Area
        {
            get
            {
                if (type == OutlineType.Circle)
                {
                    return (float)Math.PI * radius * radius;
                }
                else if (type == OutlineType.Rectangle)
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

        /// <summary>
        /// Creates a dummy outline
        /// </summary>
        public Outline()
        {
        }

        /// <summary>
        /// Creates a new circle outline
        /// </summary>
        public Outline(Vector2 position, float radius)
        {
            SetCircle(position, radius);
        }

        /// <summary>
        /// Creates a new rectangle outline
        /// </summary>
        public Outline(Vector2 min, Vector2 max, Vector2 position, float rotation)
        {
            SetRectangle(min, max, position, rotation);
        }

        /// <summary>
        /// Setup a circle outline
        /// </summary>
        public void SetCircle(Vector2 position, float radius)
        {
            type = OutlineType.Circle;
            this.position = position;
            this.radius = radius;
        }

        /// <summary>
        /// Setup a rectangle outline
        /// </summary>
        public void SetRectangle(Vector2 min, Vector2 max, Vector2 position, float rotation)
        {
            type = OutlineType.Rectangle;
            this.min = min;
            this.max = max;
            this.position = position;
            this.rotation = rotation;
        }

        /// <summary>
        /// Tests to see if the outline intersects with the specified point
        /// </summary>
        public bool Overlaps(Vector2 point)
        {
            if (type == OutlineType.Empty)
            {
                return false;
            }

            if (type == OutlineType.Circle)
            {
                return Math2D.PointInCircle(point, position, radius);
            }

            return type == OutlineType.Rectangle ? Math2D.PointInRectangle(point, min, max, position, rotation) : false;
        }

        /// <summary>
        /// Gets the distance from the outline to the point
        /// </summary>
        public float DistanceTo(Vector2 point)
        {
            if (type == OutlineType.Empty)
            {
                return 0;
            }

            if (type == OutlineType.Circle)
            {
                var distance = Vector2.Subtract(point, position).Length() - radius;
                return distance < 0 ? 0 : distance;
            }

            return type == OutlineType.Rectangle ? Math2D.DistanceToRectangle(min, max, position, rotation, point) : 0;
        }

        public Vector2 GenerateAPointInsideOutline()
        {
            if(type == OutlineType.Rectangle)
            {
                Vector2 p = max - min;
                var ret = new Vector2((float)(min.X + p.X * random.NextDouble()),(float)(min.Y + p.Y * random.NextDouble()));
                return Math2D.LocalToWorld(ret, position, rotation);
            }
            else if (type == OutlineType.Circle)
            {
                var angle = 2* Math.PI * random.NextDouble();
                var r = radius * random.NextDouble();
                var ret = new Vector2((float)(r * Math.Cos(angle)), (float)(r * Math.Sin(angle)));
                return Math2D.LocalToWorld(ret, position, rotation);
            }
            else
            {
                throw new Exception("Type error");
            }
        }

        /// <summary>
        /// To override the operator "*". Enlarge the outline by scaler n
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static Outline operator *(Outline t, float n)
        {
            Outline outlineRet;
            if (t.type == OutlineType.Circle)
            {
                outlineRet = new Outline(t.position, t.radius * n);
            }
            else if (t.type == OutlineType.Rectangle)
            {
                outlineRet = new Outline(t.min * n, t.max * n, t.position, t.rotation);
            }
            else
            {
                outlineRet = null;
            }

            return outlineRet;
        }

        /// <summary>
        /// Enlarge the outline using addition
        /// </summary>
        public static Outline operator +(Outline outline, float n)
        {            
            if (outline.type == OutlineType.Circle)
            {
                var newRadius = outline.radius + n;
                if (newRadius < 0)
                {
                    newRadius = 0;
                }

                return new Outline(outline.position, outline.radius + n);
            }

            if (outline.type == OutlineType.Rectangle)
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

                return new Outline(min, max, outline.position, outline.rotation);
            }

            return new Outline();
        }

        /// <summary>
        /// Tests to see if two outline intersects
        /// </summary>
        public static ContainmentType Intersects(Outline o1, Outline o2)
        {
            if (null == o1 || null == o2)
            {
                throw new ArgumentNullException();
            }

            // Ignore dummy outlines
            if (o1.type == OutlineType.Empty ||
                o2.type == OutlineType.Empty)
            {
                return ContainmentType.Disjoint;
            }

            // Circle vs Circle
            if (o1.type == OutlineType.Circle &&
                o2.type == OutlineType.Circle)
            {
                return Math2D.CircleIntersects(
                    o1.position, o1.radius, o2.position, o2.radius);
            }

            // Rectangle vs Rectangle
            if (o1.type == OutlineType.Rectangle &&
                o2.type == OutlineType.Rectangle)
            {
                return Math2D.RectangleIntersects(
                    o1.min, o1.max, o1.position, o1.rotation,
                    o2.min, o2.max, o2.position, o2.rotation);
            }

            // Rectangle vs Circle
            if (o1.type == OutlineType.Rectangle &&
                o2.type == OutlineType.Circle)
            {
                return Math2D.RectangleCircleIntersects(
                    o1.min, o1.max, o1.position, o1.rotation,
                    o2.position, o2.radius);
            }

            // Circle vs Rectangle
            return o1.type == OutlineType.Circle &&
                o2.type == OutlineType.Rectangle
                ? Math2D.RectangleCircleIntersects(
                    o2.min, o2.max, o2.position, o2.rotation,
                    o1.position, o1.radius)
                : ContainmentType.Disjoint;
        }
    }
    #endregion
}
