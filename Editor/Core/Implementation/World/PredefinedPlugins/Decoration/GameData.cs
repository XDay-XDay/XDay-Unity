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
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Decoration.Editor
{
    internal class GameGridLODData
    {
        public List<int> DecorationMetadataIndices { get; set; } = new List<int>();
    }

    internal class GameGridData
    {
        public GameGridLODData[] LODs { get; set; }

        public GameGridData(int lodCount)
        {
            LODs = new GameGridLODData[lodCount];
            for (var lod = 0; lod < lodCount; ++lod)
            {
                LODs[lod] = new GameGridLODData();
            }
        }

        public void AddObjectToLOD(int lod, int objectIndex)
        {
            LODs[lod].DecorationMetadataIndices.Add(objectIndex);
        }
    }

    internal class GameResourceMetadata
    {
        public GameResourceMetadata(int gpuBatchID, Quaternion rotation, Vector3 scale, Rect bounds, string path)
        {
            GPUBatchID = gpuBatchID;
            Rotation = rotation;
            Scale = scale;
            Bounds = bounds;
            Path = path;
        }

        public int GPUBatchID { get; set; }
        public Quaternion Rotation { get; internal set; }
        public Vector3 Scale { get; internal set; }
        public Rect Bounds { get; internal set; }
        public string Path { get; internal set; }
    }

    internal class GameDecorationMetaData
    {
        public byte[] LODResourceChangeMasks { get; set; }
        public int[] ResourceMetadataIndex { get; set; }
        public Vector2[] Position { get; set; }
    }

    internal class Grid
    {
        public GameGridData[] Data => m_Data;

        public Grid(int lodCount, float gridWidth, float gridHeight, int xGridCount, int yGridCount, Rect bounds)
        {
            m_LODCount = lodCount;
            m_GridWidth = gridWidth;
            m_GridHeight = gridHeight;
            m_XGridCount = xGridCount;
            m_YGridCount = yGridCount;
            m_Bounds = bounds;
            m_Data = new GameGridData[m_XGridCount * m_YGridCount];
            for (var i = 0; i < m_Data.Length; ++i)
            {
                m_Data[i] = new GameGridData(lodCount);
            }
        }

        public void Add(DecorationObject decoration)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(decoration.ResourceDescriptor.GetPath(0));
            var localBounds = prefab.QueryRectWithLocalScaleAndRotation(decoration.Rotation, decoration.Scale);

            var worldBounds = new Rect(decoration.Position.ToVector2() + localBounds.min, localBounds.size);
            var min = PositionToCoordinate(worldBounds.min.ToVector3XZ());
            var max = PositionToCoordinate(worldBounds.max.ToVector3XZ());

            for (var y = min.y; y <= max.y; ++y)
            {
                for (var x = min.x; x <= max.x; ++x)
                {
                    if (x >= 0 && x < m_XGridCount &&
                        y >= 0 && y < m_YGridCount)
                    {
                        for (var lod = 0; lod < m_LODCount; ++lod)
                        {
                            if (decoration.ExistsInLOD(lod))
                            {
                                m_Data[y * m_XGridCount + x].AddObjectToLOD(lod, decoration.RuntimeObjectIndex);
                            }
                        }
                    }
                }
            }
        }

        private Vector2Int PositionToCoordinate(Vector3 position)
        {
            var localX = position.x - m_Bounds.xMin;
            var localZ = position.z - m_Bounds.yMin;
            return new Vector2Int(Mathf.FloorToInt(localX / m_GridWidth), Mathf.FloorToInt(localZ / m_GridHeight));
        }

        private readonly GameGridData[] m_Data;
        private Rect m_Bounds;
        private readonly int m_LODCount;
        private readonly float m_GridWidth;
        private readonly float m_GridHeight;
        private readonly int m_XGridCount;
        private readonly int m_YGridCount;
    }
}


//XDay