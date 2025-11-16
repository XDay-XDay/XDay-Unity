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
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystemLayer
    {
        public void FixGrids()
        {
            SimpleStopwatch w = new();
            EditorUtility.DisplayProgressBar("修复无效的格子", $"修复中...", 0);

            w.Begin();
            Dictionary<int, RegionObject> regions = new();
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                regions[m_Regions[i].ID] = m_Regions[i];
            }

            //填充斜边有格子但4周没格子的情况
            int gridTileIdx;
            for (int i = 0; i < m_VerticalGridCount; ++i)
            {
                for (int j = 0; j < m_HorizontalGridCount; ++j)
                {
                    gridTileIdx = i * m_HorizontalGridCount + j;
                    var type = m_GridData[gridTileIdx];
                    if (type != 0)
                    {
                        for (int k = 0; k < 4; ++k)
                        {
                            int nx = m_ObliqueNeighbours[k].x + j;
                            int ny = m_ObliqueNeighbours[k].y + i;
                            if (nx >= 0 && nx < m_HorizontalGridCount &&
                                ny >= 0 && ny < m_VerticalGridCount)
                            {
                                if (m_GridData[ny * m_HorizontalGridCount + nx] == type &&
                                    m_GridData[ny * m_HorizontalGridCount + j] != type &&
                                    m_GridData[i * m_HorizontalGridCount + nx] != type)
                                {
                                    m_GridData[ny * m_HorizontalGridCount + j] = type;
                                }
                            }
                        }
                    }
                }
            }

            var worldRoot = Object.FindObjectsByType<XDayWorldEditor>(FindObjectsSortMode.None)[0];

            //填充空格子
            for (int i = 0; i < m_VerticalGridCount; ++i)
            {
                for (int j = 0; j < m_HorizontalGridCount; ++j)
                {
                    var idx = i * m_HorizontalGridCount + j;
                    var type = m_GridData[idx];
                    if (type == 0)
                    {
                        Debug.LogError($"填充空格: {j}-{i}");
                        m_GridData[idx] = FindNearestValidRegionID(j, i);
                        Debug.Assert(m_GridData[idx] != 0);

                        var region = GetRegion(m_GridData[idx]);
                        var obj = new GameObject($"填充空格 (x:{j},y:{i}为{region.ConfigID})");
                        obj.transform.position = CoordinateToCenterPosition(j, i);
                        obj.transform.SetParent(worldRoot.transform, true);
                    }
                }
            }

            w.Stop();
            Debug.Log($"步骤1耗时: {w.ElapsedSeconds}秒");

            w.Begin();
            bool[] visitedTiles = new bool[m_VerticalGridCount * m_HorizontalGridCount];
            List<Vector2Int> connectedTiles = new();
            for (int i = 0; i < m_VerticalGridCount; ++i)
            {
                for (int j = 0; j < m_HorizontalGridCount; ++j)
                {
                    gridTileIdx = i * m_HorizontalGridCount + j;
                    var type = m_GridData[gridTileIdx];
                    if (type != 0)
                    {
                        if (visitedTiles[i * m_HorizontalGridCount + j] == false)
                        {
                            GetCollectedTiles(j, i, connectedTiles, visitedTiles);
                            if (connectedTiles.Count == 1)
                            {
                                int validNeighbourID = GetValidNeighbourID(j, i);
                                var region = GetRegion(validNeighbourID);
                                Debug.LogError($"只有一个格子的区域(x:{j},y:{i})修改成{(region == null ? 0 : region.ConfigID)}");
                                m_GridData[i * m_HorizontalGridCount + j] = validNeighbourID;
                                var obj = new GameObject($"一个格子的区域(x:{j},y:{i},修改成{region.ConfigID})");
                                obj.transform.position = CoordinateToCenterPosition(j, i);
                                obj.transform.SetParent(worldRoot.transform, true);
                            }
                            else if (connectedTiles.Count < 10)
                            {
                                Debug.LogError($"区域层{Name}层有无效的格子 (x:{j},y:{i})");
                            }
                        }
                    }
                }
            }

            w.Stop();
            Debug.Log($"步骤2耗时: {w.ElapsedSeconds}秒");

            m_Renderer.UpdateColors(0, 0, m_HorizontalGridCount - 1, m_VerticalGridCount - 1);

            EditorUtility.ClearProgressBar();
        }

        private int FindNearestValidRegionID(int j, int i)
        {
            m_TempStack.Clear();
            m_TempStack.Add(new Vector2Int(j, i));
            m_TempList.Clear();
            while (m_TempStack.Count > 0)
            {
                var cur = m_TempStack[^1];
                m_TempStack.RemoveAt(m_TempStack.Count - 1);

                var curID = m_GridData[cur.y * m_HorizontalGridCount + cur.x];
                if (curID != 0)
                {
                    return curID;
                }

                m_TempList.Add(cur);

                for (int k = 0; k < 4; ++k)
                {
                    int nx = m_Neighbours[k].x + cur.x;
                    int ny = m_Neighbours[k].y + cur.y;
                    var neighbour = new Vector2Int(nx, ny);
                    if (nx >= 0 && nx < m_HorizontalGridCount &&
                        ny >= 0 && ny < m_VerticalGridCount &&
                        !m_TempList.Contains(neighbour))
                    {
                        if (m_GridData[ny * m_HorizontalGridCount + nx] != 0)
                        {
                            m_TempStack.Add(neighbour);
                        }
                    }
                }
            }
            return 0;
        }

        private void GetCollectedTiles(int x, int y, List<Vector2Int> connectedTiles, bool[] visitedTiles)
        {
            connectedTiles.Clear();
            m_TempStack.Clear();
            m_TempStack.Add(new Vector2Int(x, y));
            while (m_TempStack.Count > 0)
            {
                var cur = m_TempStack[m_TempStack.Count - 1];
                m_TempStack.RemoveAt(m_TempStack.Count - 1);

                visitedTiles[cur.y * m_HorizontalGridCount + cur.x] = true;
                connectedTiles.Add(cur);

                var curID = m_GridData[cur.y * m_HorizontalGridCount + cur.x];
                for (int i = 0; i < 4; ++i)
                {
                    int nx = m_Neighbours[i].x + cur.x;
                    int ny = m_Neighbours[i].y + cur.y;
                    if (nx >= 0 && nx < m_HorizontalGridCount &&
                        ny >= 0 && ny < m_VerticalGridCount)
                    {
                        if (visitedTiles[ny * m_HorizontalGridCount + nx] == false)
                        {
                            if (m_GridData[ny * m_HorizontalGridCount + nx] == curID)
                            {
                                m_TempStack.Add(new Vector2Int(nx, ny));
                            }
                        }
                    }
                }
            }
        }

        private int GetValidNeighbourID(int x, int y)
        {
            for (int i = 0; i < 4; ++i)
            {
                int nx = m_Neighbours[i].x + x;
                int ny = m_Neighbours[i].y + y;
                if (nx >= 0 && nx < m_HorizontalGridCount &&
                    ny >= 0 && ny < m_VerticalGridCount)
                {
                    var idx = ny * m_HorizontalGridCount + nx;
                    if (m_GridData[idx] != 0)
                    {
                        return m_GridData[idx];
                    }
                }
            }
            return 0;
        }

        private readonly HashSet<Vector2Int> m_TempList = new();
        private readonly List<Vector2Int> m_ObliqueNeighbours = new() 
        {
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
        };
        private readonly List<Vector2Int> m_TempStack = new();
        private readonly List<Vector2Int> m_Neighbours = new() 
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
        };
    }
}
