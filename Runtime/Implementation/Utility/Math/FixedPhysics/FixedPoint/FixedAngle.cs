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
    public struct FixedAngle
    {
        public int Value;
        public uint Scale;

        public static FixedAngle HalfPi = new FixedAngle(15708, FixedAcosTable.Scale);
        public static FixedAngle PI = new FixedAngle(31416, FixedAcosTable.Scale);
        public static FixedAngle TWOPI = new FixedAngle(62832, FixedAcosTable.Scale);
        public static FixedAngle Zero = new FixedAngle(0, FixedAcosTable.Scale);

        public FixedAngle(int value, uint scale)
        {
            Value = value;
            Scale = scale;
        }

        public float ToRadian()
        {
            return Value / (float)Scale;
        }

        public int ToAngle()
        {
            float radian = ToRadian();
            return (int)Math.Round(radian * 180.0f / Math.PI);
        }

        public static bool operator >(FixedAngle a, FixedAngle b)
        {
            if (a.Scale != b.Scale)
            {
                throw new System.Exception("Scale not equal");
            }
            return a.Value > b.Value;
        }

        public static bool operator <(FixedAngle a, FixedAngle b)
        {
            if (a.Scale != b.Scale)
            {
                throw new System.Exception("Scale not equal");
            }
            return a.Value < b.Value;
        }

        public static bool operator >=(FixedAngle a, FixedAngle b)
        {
            if (a.Scale != b.Scale)
            {
                throw new System.Exception("Scale not equal");
            }
            return a.Value >= b.Value;
        }

        public static bool operator <=(FixedAngle a, FixedAngle b)
        {
            if (a.Scale != b.Scale)
            {
                throw new System.Exception("Scale not equal");
            }
            return a.Value <= b.Value;
        }

        public static bool operator ==(FixedAngle a, FixedAngle b)
        {
            if (a.Scale != b.Scale)
            {
                throw new System.Exception("Scale not equal");
            }
            return a.Value == b.Value;
        }

        public static bool operator !=(FixedAngle a, FixedAngle b)
        {
            if (a.Scale != b.Scale)
            {
                throw new System.Exception("Scale not equal");
            }
            return a.Value != b.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj != null || obj is not FixedAngle a) 
            {
                return false;
            }

            return a.Value == Value && a.Scale == Scale;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return $"Value: {Value}, Scale: {Scale}";
        }
    }
}
