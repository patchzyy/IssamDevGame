using System;
using SpaceDefence.Collision;
using Microsoft.Xna.Framework;

namespace SpaceDefence
{

    public class LinePieceCollider : Collider, IEquatable<LinePieceCollider>
    {
        private const float Epsilon = 0.0001f;

        public Vector2 Start;
        public Vector2 End;

        /// <summary>
        /// The length of the LinePiece, changing the length moves the end vector to adjust the length.
        /// </summary>
        public float Length 
        { 
            get { 
                return (End - Start).Length(); 
            } 
            set {
                End = Start + GetDirection() * value; 
            }
        }

        /// <summary>
        /// The A component from the standard line formula Ax + By + C = 0
        /// </summary>
        public float StandardA
        {
            get
            {
                return Start.Y - End.Y;
            }
        }

        /// <summary>
        /// The B component from the standard line formula Ax + By + C = 0
        /// </summary>
        public float StandardB
        {
            get
            {
                return End.X - Start.X;
            }
        }

        /// <summary>
        /// The C component from the standard line formula Ax + By + C = 0
        /// </summary>
        public float StandardC
        {
            get
            {
                return Start.X * End.Y - End.X * Start.Y;
            }
        }

        public LinePieceCollider(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }
        
        public LinePieceCollider(Vector2 start, Vector2 direction, float length)
        {
            Start = start;
            End = start + direction * length;
        }

        /// <summary>
        /// Should return the angle between a given direction and the up vector.
        /// </summary>
        /// <param name="direction">The Vector2 pointing out from (0,0) to calculate the angle to.</param>
        /// <returns> The angle in radians between the the up vector and the direction to the cursor.</returns>
        public static float GetAngle(Vector2 direction)
        {
            if (direction.LengthSquared() <= Epsilon)
                return 0;

            direction.Normalize();
            return (float)(Math.Atan2(direction.Y, direction.X) + Math.PI / 2);
        }


        /// <summary>
        /// Calculates the normalized vector pointing from point1 to point2
        /// </summary>
        /// <returns> A Vector2 containing the direction from point1 to point2. </returns>
        public static Vector2 GetDirection(Vector2 point1, Vector2 point2)
        {
            Vector2 direction = point2 - point1;
            if (direction.LengthSquared() <= Epsilon)
                return -Vector2.UnitY;

            direction.Normalize();
            return direction;
        }


        /// <summary>
        /// Gets whether or not the Line intersects another Line
        /// </summary>
        /// <param name="other">The Line to check for intersection</param>
        /// <returns>true there is any overlap between the Circle and the Line.</returns>
        public override bool Intersects(LinePieceCollider other)
        {
            Vector2 firstLine = End - Start;
            Vector2 secondLine = other.End - other.Start;
            float divisor = Cross(firstLine, secondLine);
            Vector2 difference = other.Start - Start;

            if (Math.Abs(divisor) <= Epsilon)
            {
                if (Math.Abs(Cross(difference, firstLine)) > Epsilon)
                    return false;

                return PointOnSegment(other.Start, Start, End)
                    || PointOnSegment(other.End, Start, End)
                    || PointOnSegment(Start, other.Start, other.End)
                    || PointOnSegment(End, other.Start, other.End);
            }

            float startDistance = Cross(difference, secondLine) / divisor;
            float otherDistance = Cross(difference, firstLine) / divisor;

            return startDistance >= 0 && startDistance <= 1 && otherDistance >= 0 && otherDistance <= 1;
        }


        /// <summary>
        /// Gets whether or not the line intersects a Circle.
        /// </summary>
        /// <param name="other">The Circle to check for intersection.</param>
        /// <returns>true there is any overlap between the two Circles.</returns>
        public override bool Intersects(CircleCollider other)
        {
            Vector2 nearestPoint = NearestPointOnLine(other.Center);
            return Vector2.DistanceSquared(nearestPoint, other.Center) <= other.Radius * other.Radius;
        }

        /// <summary>
        /// Gets whether or not the Line intersects the Rectangle.
        /// </summary>
        /// <param name="other">The Rectangle to check for intersection.</param>
        /// <returns>true there is any overlap between the Circle and the Rectangle.</returns>
        public override bool Intersects(RectangleCollider other)
        {
            if (other.Contains(Start) || other.Contains(End))
                return true;

            Rectangle rectangle = other.shape;
            Vector2 topLeft = new Vector2(rectangle.Left, rectangle.Top);
            Vector2 topRight = new Vector2(rectangle.Right, rectangle.Top);
            Vector2 bottomLeft = new Vector2(rectangle.Left, rectangle.Bottom);
            Vector2 bottomRight = new Vector2(rectangle.Right, rectangle.Bottom);

            return Intersects(new LinePieceCollider(topLeft, topRight))
                || Intersects(new LinePieceCollider(topRight, bottomRight))
                || Intersects(new LinePieceCollider(bottomRight, bottomLeft))
                || Intersects(new LinePieceCollider(bottomLeft, topLeft));
        }

        /// <summary>
        /// Calculates the intersection point between 2 lines.
        /// </summary>
        /// <param name="Other">The line to intersect with</param>
        /// <returns>A Vector2 with the point of intersection.</returns>
        public Vector2 GetIntersection(LinePieceCollider Other)
        {
            float divisor = Cross(End - Start, Other.End - Other.Start);
            if (Math.Abs(divisor) <= Epsilon)
                return Vector2.Zero;

            Vector2 difference = Other.Start - Start;
            float distance = Cross(difference, Other.End - Other.Start) / divisor;
            return Start + (End - Start) * distance;
        }

        /// <summary>
        /// Finds the nearest point on a line to a given vector, taking into account if the line is .
        /// </summary>
        /// <param name="other">The Vector you want to find the nearest point to.</param>
        /// <returns>The nearest point on the line.</returns>
        public Vector2 NearestPointOnLine(Vector2 other)
        {
            Vector2 line = End - Start;
            float lineLengthSquared = line.LengthSquared();
            if (lineLengthSquared <= Epsilon)
                return Start;

            float progress = Vector2.Dot(other - Start, line) / lineLengthSquared;
            progress = Math.Clamp(progress, 0, 1);
            return Start + line * progress;
        }

        /// <summary>
        /// Returns the enclosing Axis Aligned Bounding Box containing the control points for the line.
        /// As an unbound line has infinite length, the returned bounding box assumes the line to be bound.
        /// </summary>
        /// <returns></returns>
        public override Rectangle GetBoundingBox()
        {
            Point topLeft = new Point((int)Math.Min(Start.X, End.X), (int)Math.Min(Start.Y, End.Y));
            Point size = new Point((int)Math.Max(Start.X, End.X), (int)Math.Max(Start.Y, End.Y)) - topLeft;
            return new Rectangle(topLeft,size);
        }


        /// <summary>
        /// Gets whether or not the provided coordinates lie on the line.
        /// </summary>
        /// <param name="coordinates">The coordinates to check.</param>
        /// <returns>true if the coordinates are within the circle.</returns>
        public override bool Contains(Vector2 coordinates)
        {
            Vector2 nearestPoint = NearestPointOnLine(coordinates);
            return Vector2.DistanceSquared(nearestPoint, coordinates) <= 1f;
        }

        public bool Equals(LinePieceCollider other)
        {
            return other.Start == this.Start && other.End == this.End;
        }

        /// <summary>
        /// Calculates the normalized vector pointing from point1 to point2
        /// </summary>
        /// <returns> A Vector2 containing the direction from point1 to point2. </returns>
        public static Vector2 GetDirection(Point point1, Point point2)
        {
            return GetDirection(point1.ToVector2(), point2.ToVector2());
        }


        /// <summary>
        /// Calculates the normalized vector pointing from point1 to point2
        /// </summary>
        /// <returns> A Vector2 containing the direction from point1 to point2. </returns>
        public Vector2 GetDirection()
        {
            return GetDirection(Start, End);
        }


        /// <summary>
        /// Should return the angle between a given direction and the up vector.
        /// </summary>
        /// <param name="direction">The Vector2 pointing out from (0,0) to calculate the angle to.</param>
        /// <returns> The angle in radians between the the up vector and the direction to the cursor.</returns>
        public float GetAngle()
        {
            return GetAngle(GetDirection());
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.X * second.Y - first.Y * second.X;
        }

        private static bool PointOnSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            if (Math.Abs(Cross(point - segmentStart, segmentEnd - segmentStart)) > Epsilon)
                return false;

            float minX = Math.Min(segmentStart.X, segmentEnd.X) - Epsilon;
            float maxX = Math.Max(segmentStart.X, segmentEnd.X) + Epsilon;
            float minY = Math.Min(segmentStart.Y, segmentEnd.Y) - Epsilon;
            float maxY = Math.Max(segmentStart.Y, segmentEnd.Y) + Epsilon;

            return point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY;
        }
    }
}
