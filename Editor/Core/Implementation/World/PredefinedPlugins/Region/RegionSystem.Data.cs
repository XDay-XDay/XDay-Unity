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

using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystem
    {
        public class SmoothRegionMeshGenerationParam
        {
            public SmoothRegionMeshGenerationParam(int lod,
                float segmentLengthRatio,
                float minTangentLength,
                float maxTangentLength,
                float pointDeltaDistance,
                int maxPointCountInOneSegment,
                float lineWidth,
                float gridErrorThreshold,
                Material edgeMaterial, Material regionMaterial)
            {
                SegmentLengthRatio = segmentLengthRatio;
                MinTangentLength = minTangentLength;
                MaxTangentLength = maxTangentLength;
                PointDeltaDistance = pointDeltaDistance;
                MaxPointCountInOneSegment = maxPointCountInOneSegment;
                LineWidth = lineWidth;
                EdgeMaterial = edgeMaterial;
                RegionMaterial = regionMaterial;
                GridErrorThreshold = gridErrorThreshold;
            }

            public float SegmentLengthRatio = 0.3f;
            public float MinTangentLength = 10.0f;
            public float MaxTangentLength = 30.0f;
            public float PointDeltaDistance = 10.0f;
            public int MaxPointCountInOneSegment = 10;
            public float LineWidth = 30;
            public float GridErrorThreshold;
            public Material EdgeMaterial;
            public Material RegionMaterial;
        }

        public class TerritorySharedEdgeInfo
        {
            public int TerritoryID;
            public int NeighbourTerritoryID;
            public string PrefabPath;
            public Material Material;
        }
    }
}
