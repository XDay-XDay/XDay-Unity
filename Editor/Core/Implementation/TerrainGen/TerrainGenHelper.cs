

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
