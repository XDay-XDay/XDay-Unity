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

namespace XDay.UtilityAPI.Editor
{
    public abstract class GraphNodeEditor
    {
        public GraphNodeEditor(float worldWidth, float worldHeight)
        {
            m_Viewer = new VirtualViewer(worldWidth, worldHeight, OnZoomChanged);
        }

        public void Reset()
        {
            m_Viewer.Reset();
            m_OccupiedCoordinates.Clear();
            OnReset();
        }

        private void CreateGridLines()
        {
            float worldWidth = m_Viewer.GetWorldWidth();
            float worldHeight = m_Viewer.GetWorldHeight();
            float gridSize = m_GridSize;
            int horizontalGridCount = Mathf.CeilToInt(worldWidth / gridSize);
            int verticalGridCount = Mathf.CeilToInt(worldHeight / gridSize);
            m_GridLines ??= new Vector3[horizontalGridCount * 2 + verticalGridCount * 2];
            int k = 0;
            for (int i = 0; i < verticalGridCount; ++i)
            {
                Vector2 start = World2Window(new Vector2(-worldWidth * 0.5f, i * gridSize - worldHeight * 0.5f));
                Vector2 end = World2Window(new Vector2(worldWidth * 0.5f, i * gridSize - worldHeight * 0.5f));
                m_GridLines[k] = start;
                m_GridLines[k + 1] = end;
                k += 2;
            }
            for (int i = 0; i < horizontalGridCount; ++i)
            {
                Vector2 start = World2Window(new Vector2(i * gridSize - worldWidth * 0.5f, -worldHeight * 0.5f));
                Vector2 end = World2Window(new Vector2(i * gridSize - worldWidth * 0.5f, worldHeight * 0.5f));
                m_GridLines[k] = start;
                m_GridLines[k + 1] = end;
                k += 2;
            }
        }

        public void Render(float windowContentWidth, float windowContentHeight, System.Action repaint)
        {
            m_Repaint = repaint;

            bool windowSizeChanged = !Mathf.Approximately(m_WindowContentWidth, windowContentWidth) || !Mathf.Approximately(m_WindowContentHeight, windowContentHeight);
            m_WindowContentWidth = windowContentWidth;
            m_WindowContentHeight = windowContentHeight;

            if (windowSizeChanged)
            {
                OnWindowChanged();
                OnWindowSizeChange((int)windowContentWidth, (int)windowContentHeight);
            }

            DrawGUI();
            UpdateInput();
        }

        public void OnWindowSizeChange(int viewportWidth, int viewportHeight)
        {
            float ratio = viewportWidth / (float)viewportHeight;
            m_Viewer.SetFrustum(viewportHeight, ratio);
        }

        protected Vector2 AlignToGrid(Vector2 pos)
        {
            if (m_AlignToGrid)
            {
                int gx = Mathf.FloorToInt(pos.x / m_GridSize);
                int gy = Mathf.FloorToInt(pos.y / m_GridSize);
                return new Vector2(gx * m_GridSize, gy * m_GridSize);
            }
            return pos;
        }

        public Vector2 World2Window(Vector2 pos)
        {
            m_Viewer.WorldToWindow(m_WindowContentWidth, m_WindowContentHeight, pos, out float x, out float y);
            return new Vector2(x, y);
        }

        public Vector2 Window2World(Vector2 pos)
        {
            m_Viewer.WindowToWorld(m_WindowContentWidth, m_WindowContentHeight, pos, out float x, out float y);
            return new Vector2(x, y);
        }

        public Vector2 World2Window(float px, float py)
        {
            m_Viewer.WorldToWindow(m_WindowContentWidth, m_WindowContentHeight, new Vector2(px, py), out float x, out float y);
            return new Vector2(x, y);
        }

        public Vector2 Window2World(float px, float py)
        {
            m_Viewer.WindowToWorld(m_WindowContentWidth, m_WindowContentHeight, new Vector2(px, py), out float x, out float y);
            return new Vector2(x, y);
        }

        protected float WorldLengthToWindowLength(float length)
        {
            return length / m_Viewer.GetZoom();
        }

        protected Vector2 WorldLengthToWindowLength(Vector2 length)
        {
            return length / m_Viewer.GetZoom();
        }

        protected float WindowLengthToWorldLength(float length)
        {
            return length * m_Viewer.GetZoom();
        }

        protected Vector2 WindowLengthToWorldLength(Vector2 length)
        {
            return length * m_Viewer.GetZoom();
        }

        public Vector2Int WorldPositionToGridCoordinateFloor(float x, float y)
        {
            float worldWidth = m_Viewer.GetWorldWidth();
            float worldHeight = m_Viewer.GetWorldHeight();
            int gridX = Mathf.FloorToInt((x + worldWidth * 0.5f) / m_GridSize);
            int gridY = Mathf.FloorToInt((y + worldHeight * 0.5f) / m_GridSize);
            return new Vector2Int(gridX, gridY);
        }

        public Vector2Int WorldPositionToGridCoordinateCeil(float x, float y)
        {
            float worldWidth = m_Viewer.GetWorldWidth();
            float worldHeight = m_Viewer.GetWorldHeight();
            int gridX = Mathf.CeilToInt((x + worldWidth * 0.5f) / m_GridSize);
            int gridY = Mathf.CeilToInt((y + worldHeight * 0.5f) / m_GridSize);
            return new Vector2Int(gridX, gridY);
        }

        public Vector2Int WorldPositionToGridCoordinateFloor(Vector2 pos)
        {
            return WorldPositionToGridCoordinateFloor(pos.x, pos.y);
        }

        public Vector2Int WorldPositionToGridCoordinateCeil(Vector2 pos)
        {
            return WorldPositionToGridCoordinateCeil(pos.x, pos.y);
        }

        public Vector2 GridCoordinateToWorldPosition(int x, int y)
        {
            float worldWidth = m_Viewer.GetWorldWidth();
            float worldHeight = m_Viewer.GetWorldHeight();
            float posX = x * m_GridSize - worldWidth * 0.5f;
            float posY = y * m_GridSize - worldHeight * 0.5f;
            return new Vector2(posX, posY);
        }

        public Vector2 GridCoordinateToWorldPosition(Vector2Int coord)
        {
            return GridCoordinateToWorldPosition(coord.x, coord.y);
        }

        protected void DrawGrid()
        {
            if (m_DrawGrid)
            {
                CreateGridLines();
                float val = 120;
                Color color = new(val / 255.0f, val / 255.0f, val / 255.0f, 0.2f);
                Handles.BeginGUI();
                Handles.color = color;
                Handles.DrawLines(m_GridLines);
                Handles.EndGUI();
            }
        }

        void DrawGUI()
        {
            OnDrawGUI();
        }

        void UpdateInput()
        {
            if (m_MenuShow)
            {
                return;
            }

            var e = Event.current;

            var mousePos = e.mousePosition;
            //move view
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 2)
            {
                if (e.type == EventType.MouseDown)
                {
                    m_Mover.Reset();
                }

                m_Mover.Update(mousePos);
                var movement = m_Mover.GetMovement();
                m_Viewer.Move((float)-movement.x, (float)movement.y);
            }
            if (e.type == EventType.MouseUp && e.button == 2)
            {
                m_Mover.Reset();
                OnMouseButtonReleased(2, mousePos);
            }
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                m_Mover.Reset();
                OnMouseButtonReleased(0, mousePos);
            }
            float scrollDelta = 0;
            if (e.isScrollWheel)
            {
                scrollDelta = -e.delta.y;
            }
            float zoomDelta = 0.1f * GetZoomSpeed();
            if (scrollDelta > 0)
            {
                m_Viewer.Zoom(-zoomDelta, m_WindowContentWidth, m_WindowContentHeight, new Vector2(mousePos.x, mousePos.y));
            }
            else if (scrollDelta < 0)
            {
                m_Viewer.Zoom(zoomDelta, m_WindowContentWidth, m_WindowContentHeight, new Vector2(mousePos.x, mousePos.y));
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                OnMouseButtonPressed(0, mousePos);
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                OnMouseButtonPressed(1, mousePos);
            }
            else if (e.type == EventType.MouseDown && e.button == 2)
            {
                OnMouseButtonPressed(2, mousePos);
            }
            else if (e.type == EventType.MouseDrag && e.button == 0)
            {
                m_Mover.Update(mousePos);
                var movement = m_Mover.GetMovement();
                OnMouseDrag(0, new Vector2(movement.x * m_Viewer.GetZoom(), movement.y * m_Viewer.GetZoom()));
            }
        }

        public void OccupyGrids(Vector2Int min, Vector2Int max)
        {
            for (int i = min.y; i <= max.y; ++i)
            {
                for (int j = min.x; j <= max.x; ++j)
                {
                    var coord = new Vector2Int(j, i);
                    bool found = m_OccupiedCoordinates.TryGetValue(coord, out int refCount);
                    if (!found)
                    {
                        m_OccupiedCoordinates.Add(coord, 1);
                    }
                    else
                    {
                        m_OccupiedCoordinates[coord] = refCount + 1;
                    }
                }
            }
        }

        public void ReleaseGrids(Vector2Int min, Vector2Int max)
        {
            for (int i = min.y; i <= max.y; ++i)
            {
                for (int j = min.x; j <= max.x; ++j)
                {
                    var coord = new Vector2Int(j, i);
                    bool found = m_OccupiedCoordinates.TryGetValue(coord, out int refCount);
                    if (found)
                    {
                        refCount -= 1;
                        if (refCount == 0)
                        {
                            m_OccupiedCoordinates.Remove(coord);
                        }
                        else
                        {
                            m_OccupiedCoordinates[coord] = refCount;
                        }
                    }
                }
            }
        }

        public void DrawRect(Vector2 min, Vector2 max, Color color)
        {
            EditorGUI.DrawRect(new Rect(min, max - min), color);
        }

        protected void DrawTexture(Vector2 min, Vector2 max, Texture texture, ScaleMode scaleMode = ScaleMode.ScaleToFit)
        {
            if (texture == null)
            {
                return;
            }
            var min1 = Vector2.Min(min, max);
            var max1 = Vector2.Max(min, max);
            GUI.DrawTexture(new Rect(min1, max1 - min1), texture, scaleMode);
        }

        protected void DrawLineRect(Vector2 min, Vector2 max, Color color)
        {
            Color oldColor = Handles.color;
            Handles.color = color;
            Handles.DrawDottedLine(new Vector3(min.x, min.y, 0), new Vector3(min.x, max.y, 0), 2);
            Handles.DrawDottedLine(new Vector3(min.x, max.y, 0), new Vector3(max.x, max.y, 0), 2);
            Handles.DrawDottedLine(new Vector3(max.x, max.y, 0), new Vector3(max.x, min.y, 0), 2);
            Handles.DrawDottedLine(new Vector3(max.x, min.y, 0), new Vector3(min.x, min.y, 0), 2);
            Handles.color = oldColor;
        }

        protected abstract void OnDrawGUI();
        protected abstract void OnMouseButtonPressed(int button, Vector2 mousePos);
        protected abstract void OnMouseButtonReleased(int button, Vector2 mousePos);
        protected abstract void OnMouseDrag(int button, Vector2 movement);
        protected virtual void OnWindowChanged() { }
        protected virtual float GetZoomSpeed() { return 1.0f; }
        protected virtual void OnReset() { }
        protected virtual void OnZoomChanged() { }
        protected void Repaint()
        {
            m_Repaint?.Invoke();
        }

        protected float m_WindowContentWidth;
        protected float m_WindowContentHeight;
        //in world space
        protected float m_GridSize = 20;
        protected VirtualViewer m_Viewer;
        protected bool m_MenuShow = false;
        protected bool m_AlignToGrid = false;
        protected bool m_DrawGrid = true;
        protected IMover m_Mover = IMover.Create();
        protected Vector2 m_CursorPos;
        private Vector3[] m_GridLines;
        protected Dictionary<Vector2Int, int> m_OccupiedCoordinates = new();
        private Action m_Repaint;
    };

}

