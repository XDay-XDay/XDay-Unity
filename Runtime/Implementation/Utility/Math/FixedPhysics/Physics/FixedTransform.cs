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


namespace XDay
{
    internal readonly struct FixedTransform
    {
        public readonly static FixedTransform Zero = new FixedTransform(0, 0, 0);
        public readonly FixedPoint PositionX;
        public readonly FixedPoint PositionY;
        public readonly FixedPoint Sin;
        public readonly FixedPoint Cos;

        public FixedTransform(FixedVector2 position, FixedPoint angleInRadian)
        {
            PositionX = position.X;
            PositionY = position.Y;
            Sin = FixedMath.Sin(angleInRadian);
            Cos = FixedMath.Cos(angleInRadian);
        }

        public FixedTransform(FixedPoint x, FixedPoint y, FixedPoint angleInRadian)
        {
            PositionX = x;
            PositionY = y;
            Sin = FixedMath.Sin(angleInRadian);
            Cos = FixedMath.Cos(angleInRadian);
        }
    }
}
