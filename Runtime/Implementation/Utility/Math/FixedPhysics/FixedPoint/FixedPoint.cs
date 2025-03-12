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
    public struct FixedPoint
    {
        public readonly float FloatValue => m_ScaledValue / (float)m_ScaleFactor;
        public readonly int IntValue
        {
            get
            {
                if (m_ScaledValue >= 0)
                {
                    return (int)(m_ScaledValue >> m_ShiftBit);
                }
                return -(int)(-m_ScaledValue >> m_ShiftBit);
            }
        }
        public long ScaledValue { set => m_ScaledValue = value; get => m_ScaledValue; }
        public static readonly FixedPoint Zero = new(0);
        public static readonly FixedPoint One = new(1);
        public static readonly FixedPoint Max = new FixedPoint(int.MaxValue);
        public static readonly FixedPoint Min = new FixedPoint(int.MinValue);

        public FixedPoint(int value)
        {
            m_ScaledValue = (long)value * m_ScaleFactor;
        }

        public FixedPoint(float value)
        {
            m_ScaledValue = (long)Math.Round(value * m_ScaleFactor);
        }

        private FixedPoint(long scaledValued)
        {
            m_ScaledValue = scaledValued;
        }

        public static implicit operator FixedPoint(int v)
        {
            return new FixedPoint(v);
        }

        public static explicit operator FixedPoint(float v)
        {
            return new FixedPoint(v);
        }

        public static FixedPoint operator + (FixedPoint v1, FixedPoint v2)
        {
            return new FixedPoint(v1.m_ScaledValue + v2.m_ScaledValue);
        }

        public static FixedPoint operator -(FixedPoint v1, FixedPoint v2)
        {
            return new FixedPoint(v1.m_ScaledValue - v2.m_ScaledValue);
        }

        public static FixedPoint operator -(FixedPoint v1)
        {
            return new FixedPoint(-v1.m_ScaledValue);
        }

        public static FixedPoint operator *(FixedPoint v1, FixedPoint v2)
        {
            long val = v1.m_ScaledValue * v2.m_ScaledValue;
            if (val >= 0)
            {
                val >>= m_ShiftBit;
            }
            else
            {
                val = -(-val >> m_ShiftBit);
            }
            return new FixedPoint(val);
        }

        public static FixedPoint operator /(FixedPoint v1, FixedPoint v2)
        {
            if (v2.m_ScaledValue == 0)
            {
                throw new Exception("divide by zero!");
            }
            return new FixedPoint((v1.m_ScaledValue << m_ShiftBit) / v2.m_ScaledValue);
        }

        public static bool operator == (FixedPoint v1, FixedPoint v2)
        {
            return v1.m_ScaledValue == v2.m_ScaledValue;
        }

        public static bool operator !=(FixedPoint v1, FixedPoint v2)
        {
            return v1.m_ScaledValue != v2.m_ScaledValue;
        }

        public static bool operator > (FixedPoint v1, FixedPoint v2)
        {
            return v1.m_ScaledValue > v2.m_ScaledValue;
        }

        public static bool operator <(FixedPoint v1, FixedPoint v2)
        {
            return v1.m_ScaledValue < v2.m_ScaledValue;
        }

        public static bool operator >=(FixedPoint v1, FixedPoint v2)
        {
            return v1.m_ScaledValue >= v2.m_ScaledValue;
        }

        public static bool operator <=(FixedPoint v1, FixedPoint v2)
        {
            return v1.m_ScaledValue <= v2.m_ScaledValue;
        }

        public static FixedPoint operator >>(FixedPoint v1, int bit)
        {
            if (v1.m_ScaledValue >= 0)
            {
                return new FixedPoint(v1.m_ScaledValue >> bit);
            }
            return new FixedPoint(-((-v1.m_ScaledValue) >> bit));
        }

        public static FixedPoint operator <<(FixedPoint v1, int bit)
        {
            return new FixedPoint(v1.m_ScaledValue << bit);
        }

        public readonly int CompareTo(FixedPoint p)
        {
            return m_ScaledValue.CompareTo(p.m_ScaledValue);
        }

        public readonly override bool Equals(object obj)
        {
            if (obj == null || obj is not FixedPoint o)
            {
                return false;
            }

            return o.m_ScaledValue == m_ScaledValue;
        }

        public readonly override int GetHashCode()
        {
            return m_ScaledValue.GetHashCode();
        }

        public readonly override string ToString()
        {
            return FloatValue.ToString();
        }

        public readonly FixedPoint Abs()
        {
            return m_ScaledValue >= 0 ? new FixedPoint(m_ScaledValue) : new FixedPoint(-m_ScaledValue);
        }

        private long m_ScaledValue = 0;
        private const int m_ShiftBit = 16;
        private const int m_ScaleFactor = 1 << m_ShiftBit;
    }
}
