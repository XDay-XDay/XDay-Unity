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
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystem
    {
        protected override void SceneGUISelectedInternal()
        {
            var e = Event.current;

            DrawBounds();

            foreach (var layer in m_Layers)
            {
                layer.Update();
            }

            if (m_Action == Operation.EditRegion)
            {
                CommandSetRegion(e, World);
                HandleUtility.AddDefaultControl(0);
            }
            else if (m_Action == Operation.Select)
            {
                CommandSelect(e, World);
            }

            var currentLayer = GetCurrentLayer();
            if (currentLayer != null)
            {
                var region = currentLayer.SelectedRegion;
                if (region != null)
                {
                    m_TileIndicator?.Draw(region.Color, centerAlignment: true);
                }
            }

            if (e.keyCode == KeyCode.Q)
            {
                SetBrushSize(m_BrushSize + 1);
                e.Use();
            }
            else if (e.keyCode == KeyCode.W)
            {
                SetBrushSize(m_BrushSize - 1);
                e.Use();
            }

            if (m_ShowName)
            {
                DrawRegionNames();
            }

            SceneView.RepaintAll();
        }

        protected override void SceneGUIInternal()
        {
            m_TileIndicator.Visible = false;
        }

        private void CommandSelect(Event e, IWorld world)
        {
            var layer = GetCurrentLayer();
            if (layer == null)
            {
                return;
            }
            var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);
            if ((e.type == EventType.MouseDown) && e.button == 2 && e.alt == false)
            {
                var coord = layer.PositionToCoordinate(worldPosition);
                Debug.Log($"当前坐标: {coord}");
            }
        }

        private void CommandSetRegion(Event e, IWorld world)
        {
            var layer = GetCurrentLayer();
            if (layer == null)
            {
                return;
            }

            var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);

            m_TileIndicator.Visible = true;
            UpdateTileCursor(worldPosition);

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.alt == false)
            {
                var region = layer.SelectedRegion;
                if (region != null)
                {
                    var coord = layer.PositionToCoordinate(worldPosition.x, worldPosition.z);
                    if (coord != m_LastPaintCoord)
                    {
                        m_LastPaintCoord = coord;
                        if (layer.IsValidCoordinate(coord.x, coord.y))
                        {
                            var minX = coord.x - m_BrushSize / 2;
                            var minY = coord.y - m_BrushSize / 2;
                            layer.SetRegion(minX, minY, m_BrushSize, m_BrushSize, e.control ? 0 : region.ID);
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "没有创建区域", "确定");
                }
            }

            if (e.type == EventType.MouseUp)
            {
                m_LastPaintCoord = new Vector2Int(-1000, -1000);
            }
        }

        private void UpdateTileCursor(Vector3 worldPosition)
        {
            var layer = GetCurrentLayer();
            if (layer != null)
            {
                var coord = layer.PositionToCoordinate(worldPosition.x, worldPosition.z);
                if (layer.IsValidCoordinate(coord.x, coord.y))
                {
                    var minX = coord.x - m_BrushSize / 2;
                    var minY = coord.y - m_BrushSize / 2;
                    var pos = (layer.CoordinateToPosition(minX, minY) + layer.CoordinateToPosition(minX + m_BrushSize, minY + m_BrushSize)) * 0.5f;
                    pos.y = 0.05f;
                    m_TileIndicator.Position = pos;
                    m_TileIndicator.Size = m_BrushSize * layer.GridWidth;
                }
            }
        }

        private void SetBrushSize(int brushSize)
        {
            m_BrushSize = brushSize;
            m_BrushSize = Mathf.Clamp(m_BrushSize, 1, 500);
        }

        private void DrawRegionNames()
        {
            if (m_TextStyle == null)
            {
                m_TextStyle = new GUIStyle(GUI.skin.label);
                m_TextStyle.normal.textColor = Color.black;
            }

            foreach (var layer in m_Layers)
            {
                foreach (var region in layer.Regions)
                {
                    Handles.Label(layer.Renderer.GetRegionBuildingPosition(region.ID), region.Name, m_TextStyle);
                }
            }
        }

        private IGizmoCubeIndicator m_TileIndicator;
        private Vector2Int m_LastPaintCoord = new(-1000, -1000);
        private GUIStyle m_TextStyle;
        private bool m_ShowName = true;
    }
}