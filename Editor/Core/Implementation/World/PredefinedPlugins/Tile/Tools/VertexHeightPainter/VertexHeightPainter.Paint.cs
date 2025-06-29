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

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;
using XDay.UtilityAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    internal enum TerrainTileEdgeCorner
    {
        BottomEdge,
        TopEdge,
        LeftEdge,
        RightEdge,
        LeftBottomCorner,
        RightBottomCorner,
        LeftTopCorner,
        RightTopCorner,
    }

    internal partial class VertexHeightPainter
    {
        bool LockEdgeVertexHeight()
        {
            return 
                m_PaintHeightParameters.PaintInOneTile || 
                m_PaintHeightParameters.KeepEdgeVertexHeight;
        }

        void OnInspectorGUIPaintHeight()
        {
            m_TileSystem.ClipMinHeight = EditorGUILayout.FloatField(new GUIContent("Clip Minimum Height", "高度低于该值的Quad会被删除"), m_TileSystem.ClipMinHeight);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Clear Clip Mask", "")))
            {
                if (EditorUtility.DisplayDialog("Warning", "Are you sure?", "Yes", "No"))
                {
#if ENABLE_CLIP_MASK
                    m_TileSystem.ClearClipMask();
#endif
                }
            }
            if (GUILayout.Button(new GUIContent("Hide Clip Mask", "")))
            {
#if ENABLE_CLIP_MASK
                m_TileSystem.ShowClipMask(false);
#endif
            }
            if (GUILayout.Button(new GUIContent("Show Clip Mask", "")))
            {
#if ENABLE_CLIP_MASK
                m_TileSystem.ShowClipMask(true);
#endif
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button(new GUIContent("Validate", "检查是否能绘制地表高度")))
            {
                string msg = m_TileSystem.CheckIfGetReadyToPaintHeight();
                if (!string.IsNullOrEmpty(msg))
                {
                    EditorUtility.DisplayDialog("Error", msg, "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("", "可以绘制高度", "OK");
                }
            }

            m_TileSystem.GetGroundHeightInGame = EditorGUILayout.ToggleLeft(new GUIContent("游戏内获取地形高度数据", "是否在运行游戏时能计算地表高度数据,勾上会增大内存占用"), m_TileSystem.GetGroundHeightInGame);
            m_TileSystem.GenerateMeshCollider = EditorGUILayout.ToggleLeft(new GUIContent("生成地形碰撞体", "是否在地表高度不为0的Tile上生成MeshCollider"), m_TileSystem.GenerateMeshCollider);
            m_TileSystem.OptimizeMesh = EditorGUILayout.ToggleLeft(new GUIContent("优化Mesh", "是否导出时优化Mesh,优化后边缘顶点法线可能会不准确"), m_TileSystem.OptimizeMesh);
            m_TileSystem.GenerateObjMesh = EditorGUILayout.ToggleLeft(new GUIContent("生成Mesh OBJ文件", "是否生成Mesh的OBJ文件"), m_TileSystem.GenerateObjMesh);
            m_TileSystem.CreateHeightMeshAsPrefab = EditorGUILayout.ToggleLeft(new GUIContent("将地表Mesh单独生成Prefab", "将地表Mesh带高度的部分生成单独的prefab"), m_TileSystem.CreateHeightMeshAsPrefab);
            m_TileSystem.CreateMaterialForGroundPrefab = EditorGUILayout.ToggleLeft(new GUIContent("为地形Prefab生成材质", "是否为每个地形Prefab生成材质"), m_TileSystem.CreateMaterialForGroundPrefab);

            m_BrushStyleManager.InspectorGUI();
        }

        CoordInfo GetCoordinateForSuppressVertexHeight(int x, int y, TileSystem m_TileSystem)
        {
            var tile = m_TileSystem.GetTile(x, y);
            if (tile == null || tile.VertexHeights == null)
            {
                return null;
            }

            CoordInfo coord = new()
            {
                tileCoord = new Vector2Int(x, y)
            };

            //不影响edge
            int minX = 1;
            int minY = 1;
            int maxX = tile.MeshResolution - 1;
            int maxY = tile.MeshResolution - 1;

            var rangeInTile = new RectInt(minX, minY, maxX - minX, maxY - minY);
            //计算tile于笔刷的顶点intersection
            coord.rangeInTile = rangeInTile;

            coord.brushHeights.Clear();
            int width = rangeInTile.width;
            int height = rangeInTile.height;
            var oldHeights = tile.VertexHeights;

            for (int i = 0; i <= height; ++i)
            {
                for (int j = 0; j <= width; ++j)
                {
                    int idx = (i + minY) * (tile.MeshResolution + 1) + j + minX;
                    float oldHeight = oldHeights[idx];
                    if (Mathf.Abs(oldHeight) < m_PaintHeightParameters.SupressVertexHeightThreshold)
                    {
                        oldHeight = 0;
                    }
                    coord.brushHeights.Add(oldHeight);
                }
            }

            return coord;
        }

        void SuppressAllTileVertexHeight(TileSystem m_TileSystem)
        {
            m_PaintHeightParameters.TileCoords.Clear();

            int horizontalTileCount = m_TileSystem.XTileCount;
            int verticaltileCount = m_TileSystem.YTileCount;
            for (int y = 0; y < verticaltileCount; ++y)
            {
                for (int x = 0; x < horizontalTileCount; ++x)
                {
                    var coord = GetCoordinateForSuppressVertexHeight(x, y, m_TileSystem);

                    if (coord != null)
                    {
                        m_PaintHeightParameters.TileCoords.Add(coord);
                    }
                }
            }

            SupressVertexHeight(true, m_TileSystem);
        }

        private void SupressVertexHeight(bool isMouseDown, TileSystem m_TileSystem)
        {
            UndoSystem.NextGroupAndJoin();
            if (m_PaintHeightParameters.TileCoords.Count > 0)
            {
                for (int i = 0; i < m_PaintHeightParameters.TileCoords.Count; ++i)
                {
                    var tile = m_TileSystem.GetTile(m_PaintHeightParameters.TileCoords[i].tileCoord.x, m_PaintHeightParameters.TileCoords[i].tileCoord.y);
                    SetHeight(m_PaintHeightParameters.TileCoords[i], tile.MeshResolution, m_TileSystem);
                }

                //FixEdge(actions, m_PaintHeightParameters.tileCoords);
            }
        }

        private void OnSceneGUIPaintHeight()
        {
            if (m_BrushStyleManager.SelectedStyle == null)
            {
                return;
            }

            var currentEvent = Event.current;
            var worldPos = EditorHelper.MousePositionToWorldRay(currentEvent.mousePosition, out Ray ray);
            var camera = SceneView.currentDrawingSceneView.camera;
            var root = WorldEditor.WorldManager.FirstWorld.Root;

            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.keyCode == KeyCode.B)
                {
                    m_PaintHeightParameters.SmoothBrush = !m_PaintHeightParameters.SmoothBrush;
                    SceneView.RepaintAll();
                }
                if (currentEvent.keyCode == KeyCode.R)
                {
                    m_TileSystem.FixNormal();
                }
            }

            var parameters = m_PaintHeightParameters;

            var origin = camera.transform.position;
            var physxEngine = UnityEngine.Object.FindAnyObjectByType<PhysxSetup>();
            bool hit = physxEngine.Raycast(origin, ray.direction, out Vector3 intersection, out Vector3 normal);
            if (hit && parameters.Mode != HeightMode.SetClipMask)
            {
                worldPos = intersection;
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 && currentEvent.alt == false)
            {
                if (parameters.Mode == HeightMode.ShowTileResolution)
                {
                    var coord = m_TileSystem.RotatedPositionToCoordinate(worldPos.x, worldPos.z);
                    var tile = m_TileSystem.GetTile(coord.x, coord.y);
                    if (tile != null)
                    {
                        EditorUtility.DisplayDialog("", $"Tile的Mesh分辨率为{tile.MeshResolution}X{tile.MeshResolution}", "确定");
                    }
                }

                FixEdge(m_TileSystem);
            }

            if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) &&
                currentEvent.button == 0 && 
                currentEvent.alt == false)
            {
                //点击鼠标左键
                bool lowerTerrain = currentEvent.control;
                if (parameters.Mode == HeightMode.ChangeHeight ||
                    parameters.Mode == HeightMode.SetHeight)
                {
                    bool paint = true;
                    float targetHeight = 0;
                    if (parameters.Mode == HeightMode.SetHeight)
                    {
                        if (currentEvent.control)
                        {
                            m_PaintHeightParameters.TargetHeight = worldPos.y;
                            paint = false;
                            SceneView.RepaintAll();
                        }
                        targetHeight = m_PaintHeightParameters.TargetHeight;
                    }
                    if (paint)
                    {
                        GetCoordinates(worldPos, m_PaintHeightParameters.Range, targetHeight, lowerTerrain, parameters.Mode);
                        if (parameters.TileCoords.Count > 0)
                        {
                            bool mouseDown = currentEvent.type == EventType.MouseDown;
                            if (mouseDown)
                            {
                                m_TilesNeedFixEdge.Clear();
                            }

                            for (int i = 0; i < parameters.TileCoords.Count; ++i)
                            {
                                SetHeight(parameters.TileCoords[i], m_PaintHeightParameters.Resolution, m_TileSystem);
                            }

                            CheckEdge(parameters.TileCoords, m_TileSystem);
                        }
                    }
                }
                else if (parameters.Mode == HeightMode.SetTileResolution)
                {
                    if (currentEvent.type == EventType.MouseDown)
                    {
                        UndoSystem.NextGroupAndJoin();
                        GetCoordinates(worldPos, m_PaintHeightParameters.Range, 0, lowerTerrain, parameters.Mode);
                        if (parameters.TileCoords.Count > 0)
                        {
                            for (int i = 0; i < parameters.TileCoords.Count; ++i)
                            {
                                SetResolution(parameters.TileCoords[i].tileCoord.x, parameters.TileCoords[i].tileCoord.y, m_PaintHeightParameters.Resolution, m_TileSystem);
                            }

                            CheckEdge(parameters.TileCoords, m_TileSystem);
                        }
                    }
                }
                else if (parameters.Mode == HeightMode.SuppressVertex)
                {
                    parameters.TileCoords.Clear();
                    var coord = m_TileSystem.RotatedPositionToCoordinate(worldPos.x, worldPos.z);
                    var coordInfo = GetCoordinateForSuppressVertexHeight(coord.x, coord.y, m_TileSystem);
                    if (coordInfo != null)
                    {
                        parameters.TileCoords.Add(coordInfo);
                    }
                    SupressVertexHeight(currentEvent.type == EventType.MouseDown, m_TileSystem);
                }
                else if (parameters.Mode == HeightMode.ResetHeight)
                {
                    if (currentEvent.type == EventType.MouseDown)
                    {
                        UndoSystem.NextGroupAndJoin();
                        GetCoordinates(worldPos, m_PaintHeightParameters.Range, 0, lowerTerrain, HeightMode.ChangeHeight);
                        if (parameters.TileCoords.Count > 0)
                        {
                            for (int i = 0; i < parameters.TileCoords.Count; ++i)
                            {
                                ResetHeight(parameters.TileCoords[i], m_PaintHeightParameters.Resolution, m_TileSystem);
                            }

                            CheckEdge(parameters.TileCoords, m_TileSystem);
                        }
                    }
                }
                else if (parameters.Mode == HeightMode.Smooth)
                {
                    //平滑选中的顶点
                    DoSmooth(worldPos, currentEvent.type == EventType.MouseDown, m_TileSystem);
                }
                else if (parameters.Mode == HeightMode.ResetEdgeHeight)
                {
                    if (currentEvent.type == EventType.MouseDown)
                    {
                        UndoSystem.NextGroupAndJoin();
                        GetCoordinates(worldPos, m_PaintHeightParameters.Range, 0, lowerTerrain, HeightMode.ChangeHeight);
                        for (int i = 0; i < parameters.TileCoords.Count; ++i)
                        {
                            ResetEdgeHeight(parameters.TileCoords[i]);
                        }
                    }
                }
                else if (parameters.Mode == HeightMode.SetClipMask)
                {
                    SetClipMask(worldPos, currentEvent.control);
                }
            }

            if (parameters.Mode != HeightMode.SetClipMask)
            {
                if (parameters.Mode == HeightMode.Smooth)
                {
                    Handles.DrawWireDisc(worldPos, Vector3.up, m_PaintHeightParameters.Range * 0.5f);
                }
                else
                {
                    Handles.DrawLine(worldPos, worldPos + normal * 10);
                    Handles.DrawWireDisc(worldPos, normal, m_PaintHeightParameters.Range * 0.5f);
                }
            }
            else
            {
                var coord = m_TileSystem.RotatedPositionToCoordinate(worldPos.x, worldPos.z);
                var tile = m_TileSystem.GetTile(coord.x, coord.y);
                if (tile != null)
                {
                    var localPos = worldPos - tile.Position;
                    float gridSize = m_TileSystem.TileWidth / tile.MeshResolution;
                    float x = localPos.x - localPos.x % gridSize + gridSize * 0.5f;
                    float z = localPos.z - localPos.z % gridSize + gridSize * 0.5f;
                    Handles.DrawWireCube(new Vector3(x, 0, z) + tile.Position, new Vector3(gridSize, 0, gridSize));
                }
            }

            SceneView.RepaintAll();
        }

        Rect GetTileWorldRect(int x, int y)
        {
            var pos = m_TileSystem.CoordinateToUnrotatedPosition(x, y);
            return new Rect(pos.x, pos.z, m_TileSystem.TileWidth, m_TileSystem.TileHeight);
        }

        Vector2Int FromWorldPositionToCoordinate(float x, float z, int resolution)
        {
            float gridSize = m_TileSystem.TileWidth / resolution;
            return new Vector2Int(Mathf.FloorToInt(x / gridSize), Mathf.FloorToInt(z / gridSize));
        }

        private void GetCoordinates(Vector3 worldPos, float brushSize, float targetHeight, bool lowerTerrain, HeightMode mode)
        {
            m_PaintHeightParameters.TileCoords.Clear();
            var minPosX = worldPos.x - brushSize * 0.5f;
            var minPosZ = worldPos.z - brushSize * 0.5f;
            var maxPosX = worldPos.x + brushSize * 0.5f;
            var maxPosZ = worldPos.z + brushSize * 0.5f;
            var minCoord = m_TileSystem.UnrotatedPositionToCoordinate(minPosX, minPosZ);
            var maxCoord = m_TileSystem.UnrotatedPositionToCoordinate(maxPosX, maxPosZ);
            if (m_PaintHeightParameters.PaintInOneTile)
            {
                minCoord = m_TileSystem.RotatedPositionToCoordinate(worldPos.x, worldPos.z);
                maxCoord = minCoord;
            }
            Rect brushRect = new Rect(minPosX, minPosZ, maxPosX - minPosX, maxPosZ - minPosZ);
            var brush = m_BrushStyleManager.SelectedStyle;
            int mipmap = brush.CalculateMipMapLevel(false, (int)brushSize);
            float gridSize = m_TileSystem.TileWidth / m_PaintHeightParameters.Resolution;
            float brushRectMinX = brushRect.x;
            float brushRectMinZ = brushRect.y;
            float changeHeightFactor = lowerTerrain ? -10.0f : 10.0f;

            Func<bool, float, float, int, float> sampleFunc;
            if (m_PaintHeightParameters.SmoothBrush)
            {
                sampleFunc = brush.TextureLODLinearAlpha;
            }
            else
            {
                sampleFunc = brush.TextureLODPointAlpha;
            }

            bool lockVertexHeight = LockEdgeVertexHeight();
            for (int y = minCoord.y; y <= maxCoord.y; ++y)
            {
                for (int x = minCoord.x; x <= maxCoord.x; ++x)
                {
                    if (x >= 0 && x < m_TileSystem.XTileCount && y >= 0 && y < m_TileSystem.YTileCount)
                    {
                        var tile = m_TileSystem.GetTile(x, y);
                        if (tile != null)
                        {
                            Rect tileRect = GetTileWorldRect(x, y);
                            Rect intersection;
                            bool intersected = Helper.GetRectIntersection(tileRect, brushRect, out intersection);
                            Debug.Assert(intersected);
                            var tileStartPos = m_TileSystem.CoordinateToUnrotatedPosition(x, y);
                            var intersectMinCoord = FromWorldPositionToCoordinate(intersection.xMin - tileStartPos.x, intersection.yMin - tileStartPos.z, m_PaintHeightParameters.Resolution);
                            float intersectionMaxX = intersection.xMax + 0.5f;
                            float intersectionMaxY = intersection.yMax + 0.5f;
                            var intersectMaxCoord = FromWorldPositionToCoordinate(intersectionMaxX - tileStartPos.x, intersectionMaxY - tileStartPos.z, m_PaintHeightParameters.Resolution);

                            CoordInfo coord = new CoordInfo();
                            coord.tileCoord = new Vector2Int(x, y);

                            int minX = intersectMinCoord.x;
                            int minY = intersectMinCoord.y;
                            int maxX = intersectMaxCoord.x;
                            int maxY = intersectMaxCoord.y;
                            if (lockVertexHeight)
                            {
                                minY = Mathf.Max(1, minY);
                                minX = Mathf.Max(1, minX);
                                maxY = Mathf.Min(m_PaintHeightParameters.Resolution - 1, maxY);
                                maxX = Mathf.Min(m_PaintHeightParameters.Resolution - 1, maxX);
                            }
                            var rangeInTile = new RectInt(minX, minY, maxX - minX, maxY - minY);
                            int width = rangeInTile.width;
                            int height = rangeInTile.height;
                            if (width >= 0 && height >= 0)
                            {
                                //计算tile于笔刷的顶点intersection
                                coord.rangeInTile = rangeInTile;

                                coord.brushHeights.Clear();
                                var oldHeights = tile.VertexHeights;
                                bool invalidOldHeight = false;
                                if (oldHeights == null || oldHeights.Length != (m_PaintHeightParameters.Resolution + 1) * (m_PaintHeightParameters.Resolution + 1))
                                {
                                    invalidOldHeight = true;
                                }

                                bool addCoord = true;
                                if (mode == HeightMode.SuppressVertex)
                                {
                                    if (invalidOldHeight)
                                    {
                                        addCoord = false;
                                    }
                                    else
                                    {
                                        for (int i = 0; i <= height; ++i)
                                        {
                                            for (int j = 0; j <= width; ++j)
                                            {
                                                int idx = (i + minY) * (m_PaintHeightParameters.Resolution + 1) + j + minX;
                                                float oldHeight = oldHeights[idx];
                                                if (Mathf.Abs(oldHeight) < m_PaintHeightParameters.SupressVertexHeightThreshold)
                                                {
                                                    oldHeight = 0;
                                                }
                                                coord.brushHeights.Add(oldHeight);
                                            }
                                        }
                                    }
                                }
                                else if (mode == HeightMode.ChangeHeight)
                                {
                                    //使用change height模式
                                    for (int i = 0; i <= height; ++i)
                                    {
                                        for (int j = 0; j <= width; ++j)
                                        {
                                            float posX = tileStartPos.x + (j + rangeInTile.x) * gridSize;
                                            float posZ = tileStartPos.z + (i + rangeInTile.y) * gridSize;
                                            float rx = (posX - brushRectMinX) / brushSize;
                                            float ry = (posZ - brushRectMinZ) / brushSize;
                                            int idx = (i + minY) * (m_PaintHeightParameters.Resolution + 1) + j + minX;
                                            float sampleValue = sampleFunc(false, rx, ry, mipmap);
                                            if (invalidOldHeight)
                                            {
                                                //直接设置高度
                                                coord.brushHeights.Add(sampleValue * changeHeightFactor * m_PaintHeightParameters.Intensity);
                                            }
                                            else
                                            {
                                                //修改高度
                                                float oldHeight = oldHeights[idx];
                                                coord.brushHeights.Add(oldHeight + sampleValue * changeHeightFactor * m_PaintHeightParameters.Intensity);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //使用set height模式
                                    for (int i = 0; i <= height; ++i)
                                    {
                                        for (int j = 0; j <= width; ++j)
                                        {
                                            float posX = tileStartPos.x + (j + rangeInTile.x) * gridSize;
                                            float posZ = tileStartPos.z + (i + rangeInTile.y) * gridSize;
                                            float rx = (posX - brushRectMinX) / brushSize;
                                            float ry = (posZ - brushRectMinZ) / brushSize;
                                            int idx = (i + minY) * (m_PaintHeightParameters.Resolution + 1) + j + minX;
                                            float sampleValue = sampleFunc(false, rx, ry, mipmap);
                                            if (invalidOldHeight)
                                            {
                                                //直接设置高度
                                                float newHeight = Mathf.Min(targetHeight, sampleValue * changeHeightFactor * m_PaintHeightParameters.Intensity);
                                                coord.brushHeights.Add(newHeight);
                                            }
                                            else
                                            {
                                                //修改高度
                                                float oldHeight = oldHeights[idx];
                                                float deltaHeight = sampleValue * changeHeightFactor * m_PaintHeightParameters.Intensity;
                                                if (oldHeight > targetHeight)
                                                {
                                                    oldHeight -= deltaHeight;
                                                    if (oldHeight < targetHeight)
                                                    {
                                                        oldHeight = targetHeight;
                                                    }
                                                }
                                                else
                                                {
                                                    oldHeight += deltaHeight;
                                                    if (oldHeight > targetHeight)
                                                    {
                                                        oldHeight = targetHeight;
                                                    }
                                                }

                                                coord.brushHeights.Add(oldHeight);
                                            }
                                        }
                                    }
                                }

                                if (addCoord)
                                {
                                    m_PaintHeightParameters.TileCoords.Add(coord);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SetResolution(int tileX, int tileY, int resolution, TileSystem m_TileSystem)
        {
            var tile = m_TileSystem.GetTile(tileX, tileY);
            if (tile != null)
            {
                var actionSetResolution = new UndoActionSetTileMeshResolution($"Set Tile Mesh Resolution ({tileX},{tileY})", UndoSystem.Group, m_TileSystem.ID, tileX, tileY, resolution, true);
                UndoSystem.PerformCustomAction(actionSetResolution, true);
            }
        }

        private void SetHeight(CoordInfo tileCoord, int resolution, TileSystem m_TileSystem)
        {
            int tileX = tileCoord.tileCoord.x;
            int tileY = tileCoord.tileCoord.y;
            int minX = tileCoord.rangeInTile.min.x;
            int minY = tileCoord.rangeInTile.min.y;
            int maxX = tileCoord.rangeInTile.max.x;
            int maxY = tileCoord.rangeInTile.max.y;
            SetResolutionAndHeights("SetHeight", tileX, tileY, minX, minY, maxX, maxY, resolution, tileCoord.brushHeights, 0, LockEdgeVertexHeight(), true);
        }

        private void ResetHeight(CoordInfo tileCoord, int resolution, TileSystem m_TileSystem)
        {
            int tileX = tileCoord.tileCoord.x;
            int tileY = tileCoord.tileCoord.y;
            int minX = 0;
            int minY = 0;
            int maxX = resolution;
            int maxY = resolution;
            SetResolutionAndHeights("ResetHeight", tileX, tileY, minX, minY, maxX, maxY, resolution, null, 0, LockEdgeVertexHeight(), true);
        }

        bool In(int x, int y, List<TileModificationInfo> coords)
        {
            var v = new Vector2Int(x, y);
            for (int i = 0; i < coords.Count; ++i)
            {
                if (coords[i].Coordinate == v)
                {
                    return true;
                }
            }
            return false;
        }

        bool In(int x, int y, List<CoordInfo> coords)
        {
            var v = new Vector2Int(x, y);
            for (int i = 0; i < coords.Count; ++i)
            {
                if (coords[i].tileCoord == v)
                {
                    return true;
                }
            }
            return false;
        }

        bool FixEdge(TileSystem m_TileSystem)
        {
            bool edgeFixed = false;
            if (!m_PaintHeightParameters.PaintInOneTile)
            {
                edgeFixed = FixEdgeWithZeroHeight();
                m_TilesNeedFixEdge.Clear();
            }
            return edgeFixed;
        }

        //将tile和neighbour弄成一样的分辨率
        void MakeSameHeightAndResolution(
            string title,
            TileSystem m_TileSystem, 
            int tileX, 
            int tileY, 
            int tx, 
            int ty, 
            int neighbourTileX, 
            int neighbourTileY, 
            int nx, 
            int ny, 
            int newResolution)
        {
            var tile = m_TileSystem.GetTile(tileX, tileY);
            float h = tile.GetHeight(tx, ty);

            SetResolutionAndHeights(title, tileX, tileY, nx, ny, nx, ny, newResolution, null, h, false, true);
        }

        //将tile和neighbour弄成一样的分辨率
        void MakeSameHeightAndResolution(string title, TileSystem m_TileSystem, int tileX, int tileY, int tMinX, int tMinY, int tMaxX, int tMaxY, int neighbourTileX, int neighbourTileY, int nMinX, int nMinY, int newResolution)
        {
            var tile = m_TileSystem.GetTile(tileX, tileY);
            m_TempList.Clear();
            for (int i = tMinY; i <= tMaxY; ++i)
            {
                for (int j = tMinX; j <= tMaxX; ++j)
                {
                    float h = tile.GetHeight(j, i);
                    m_TempList.Add(h);
                }
            }

            SetResolutionAndHeights(
                title,
                neighbourTileX, neighbourTileY, 
                nMinX,
                nMinY, 
                nMinX + tMaxX - tMinX, 
                nMinY + tMaxY - tMinY, 
                newResolution, m_TempList, 0, false, true);
            m_TempList.Clear();
            //var act = new ActionChangeVaryingTileSizeTerrainHeightsAndResolution(mLayerLogic.layerID, neighbourTileX, neighbourTileY, nMinX, nMinY, nMinX + tMaxX - tMinX, nMinY + tMaxY - tMinY, newResolution);
            //act.Begin();
            //for (int i = tMinY; i <= tMaxY; ++i)
            //{
            //    for (int j = tMinX; j <= tMaxX; ++j)
            //    {
            //        float h = tile.GetHeight(j, i);
            //        int x = nMinX + j - tMinX;
            //        int y = nMinY + i - tMinY;
            //        m_TileSystem.SetHeight(neighbourTileX, neighbourTileY, x, y, x, y, newResolution, h, true, false);
            //    }
            //}
            //act.End();
            //actions.Add(act);
        }

        TileModificationInfo AddOrGetTileModificationInfo(int x, int y)
        {
            for (int i = 0; i < m_TilesNeedFixEdge.Count; ++i)
            {
                if (m_TilesNeedFixEdge[i].Coordinate.x == x &&
                    m_TilesNeedFixEdge[i].Coordinate.y == y)
                {
                    return m_TilesNeedFixEdge[i];
                }
            }

            TileModificationInfo info = new TileModificationInfo();
            info.Coordinate = new Vector2Int(x, y);
            m_TilesNeedFixEdge.Add(info);

            return info;
        }

        //检查edge的哪些边被修改了
        void CheckEdge(List<CoordInfo> coords, TileSystem m_TileSystem)
        {
            int h = m_TileSystem.XTileCount;
            int v = m_TileSystem.YTileCount;

            for (int i = 0; i < coords.Count; ++i)
            {
                var tileCoord = coords[i];
                int tileX = tileCoord.tileCoord.x;
                int tileY = tileCoord.tileCoord.y;
                var tile = m_TileSystem.GetTile(tileX, tileY);
                if (tile != null)
                {
                    TileModificationInfo tileInfo = AddOrGetTileModificationInfo(tileX, tileY);
                    //check 8 neighbours
                    for (int oy = -1; oy <= 1; ++oy)
                    {
                        for (int ox = -1; ox <= 1; ++ox)
                        {
                            var nx = tileX + ox;
                            var ny = tileY + oy;
                            if (nx >= 0 && nx < h && ny >= 0 && ny < v)
                            {
                                if (!In(nx, ny, coords))
                                {
                                    var neighbourTile = m_TileSystem.GetTile(nx, ny);
                                    if (neighbourTile != null)
                                    {
                                        int nr = neighbourTile.MeshResolution;
                                        int tr = tile.MeshResolution;
                                        if (neighbourTile != null)
                                        {
                                            if (ox == 1 && oy == 1)
                                            {
                                                //right top neighbour
                                                if (tileCoord.IsEdgeModified(tr, TerrainTileEdgeCorner.RightTopCorner))
                                                {
                                                    tileInfo.ModifiedEdges |= TerrainTileEdgeCorner.RightTopCorner;
                                                }
                                            }
                                            else if (ox == 1 && oy == -1)
                                            {
                                                //right bottom neighbour
                                                if (tileCoord.IsEdgeModified(tr, TerrainTileEdgeCorner.RightBottomCorner))
                                                {
                                                    tileInfo.ModifiedEdges |= TerrainTileEdgeCorner.RightBottomCorner;
                                                }
                                            }
                                            else if (ox == 1 && oy == 0)
                                            {
                                                //right neighbour
                                                if (tileCoord.IsEdgeModified(tr, TerrainTileEdgeCorner.RightEdge))
                                                {
                                                    tileInfo.ModifiedEdges |= TerrainTileEdgeCorner.RightEdge;
                                                }
                                            }
                                            else if (ox == -1 && oy == 1)
                                            {
                                                //left top neighbour
                                                if (tileCoord.IsEdgeModified(tr, TerrainTileEdgeCorner.LeftTopCorner))
                                                {
                                                    tileInfo.ModifiedEdges |= TerrainTileEdgeCorner.LeftTopCorner;
                                                }
                                            }
                                            else if (ox == -1 && oy == -1)
                                            {
                                                //left bottom neighbour
                                                if (tileCoord.IsEdgeModified(tr, TerrainTileEdgeCorner.LeftBottomCorner))
                                                {
                                                    tileInfo.ModifiedEdges |= TerrainTileEdgeCorner.LeftBottomCorner;
                                                }
                                            }
                                            else if (ox == -1 && oy == 0)
                                            {
                                                //left neighbour
                                                if (tileCoord.IsEdgeModified(tr, TerrainTileEdgeCorner.LeftEdge))
                                                {
                                                    tileInfo.ModifiedEdges |= TerrainTileEdgeCorner.LeftEdge;
                                                }
                                            }
                                            else if (ox == 0 && oy == 1)
                                            {
                                                //top neighbour
                                                if (tileCoord.IsEdgeModified(tr, TerrainTileEdgeCorner.TopEdge))
                                                {
                                                    tileInfo.ModifiedEdges |= TerrainTileEdgeCorner.TopEdge;
                                                }
                                            }
                                            else if (ox == 0 && oy == -1)
                                            {
                                                //bottom neighbour
                                                if (tileCoord.IsEdgeModified(tr, TerrainTileEdgeCorner.BottomEdge))
                                                {
                                                    tileInfo.ModifiedEdges |= TerrainTileEdgeCorner.BottomEdge;
                                                }
                                            }
                                            else
                                            {
                                                Debug.Assert(false, "unknown");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /*
         * 修复的edge的逻辑有2点:
         * 1.如果某个tile的edge顶点被修改了,则将他的neighbour的resolution设置成相同,然后将邻接边的顶点设置为相同高度,否则用2
         * 2.如果这些tiles的相邻tile resolution不同,则将它们的邻接边的edge顶点高度设置为0,避免有接缝问题
        */
        bool FixEdgeWithZeroHeight()
        {
            int h = m_TileSystem.XTileCount;
            int v = m_TileSystem.YTileCount;
            for (int i = 0; i < m_TilesNeedFixEdge.Count; ++i)
            {
                var tileCoord = m_TilesNeedFixEdge[i];
                int tileX = tileCoord.Coordinate.x;
                int tileY = tileCoord.Coordinate.y;
                var tile = m_TileSystem.GetTile(tileX, tileY);
                if (tile != null)
                {
                    //check 8 neighbours
                    for (int oy = -1; oy <= 1; ++oy)
                    {
                        for (int ox = -1; ox <= 1; ++ox)
                        {
                            var nx = tileX + ox;
                            var ny = tileY + oy;
                            if (nx >= 0 && nx < h && ny >= 0 && ny < v)
                            {
                                if (!In(nx, ny, m_TilesNeedFixEdge))
                                {
                                    var neighbourTile = m_TileSystem.GetTile(nx, ny);
                                    if (neighbourTile != null)
                                    {
                                        int nr = neighbourTile.MeshResolution;
                                        int tr = tile.MeshResolution;
                                        if (neighbourTile != null)
                                        {
                                            if (ox == 1 && oy == 1)
                                            {
                                                //right top neighbour
                                                if (tileCoord.ModifiedEdges.HasFlag(TerrainTileEdgeCorner.RightTopCorner))
                                                {
                                                    MakeSameHeightAndResolution("MakeSameHeightAndResolution-FixEdgeWithZeroHeight1", m_TileSystem, tileX, tileY, tr, tr, nx, ny, 0, 0, tr);
                                                }
                                                else
                                                {
                                                    if (nr != tr)
                                                    {
                                                        SetResolutionAndHeights("FixEdgeWithZeroHeight1", nx, ny, 0, 0, 0, 0, nr, null, 0, false, true);
                                                    }
                                                }
                                            }
                                            else if (ox == 1 && oy == -1)
                                            {
                                                //right bottom neighbour
                                                if (tileCoord.ModifiedEdges.HasFlag(TerrainTileEdgeCorner.RightBottomCorner))
                                                {
                                                    MakeSameHeightAndResolution("MakeSameHeightAndResolution-FixEdgeWithZeroHeight2", m_TileSystem, tileX, tileY, tr, 0, nx, ny, 0, tr, tr);
                                                }
                                                else
                                                {
                                                    if (nr != tr)
                                                    {
                                                        SetResolutionAndHeights("FixEdgeWithZeroHeight2", nx, ny, 0, nr, 0, nr, nr, null, 0, false, true);
                                                    }
                                                }
                                            }
                                            else if (ox == 1 && oy == 0)
                                            {
                                                //right neighbour
                                                if (tileCoord.ModifiedEdges.HasFlag(TerrainTileEdgeCorner.RightEdge))
                                                {
                                                    MakeSameHeightAndResolution("MakeSameHeightAndResolution-FixEdgeWithZeroHeight3", m_TileSystem, tileX, tileY, tr, 0, tr, tr, nx, ny, 0, 0, tr);
                                                }
                                                else
                                                {
                                                    if (nr != tr)
                                                    {
                                                        SetResolutionAndHeights("FixEdgeWithZeroHeight3", nx, ny, 0, 0, 0, nr, nr, null, 0, false, true);
                                                    }
                                                }
                                            }
                                            else if (ox == -1 && oy == 1)
                                            {
                                                //left top neighbour
                                                if (tileCoord.ModifiedEdges.HasFlag(TerrainTileEdgeCorner.LeftTopCorner))
                                                {
                                                    MakeSameHeightAndResolution("MakeSameHeightAndResolution-FixEdgeWithZeroHeight4", m_TileSystem, tileX, tileY, 0, tr, nx, ny, tr, 0, tr);
                                                }
                                                else
                                                {
                                                    if (nr != tr)
                                                    {
                                                        SetResolutionAndHeights("FixEdgeWithZeroHeight4", nx, ny, nr, 0, nr, 0, nr, null, 0, false, true);
                                                    }
                                                }
                                            }
                                            else if (ox == -1 && oy == -1)
                                            {
                                                //left bottom neighbour
                                                if (tileCoord.ModifiedEdges.HasFlag(TerrainTileEdgeCorner.LeftBottomCorner))
                                                {
                                                    MakeSameHeightAndResolution("MakeSameHeightAndResolution-FixEdgeWithZeroHeight5", m_TileSystem, tileX, tileY, 0, 0, nx, ny, tr, tr, tr);
                                                }
                                                else
                                                {
                                                    if (nr != tr)
                                                    {
                                                        SetResolutionAndHeights("FixEdgeWithZeroHeight5", nx, ny, nr, nr, nr, nr, nr, null, 0, false, true);
                                                    }
                                                }
                                            }
                                            else if (ox == -1 && oy == 0)
                                            {
                                                //left neighbour
                                                if (tileCoord.ModifiedEdges.HasFlag(TerrainTileEdgeCorner.LeftEdge))
                                                {
                                                    MakeSameHeightAndResolution("MakeSameHeightAndResolution-FixEdgeWithZeroHeight6", m_TileSystem, tileX, tileY, 0, 0, 0, tr, nx, ny, tr, 0, tr);
                                                }
                                                else
                                                {
                                                    if (nr != tr)
                                                    {
                                                        SetResolutionAndHeights("FixEdgeWithZeroHeight6", nx, ny, nr, 0, nr, nr, nr, null, 0, false, true);
                                                    }
                                                }
                                            }
                                            else if (ox == 0 && oy == 1)
                                            {
                                                //top neighbour
                                                if (tileCoord.ModifiedEdges.HasFlag(TerrainTileEdgeCorner.TopEdge))
                                                {
                                                    MakeSameHeightAndResolution("MakeSameHeightAndResolution-FixEdgeWithZeroHeight7", m_TileSystem, tileX, tileY, 0, tr, tr, tr, nx, ny, 0, 0, tr);
                                                }
                                                else
                                                {
                                                    if (nr != tr)
                                                    {
                                                        SetResolutionAndHeights("FixEdgeWithZeroHeight7", nx, ny, 0, 0, nr, 0, nr, null, 0, false, true);
                                                    }
                                                }
                                            }
                                            else if (ox == 0 && oy == -1)
                                            {
                                                //bottom neighbour
                                                if (tileCoord.ModifiedEdges.HasFlag(TerrainTileEdgeCorner.BottomEdge))
                                                {
                                                    MakeSameHeightAndResolution("MakeSameHeightAndResolution-FixEdgeWithZeroHeight8", m_TileSystem, tileX, tileY, 0, 0, tr, 0, nx, ny, 0, tr, tr);
                                                }
                                                else
                                                {
                                                    if (nr != tr)
                                                    {
                                                        SetResolutionAndHeights("FixEdgeWithZeroHeight8", nx, ny, 0, nr, nr, nr, nr, null, 0, false, true);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Debug.Assert(false, "unknown");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private void ResetEdgeHeight(CoordInfo tileCoord)
        {
            int tileX = tileCoord.tileCoord.x;
            int tileY = tileCoord.tileCoord.y;
            int minX = 0;
            int minY = 0;
            var resolution = m_TileSystem.GetTile(tileX, tileY).MeshResolution;
            int maxX = resolution;
            int maxY = resolution;

            var act = new UndoActionSetTileVertexHeights("Reset Edge Vertex Height", UndoSystem.Group, m_TileSystem.ID, tileX, tileY, minX, minY, maxX, minY, resolution, null, 0, false, updateMesh:true);
            UndoSystem.PerformCustomAction(act, true);
  
            var act1 = new UndoActionSetTileVertexHeights("Reset Edge Vertex Height", UndoSystem.Group, m_TileSystem.ID, tileX, tileY, minX, minY, minX, maxY, resolution, null, 0, false, updateMesh: true);
            UndoSystem.PerformCustomAction(act1, true);

            var act2 = new UndoActionSetTileVertexHeights("Reset Edge Vertex Height", UndoSystem.Group, m_TileSystem.ID, tileX, tileY, minX, maxY, maxX, maxY, resolution, null, 0, false, updateMesh: true);
            UndoSystem.PerformCustomAction(act2, true);

            var act3 = new UndoActionSetTileVertexHeights("Reset Edge Vertex Height", UndoSystem.Group, m_TileSystem.ID, tileX, tileY, maxX, minY, maxX, maxY, resolution, null, 0, false, updateMesh: true);
            UndoSystem.PerformCustomAction(act3, true);
        }

        private void DoSmooth(Vector3 worldPos, bool isMouseDown, TileSystem m_TileSystem)
        {
            if (m_BrushStyleManager.SelectedStyle == null)
            {
                return;
            }

            //smooth只发生在和worldPos所在tile的resolution相同的tile上
            m_PaintHeightParameters.TileCoords.Clear();

            var cursorCoord = m_TileSystem.RotatedPositionToCoordinate(worldPos.x, worldPos.z);
            var cursorTile = m_TileSystem.GetTile(cursorCoord.x, cursorCoord.y);
            if (cursorTile == null)
            {
                return;
            }

            int resolution = cursorTile.MeshResolution;
            if (cursorTile.VertexHeights == null)
            {
                return;
            }

            var minPosX = worldPos.x - m_PaintHeightParameters.Range * 0.5f;
            var minPosZ = worldPos.z - m_PaintHeightParameters.Range * 0.5f;
            var maxPosX = worldPos.x + m_PaintHeightParameters.Range * 0.5f;
            var maxPosZ = worldPos.z + m_PaintHeightParameters.Range * 0.5f;
            var minCoord = m_TileSystem.RotatedPositionToCoordinate(minPosX, minPosZ);
            var maxCoord = m_TileSystem.RotatedPositionToCoordinate(maxPosX, maxPosZ);

            Rect brushRect = new Rect(minPosX, minPosZ, maxPosX - minPosX, maxPosZ - minPosZ);
            var brush = m_BrushStyleManager.SelectedStyle;
            int mipmap = brush.CalculateMipMapLevel(false, (int)m_PaintHeightParameters.Range);
            float gridSize = m_TileSystem.TileWidth / resolution;
            float brushRectMinX = brushRect.x;
            float brushRectMinZ = brushRect.y;

            Func<bool, float, float, int, float> sampleFunc = null;
            if (m_PaintHeightParameters.SmoothBrush)
            {
                sampleFunc = brush.TextureLODLinearAlpha;
            }
            else
            {
                sampleFunc = brush.TextureLODPointAlpha;
            }

            //计算所选区域的平均高度
            float averageHeight = 0;
            int averageHeightCount = 0;
            for (int y = minCoord.y; y <= maxCoord.y; ++y)
            {
                for (int x = minCoord.x; x <= maxCoord.x; ++x)
                {
                    if (x >= 0 && x < m_TileSystem.XTileCount && y >= 0 && y < m_TileSystem.YTileCount)
                    {
                        var tile = m_TileSystem.GetTile(x, y);
                        if (tile != null && tile.MeshResolution == cursorTile.MeshResolution)
                        {
                            Rect tileRect = GetTileWorldRect(x, y);
                            Rect intersection;
                            bool intersected = Helper.GetRectIntersection(tileRect, brushRect, out intersection);
                            Debug.Assert(intersected);

                            var tileStartPos = m_TileSystem.CoordinateToUnrotatedPosition(x, y);
                            var intersectMinCoord = FromWorldPositionToCoordinate(intersection.xMin - tileStartPos.x, intersection.yMin - tileStartPos.z, resolution);
                            var intersectMaxCoord = FromWorldPositionToCoordinate(intersection.xMax - tileStartPos.x, intersection.yMax - tileStartPos.z, resolution);
                            CoordInfo coord = new CoordInfo();
                            coord.tileCoord = new Vector2Int(x, y);
                            var rangeInTile = new RectInt(intersectMinCoord, intersectMaxCoord - intersectMinCoord);
                            //计算tile于笔刷的顶点intersection
                            coord.rangeInTile = rangeInTile;
                            m_PaintHeightParameters.TileCoords.Add(coord);
                            coord.brushHeights.Clear();
                            int width = rangeInTile.width;
                            int height = rangeInTile.height;

                            var oldHeights = tile.VertexHeights;

                            int minX = rangeInTile.x;
                            int minY = rangeInTile.y;
                            for (int i = 0; i <= height; ++i)
                            {
                                for (int j = 0; j <= width; ++j)
                                {
                                    int idx = (i + minY) * (resolution + 1) + j + minX;
                                    averageHeight += oldHeights[idx];
                                    ++averageHeightCount;
                                }
                            }
                        }
                    }
                }
            }

            if (averageHeightCount > 0)
            {
                averageHeight /= averageHeightCount;

                for (int k = 0; k < m_PaintHeightParameters.TileCoords.Count; ++k)
                {
                    var rangeInTile = m_PaintHeightParameters.TileCoords[k].rangeInTile;
                    int width = rangeInTile.width;
                    int height = rangeInTile.height;
                    int minX = rangeInTile.x;
                    int minY = rangeInTile.y;
                    var brushHeights = m_PaintHeightParameters.TileCoords[k].brushHeights;
                    int x = m_PaintHeightParameters.TileCoords[k].tileCoord.x;
                    int y = m_PaintHeightParameters.TileCoords[k].tileCoord.y;
                    var tile = m_TileSystem.GetTile(x, y);
                    var oldHeights = tile.VertexHeights;
                    var tileStartPos = m_TileSystem.CoordinateToUnrotatedPosition(x, y);
                    for (int i = 0; i <= height; ++i)
                    {
                        for (int j = 0; j <= width; ++j)
                        {
                            float posX = tileStartPos.x + (j + rangeInTile.x) * gridSize;
                            float posZ = tileStartPos.z + (i + rangeInTile.y) * gridSize;
                            float rx = (posX - brushRectMinX) / m_PaintHeightParameters.Range;
                            float ry = (posZ - brushRectMinZ) / m_PaintHeightParameters.Range;
                            int idx = (i + minY) * (resolution + 1) + j + minX;
                            float sampleValue = sampleFunc(false, rx, ry, mipmap);

                            float s = sampleValue * m_PaintHeightParameters.Intensity * 0.1f;
                            float oldHeight = oldHeights[idx];
                            float delta = averageHeight - oldHeight;
                            float newHeight = oldHeight + delta * s;

                            brushHeights.Add(newHeight);
                        }
                    }
                }

                if (m_PaintHeightParameters.TileCoords.Count > 0)
                {
                    for (int i = 0; i < m_PaintHeightParameters.TileCoords.Count; ++i)
                    {
                        SetHeight(m_PaintHeightParameters.TileCoords[i], resolution, m_TileSystem);
                    }
                }
            }
        }

        private void SwitchToLOD(int lod)
        {
            lod = Mathf.Clamp(lod, 0, m_TileSystem.LODCount - 1);
            m_TileSystem.SetLODMesh(lod);
        }

        private void SetClipMask(Vector3 worldPos, bool clearTile)
        {
#if ENABLE_CLIP_MASK
            var coord = m_TileSystem.RotatedPositionToCoordinate(worldPos.x, worldPos.z);
            var tile = m_TileSystem.GetTile(coord.x, coord.y);
            if (tile != null)
            {
                if (tile.clipMask == null)
                {
                    tile.InitClipMask($"Tile {coord.x}_{coord.y}", m_TileSystem.tileWidth, m_TileSystem.tileHeight, (m_TileSystem.layerData as VaryingTileSizeTerrainLayerData).color32ArrayPool, null);
                }
                tile.clipMask.SetClipped(worldPos, m_PaintHeightParameters.parameter.ClipMaskSize, !clearTile);
            }
#endif
        }

        private void SetResolutionAndHeights(string title, int tileX, int tileY, int minX, int minY, int maxX, int maxY, int resolution, List<float> vertexHeights, float allVertexHeight, bool keepEdgeVertexHeight, bool updateMesh)
        {
            var tile = m_TileSystem.GetTile(tileX, tileY);
            if (tile.MeshResolution != resolution)
            {
                var actionSetResolution = new UndoActionSetTileMeshResolution($"Set Tile Mesh Resolution ({tileX},{tileY})", UndoSystem.Group, m_TileSystem.ID, tileX, tileY, resolution, true);
                UndoSystem.PerformCustomAction(actionSetResolution, true);
            }

            var actionSetVertexHeights = new UndoActionSetTileVertexHeights(title, UndoSystem.Group, m_TileSystem.ID, tileX, tileY, minX, minY, maxX, maxY, resolution, vertexHeights, allVertexHeight, keepEdgeVertexHeight, updateMesh);
            UndoSystem.PerformCustomAction(actionSetVertexHeights, true);
        }

        private int m_ActiveLOD = 0;
        private List<TileModificationInfo> m_TilesNeedFixEdge = new();
        private List<float> m_TempList = new();

        private class TileModificationInfo
        {
            public Vector2Int Coordinate;
            public TerrainTileEdgeCorner ModifiedEdges = 0;
        }
    }
}
