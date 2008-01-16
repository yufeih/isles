using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Engine
{
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
        public static Vector2 WorldToLocal(Vector2 p, Vector2 position, float rotation)
        {
            Vector2 v;

            // Apply translation
            p -= position;

            // Apply rotation
            float sin = (float)Math.Sin(-rotation);
            float cos = (float)Math.Cos(-rotation);

            v.X = p.X * cos - p.Y * sin;
            v.Y = p.X * sin + p.Y * cos;

            return v;
        }



        /// <summary>
        /// Transform a local point p to world space specified by position and rotation
        /// </summary>
        /// <returns></returns>
        public static Vector2 LocalToWorld(Vector2 p, Vector2 position, float rotation)
        {
            Vector2 v;

            // Apply rotation
            float sin = (float)Math.Sin(rotation);
            float cos = (float)Math.Cos(rotation);

            v.X = p.X * cos - p.Y * sin;
            v.Y = p.X * sin + p.Y * cos;

            // Apply translation
            return v + position;
        }



        /// <summary>
        /// given a plane and a ray this function determins how far along the ray 
        /// an interestion occurs. Returns null if the ray is parallel
        /// </summary>
        public static float? RayPlaneIntersects(
            Vector2 rayOrigin, Vector2 rayDirection, Vector2 planePoint, Vector2 planeNormal)
        {
            float d = Vector2.Dot(planeNormal, planePoint);
            float numer = Vector2.Dot(planeNormal, rayOrigin) - d;
            float denom = Vector2.Dot(planeNormal, rayDirection);

            // Normal is parallel to vector
            if (FloatEqualsZero(denom))
                return null;

            return -(numer / denom);
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
            float d = Vector2.Dot(planeNormal, planePoint - point);

            if (d < -Epsilon)
                return SpanType.Front;

            if (d > Epsilon)
                return SpanType.Back;

            return SpanType.Local;
        }



        /// <summary>
        /// Test to see if a ray intersects a circle
        /// </summary>
        public static float? RayCircleIntersectionTest(
            Vector2 rayOrigin, Vector2 rayHeading, Vector2 circle, float radius)
        {
            Vector2 toCircle = circle - rayOrigin;

            float length = toCircle.Length();
            float v = Vector2.Dot(toCircle, rayHeading);
            float d = radius * radius - (length * length - v * v);

            // No intersection, return null
            if (d < 0)
                return null;

            return v - (float)Math.Sqrt(d);
        }



        /// <summary>
        /// Whether a ray intersects a circle
        /// </summary>
        public static bool RayCircleIntersects(
            Vector2 rayOrigin, Vector2 rayHeading, Vector2 circle, float radius)
        {
            Vector2 toCircle = circle - rayOrigin;

            float length = toCircle.Length();
            float v = Vector2.Dot(toCircle, rayHeading);
            float d = radius * radius - (length * length - v * v);

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

            float sqrLen = PmC.LengthSquared();
            float rSqr = R * R;

            // P is inside or on the circle
            if (sqrLen <= rSqr)
                return false;

            float InvSqrLen = 1.0f / sqrLen;
            float Root = (float)Math.Sqrt(Math.Abs(sqrLen - rSqr));

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
            float dotA = (p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y);

            if (dotA <= 0)
                return Vector2.Distance(a, p);

            //if the angle is obtuse between PB and AB is obtuse then the closest
            //vertex must be b
            float dotB = (p.X - b.X) * (a.X - b.X) + (p.Y - b.Y) * (a.Y - b.Y);

            if (dotB <= 0)
                return Vector2.Distance(b, p);

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
            float dotA = (p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y);

            if (dotA <= 0)
                return Vector2.DistanceSquared(a, p);

            //if the angle is obtuse between PB and AB is obtuse then the closest
            //vertex must be b
            float dotB = (p.X - b.X) * (a.X - b.X) + (p.Y - b.Y) * (a.Y - b.Y);

            if (dotB <= 0)
                return Vector2.DistanceSquared(b, p);

            //calculate the point along AB that is the closest to p
            Vector2 Point = a + ((b - a) * dotA) / (dotA + dotB);

            //calculate the distance p-Point
            return Vector2.DistanceSquared(p, Point);
        }



        /// <summary>
        /// Given 2 lines in 2D space AB, CD this returns true if an 
        /// intersection occurs.
        /// </summary>
        public static bool LineSegmentIntersects(
            Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float rTop = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            float sTop = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);

            float Bot = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            if (Bot == 0)//parallel
            {
                return (FloatEqualsZero(rTop) && FloatEqualsZero(sTop));
            }

            float invBot = 1.0f / Bot;
            float r = rTop * invBot;
            float s = sTop * invBot;

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
            float rTop = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            float sTop = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);

            float Bot = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            if (Bot == 0)//parallel
            {
                if (FloatEqualsZero(rTop) && FloatEqualsZero(sTop))
                    return 0;

                return null;
            }

            float invBot = 1.0f / Bot;
            float r = rTop * invBot;
            float s = sTop * invBot;

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
            float rTop = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            float sTop = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);

            float Bot = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

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

            float invBot = 1.0f / Bot;
            float r = rTop * invBot;
            float s = sTop * invBot;

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
            for (int i = 0; i < object1.Length - 1; i++)
                for (int j = 0; j < object2.Length - 1; j++)
                    if (LineSegmentIntersects(
                        object2[j], object2[j + 1], object1[i], object1[i + 1]))
                    {
                        return true;
                    }

            return false;
        }



        /// <summary>
        /// Tests to see if a polygon and a line segment intersects
        /// </summary>
        /// <remarks>This algorithm does not check for enclosure</remarks>
        public static bool PolygonSegmentIntersects(Vector2[] polygon, Vector2 a, Vector2 b)
        {
            for (int i = 0; i < polygon.Length - 1; i++)
                if (LineSegmentIntersects(
                    polygon[i], polygon[i + 1], a, b))
                {
                    return true;
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
            float distBetweenCenters = Vector2.Distance(c2, c1);

            if ((distBetweenCenters < (r1 + r2)))
            {
                if ((distBetweenCenters < Math.Abs(r1 - r2)))
                    return ContainmentType.Contains;

                return ContainmentType.Intersects;
            }

            return ContainmentType.Disjoint;
        }



        /// <summary>
        /// Given two circles this function calculates the intersection points
        /// of any overlap. Returns false if no overlap found
        /// 
        /// see http://astronomy.swin.edu.au/~pbourke/geometry/2circle/
        /// </summary>
        /// <returns></returns>
        public static bool CircleIntersectionPoints(
            Vector2 v1, float r1, Vector2 v2, float r2, ref Vector2 p1, ref Vector2 p2)
        {
            throw new NotImplementedException();
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
            float distToLineSq = DistanceToLineSegmentSquared(a, b, c);

            if (distToLineSq < r * r)
            {
                if (PointInCircle(a, c, r) &&
                    PointInCircle(b, c, r))
                    return ContainmentType.Contains;

                return ContainmentType.Intersects;
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
                return ContainmentType.Intersects;

            if (LineSegmentIntersects(a, b, v1, max))
                return ContainmentType.Intersects;

            if (LineSegmentIntersects(a, b, max, v2))
                return ContainmentType.Intersects;

            if (LineSegmentIntersects(a, b, v2, min))
                return ContainmentType.Intersects;

            // Contains
            if (PointInRectangle(a, min, max))
                return ContainmentType.Contains;

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
            Vector2[] rect1 = new Vector2[4];
            Vector2[] rect2 = new Vector2[4];

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
            for (int i = 0; i < 4; i++)
            {
                rect1[i] = LocalToWorld(rect1[i], position1, rotation1);
                rect2[i] = LocalToWorld(rect2[i], position2, rotation2);
            }

            // Polygon intersection test
            if (PolygonIntersects(rect1, rect2))
            {
                for (int i = 0; i < 4; i++)
                {
                    if (PointInRectangle(rect1[i], min2, max2, position2, rotation2))
                        return ContainmentType.Intersects;

                    if (PointInRectangle(rect2[i], min1, max1, position1, rotation1))
                        return ContainmentType.Intersects;
                }

                return ContainmentType.Contains;
            }

            // No intersection
            return ContainmentType.Disjoint;
        }
        #endregion

        #region Test
        /// <summary>
        /// Test cases for Math2D class, too few of them
        /// </summary>
        public static void Test()
        {
            Vector2 min = new Vector2(-10, -30);
            Vector2 max = new Vector2(10, 30);

            for (float theta = 0; theta < 2; theta += 0.2f)
            {
                System.Diagnostics.Debug.Assert(
                    RectangleIntersects(min, max, Vector2.Zero, theta,
                                        min, max, Vector2.Zero, 0) != ContainmentType.Disjoint);
            }
        }
        #endregion
    }

}
