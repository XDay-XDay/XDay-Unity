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
    internal static class FixedCosTable
    {
        static FixedCosTable()
        {
            int count = 10000;
            Value = new FixedPoint[count];
            for (var i = 0; i < count; i++)
            {
                float angle = (float)i / (count - 1) * 360.0f;
                Value[i] = new FixedPoint(MathF.Cos(angle / 180.0f * MathF.PI));
            }
        }

        //t between [0, 1]
        public static FixedPoint Sample(FixedPoint t)
        {
            var index = t * (Value.Length - 1);
            return Value[index.IntValue];
        }

        public static FixedPoint[] Value;
    }
}