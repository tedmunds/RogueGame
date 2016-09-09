using System;

namespace RogueGame {

    /// <summary>
    /// Simple vector class with some basic operations for tile based math
    /// </summary>
    [Serializable()]
    public struct Vector2 : IEquatable<Vector2> {

        public static Vector2 Zero {
            get { return new Vector2(0, 0); }
        }

        public float x;
        public int X {
            get { return (int)x; }
        }

        public float y;
        public int Y {
            get { return (int)y; }
        }


        public Vector2(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public Vector2(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public Vector2(Vector2 vector2) {
            this.x = vector2.x;
            this.y = vector2.y;
        }


        public float Magnitude() {
            return (float)Math.Sqrt(x * x + y * y);
        }

        public float SqrMagnitude() {
            return (x * x + y * y);
        }

        public void Normalize() {
            float mag = Magnitude();
            x = x / mag;
            y = y / mag;
        }


        public Vector2 Normalized() {
            float mag = Magnitude();
            return new Vector2(x / mag, y / mag);
        }

        public static bool operator ==(Vector2 lhs, Vector2 rhs) {
            return (lhs.X == rhs.X && lhs.Y == rhs.Y);
        }

        public static bool operator !=(Vector2 lhs, Vector2 rhs) {
            return (lhs.X != rhs.X || lhs.Y != rhs.Y);
        }

        public bool Equals(Vector2 v) {
            return (x == v.x && y == v.y);
        }

        public static Vector2 operator +(Vector2 lhs, Vector2 rhs) {
            return new Vector2(lhs.x + rhs.x, lhs.y + rhs.y);
        }

        public static Vector2 operator -(Vector2 lhs, Vector2 rhs) {
            return new Vector2(lhs.x - rhs.x, lhs.y - rhs.y);
        }

        public static Vector2 operator *(Vector2 lhs, float scalar) {
            return new Vector2((lhs.x * scalar), (lhs.y * scalar));
        }

        public static Vector2 operator /(Vector2 lhs, float scalar) {
            return new Vector2((lhs.x / scalar), (lhs.y / scalar));
        }

        public static float Dot(Vector2 lhs, Vector2 rhs) {
            return (lhs.x * rhs.x + lhs.y * rhs.y);
        }
        
        /// <summary>
        /// Calculates the distance b/w a and b
        /// </summary>
        public static float Distance(Vector2 a, Vector2 b) {
            return (a - b).Magnitude();
        }

        public static int TaxiDistance(Vector2 a, Vector2 b) {
            return (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y));
        }

        /// <summary>
        /// Projects the vector a onto b
        /// </summary>
        public static Vector2 Project(Vector2 a, Vector2 b) {
            float bMag = b.Magnitude();
            return (b / bMag) * (Dot(a, b) / bMag);
        }

        /// <summary>
        /// Projects the input vector onto the dominant axis, and normalizes it
        /// </summary>
        public static Vector2 OrthoNormal(Vector2 v) {
            if(Math.Abs(v.x) > Math.Abs(v.y)) {
                v.x = Math.Sign(v.x);
                v.y = 0.0f;
            }
            else {
                v.x = 0.0f;
                v.y = Math.Sign(v.y);
            }

            return v;
        }

        public static float NormalDot(Vector2 lhs, Vector2 rhs) {
            float lhsMag = lhs.Magnitude();
            float rhsMag = rhs.Magnitude();
            float x0 = lhs.x / lhsMag;
            float y0 = lhs.y / lhsMag;
            float x1 = rhs.x / rhsMag;
            float y1 = rhs.y / rhsMag;
            return x0 * x1 + y0 * y1;
        }

        public override bool Equals(object obj) {
            Vector2 other = (Vector2)obj;
            if(other == null) {
                return false;
            }

            return (x == other.x && y == other.y);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override string ToString() {
            return "[" + x + "," + y + "]";
        }



    }
}
