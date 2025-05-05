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

namespace XDay
{
    public static class FixedMath
    {
        public static FixedPoint Pi = new FixedPoint(MathF.PI);
        public static FixedPoint TwoPi = new FixedPoint(MathF.PI) * 2;

        public static FixedPoint ToAngle(FixedPoint radian)
        {
            return radian * 180 / Pi;
        }

        public static FixedPoint ToRadian(FixedPoint angle)
        {
            return angle * Pi / 180;
        }

        //v is between [-1, 1]
        public static FixedAngle ACos(FixedPoint v)
        {
            var index = v * FixedAcosTable.HalfCount + FixedAcosTable.HalfCount;
            index = Clamp(index, 0, FixedAcosTable.Count - 1);
            var value = FixedAcosTable.Value[index.IntValue];
            return new FixedAngle(value, FixedAcosTable.Scale);
        }

        //radian between [0, 2pi]
        public static FixedPoint Cos(FixedPoint radian)
        {
            while (radian < 0)
            {
                radian += TwoPi;
            }

            while (radian > TwoPi)
            {
                radian -= TwoPi;
            }
            return FixedCosTable.Sample(radian / TwoPi);
        }

        //radian between [0, 2pi]
        public static FixedPoint Sin(FixedPoint radian)
        {
            while (radian < 0)
            {
                radian += TwoPi;
            }

            while (radian > TwoPi)
            {
                radian -= TwoPi;
            }
            return FixedSinTable.Sample(radian / TwoPi);
        }

        public static FixedVector3 Rotate(FixedVector3 pos, FixedPoint rotationYAngle)
        {
            rotationYAngle = ToRadian(rotationYAngle);
            var x = pos.X * Cos(rotationYAngle) + pos.Z * Sin(rotationYAngle);
            var z = -pos.X * Sin(rotationYAngle) + pos.Z * Cos(rotationYAngle);
            return new FixedVector3(x, pos.Y, z);
        }

        public static FixedPoint Sqrt(FixedPoint v, int iterateCount = 10)
        {
            if (v < FixedPoint.Zero)
            {
                throw new Exception("Invalid value");
            }

            if (v == FixedPoint.Zero)
            {
                return 0;
            }

            int n = 0;
            FixedPoint result = v;
            FixedPoint prevValue;
            do
            {
                prevValue = result;
                result = (result + v / result) >> 1;
                ++n;
            } while (n < iterateCount && result != prevValue);

            return result;
        }

        public static FixedPoint Clamp(FixedPoint v, FixedPoint min, FixedPoint max)
        {
            if (v < min)
            {
                return min;
            }
            if (v > max)
            {
                return max;
            }
            return v;
        }

        public static FixedPoint Min(FixedPoint a, FixedPoint b)
        {
            return a < b ? a : b;
        }

        public static FixedPoint Max(FixedPoint a, FixedPoint b)
        {
            return a > b ? a : b;
        }

        public static FixedPoint Abs(FixedPoint v)
        {
            return v.Abs();
        }

        public static FixedPoint FloorToInt(FixedPoint v)
        {
            return v - v % 1;
        }

        public static FixedPoint CeilToInt(FixedPoint v)
        {
            var d = v - v % 1;
            if (d != v)
            {
                return d + 1;
            }
            return d;
        }
    }
}
