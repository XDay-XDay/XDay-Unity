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
    public struct FixedVector2
    {
        public FixedPoint SqrMagnitude => X * X + Y * Y;
        public FixedPoint Magnitude => FixedMath.Sqrt(SqrMagnitude);
        public FixedVector2 Normalized
        {
            get
            {
                var len = Magnitude;
                if (len == 0)
                {
                    return Zero;
                }

                len = 1 / len;
                return new FixedVector2(X * len, Y * len);
            }
        }
        public FixedPoint X;
        public FixedPoint Y;
        public static readonly FixedVector2 Zero = new FixedVector2(0, 0);
        public static readonly FixedVector2 One = new FixedVector2(1, 1);
        public static readonly FixedVector2 Up = new FixedVector2(0, 1);
        public static readonly FixedVector2 Down = new FixedVector2(0, -1);
        public static readonly FixedVector2 Right = new FixedVector2(1, 0);
        public static readonly FixedVector2 Left = new FixedVector2(-1, 0);

        public FixedVector2(float x, float y)
        {
            X = new FixedPoint(x);
            Y = new FixedPoint(y);
        }

        public FixedVector2(Vector2 v)
        {
            X = new FixedPoint(v.x);
            Y = new FixedPoint(v.y);
        }

        public FixedVector2(int x, int y)
        {
            X = new FixedPoint(x);
            Y = new FixedPoint(y);
        }

        public FixedVector2(FixedPoint x, FixedPoint y)
        {
            X = x;
            Y = y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X.FloatValue, Y.FloatValue);
        }

        public FixedVector3 ToFixedVector3XZ()
        {
            return new FixedVector3(X, 0, Y);
        }

        public FixedVector3 ToFixedVector3XY()
        {
            return new FixedVector3(X, Y, 0);
        }

        public void Normalize()
        {
            var len = Magnitude;
            if (len > 0)
            {
                len = 1 / len;
                X *= len;
                Y *= len;
            }
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
                    default:
                        UnityEngine.Debug.Assert(false);
                        break;
                }
            }
        }

        public static FixedVector2 operator +(FixedVector2 a, FixedVector2 b)
        {
            return new FixedVector2(a.X + b.X, a.Y + b.Y);
        }

        public static FixedVector2 operator -(FixedVector2 a, FixedVector2 b)
        {
            return new FixedVector2(a.X - b.X, a.Y - b.Y);
        }

        public static FixedVector2 operator /(FixedVector2 a, FixedPoint v)
        {
            if (v == FixedPoint.Zero)
            {
                throw new System.Exception("Divided by zero");
            }
            return new FixedVector2(a.X / v, a.Y / v);
        }

        public static FixedVector2 operator *(FixedVector2 a, FixedPoint v)
        {
            return new FixedVector2(a.X * v, a.Y * v);
        }

        public static FixedVector2 operator *(FixedPoint v, FixedVector2 a)
        {
            return new FixedVector2(a.X * v, a.Y * v);
        }

        public static FixedVector2 operator -(FixedVector2 a)
        {
            return new FixedVector2(-a.X, -a.Y);
        }

        public static bool operator ==(FixedVector2 a, FixedVector2 b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(FixedVector2 a, FixedVector2 b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public override string ToString()
        {
            return $"({X},{Y})";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj is not FixedVector2 v)
            {
                return false;
            }

            return v == this;
        }

        public static FixedPoint GetSqrMagnitude(FixedVector2 v)
        {
            return v.X * v.X + v.Y * v.Y;
        }

        public static FixedVector2 GetNormalized(FixedVector2 v)
        {
            FixedVector2 result = v;
            result.Normalize();
            return result;
        }

        public static FixedPoint Dot(FixedVector2 v1, FixedVector2 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }

        public static FixedAngle Angle(FixedVector2 v1, FixedVector2 v2)
        {
            var k = v1.Magnitude * v2.Magnitude;
            if (k == 0)
            {
                return FixedAngle.Zero;
            }
            var d = Dot(v1, v2);

            return FixedMath.ACos(d / k);
        }

        public static FixedPoint DistanceSqr(FixedVector2 a, FixedVector2 b)
        {
            return (a - b).SqrMagnitude;
        }

        public static FixedPoint Distance(FixedVector2 a, FixedVector2 b)
        {
            return (a - b).Magnitude;
        }

        public static FixedPoint Cross(FixedVector2 a, FixedVector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }
    }
}
