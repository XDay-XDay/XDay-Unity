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



using System.Collections.Generic;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.Terrain.Editor
{
    internal static class TerrainGenHelper
    {
        public static void NormalizeHeights(List<float> heights)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            for (var i = 0; i < heights.Count; ++i)
            {
                if (heights[i] < min)
                {
                    min = heights[i];
                }

                if (heights[i] > max)
                {
                    max = heights[i];
                }
            }

            for (var i = 0; i < heights.Count; ++i)
            {
                var d = (max - min);
                if (Mathf.Approximately(d, 0))
                {
                    heights[i] = 0;
                }
                else
                {
                    heights[i] = (heights[i] - min) / d;
                }
                Debug.Assert(Helper.GE(heights[i], 0) && Helper.LE(heights[i], 1.0f));
            }
        }

        public const string TerrainGenObjTag = "TerrainGen-Obj";
    }
}
