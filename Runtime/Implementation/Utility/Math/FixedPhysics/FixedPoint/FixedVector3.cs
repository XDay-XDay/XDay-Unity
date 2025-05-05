/*
 * Copyright (c) 2024-2025 XDay
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using UnityEngine;

namespace XDay
{
    public struct FixedVector3
    {
        public FixedPoint SqrMagnitude => X * X + Y * Y + Z * Z;
        public FixedPoint Magnitude => FixedMath.Sqrt(SqrMagnitude);
        public FixedVector3 Normalized
        {
            get
            {
                var len = Magnitude;
                if (len == 0)
                {
                    return Zero;
                }

                len = 1 / len;
                return new FixedVector3(X * len, Y * len, Z * len);
            }
        }

        public FixedPoint X;
        public FixedPoint Y;
        public FixedPoint Z;

        public static readonly FixedVector3 Zero = new FixedVector3(0, 0, 0);
        public static readonly FixedVector3 One = new FixedVector3(1, 1, 1);
        public static readonly FixedVector3 Up = new FixedVector3(0, 1, 0);
        public static readonly FixedVector3 Down = new FixedVector3(0, -1, 0);
        public static readonly FixedVector3 Right = new FixedVector3(1, 0, 0);
        public static readonly FixedVector3 Left = new FixedVector3(-1, 0, 0);
        public static readonly FixedVector3 Forward = new FixedVector3(0, 0, 1);
        public static readonly FixedVector3 Back = new FixedVector3(0, 0, -1);

        public FixedVector3(float x, float y, float z)
        {
            X = new FixedPoint(x);
            Y = new FixedPoint(y);
            Z = new FixedPoint(z);
        }

        public FixedVector3(Vector3 v)
        {
            X = new FixedPoint(v.x);
            Y = new FixedPoint(v.y);
            Z = new FixedPoint(v.z);
        }

        public FixedVector3(int x, int y, int z)
        {
            X = new FixedPoint(x);
            Y = new FixedPoint(y);
            Z = new FixedPoint(z);
        }

        public FixedVector3(FixedPoint x, FixedPoint y, FixedPoint z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Normalize()
        {
            var len = Magnitude;
            if (len > 0)
            {
                len = 1 / len;
                X *= len;
                Y *= len;
                Z *= len;
            }
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X.FloatValue, Y.FloatValue, Z.FloatValue);
        }

        public FixedVector2 ToFixedVector2XZ()
        {
            return new FixedVector2(X, Z);
        }

        public FixedVector2 ToFixedVector2XY()
        {
            return new FixedVector2(X, Y);
        }

        public FixedPoint this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return X;
                    case 1:
                        return Y;
                    case 2:
                        return Z;
                    default:
                        return 0;
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
                        Z = value;
                        break;
                }
            }
        }

        public static FixedVector3 operator + (FixedVector3 a, FixedVector3 b)
        {
            return new FixedVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static FixedVector3 operator -(FixedVector3 a, FixedVector3 b)
        {
            return new FixedVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static FixedVector3 operator /(FixedVector3 a, FixedPoint v)
        {
            if (v == FixedPoint.Zero)
            {
                throw new System.Exception("Divided by zero");
            }
            return new FixedVector3(a.X / v, a.Y / v, a.Z / v);
        }

        public static FixedVector3 operator *(FixedVector3 a, FixedPoint v)
        {
            return new FixedVector3(a.X * v, a.Y * v, a.Z * v);
        }

        public static FixedVector3 operator *(FixedPoint v, FixedVector3 a)
        {
            return new FixedVector3(a.X * v, a.Y * v, a.Z * v);
        }

        public static FixedVector3 operator -(FixedVector3 a)
        {
            return new FixedVector3(-a.X, -a.Y, -a.Z);
        }

        public static bool operator ==(FixedVector3 a, FixedVector3 b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(FixedVector3 a, FixedVector3 b)
        {
            return a.X != b.X || a.Y != b.Y || a.Z != b.Z;
        }

        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj is not FixedVector3 v)
            {
                return false;
            }

            return v == this;
        }

        public static FixedPoint GetSqrMagnitude(FixedVector3 v)
        {
            return v.X * v.X + v.Y * v.Y + v.Z * v.Z;
        }

        public static FixedVector3 GetNormalized(FixedVector3 v)
        {
            FixedVector3 result = v;
            result.Normalize();
            return result;
        }

        public static FixedPoint Dot(FixedVector3 v1, FixedVector3 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public static FixedVector3 Cross(FixedVector3 v1, FixedVector3 v2)
        {
            return new FixedVector3(
                v1.Y * v2.Z - v2.Y * v1.Z,
                v1.Z * v2.X - v2.Z * v1.X,
                v1.X * v2.Y - v2.X * v1.Y
                );
        }

        public static FixedAngle Angle(FixedVector3 v1, FixedVector3 v2)
        {
            var k = v1.Magnitude * v2.Magnitude;
            if (k == 0)
            {
                return FixedAngle.Zero;
            }
            var d = Dot(v1, v2);

            return FixedMath.ACos(d / k);
        }
    }
}
