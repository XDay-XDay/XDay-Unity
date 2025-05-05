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

namespace XDay.UtilityAPI.Editor
{
    public abstract class GraphNodeEditor
    {
        public GraphNodeEditor(float worldWidth, float worldHeight)
        {
            mViewer = new VirtualViewer(worldWidth, worldHeight);
        }

        public void Reset()
        {
            mViewer.Reset();
            mOccupiedCoordinates.Clear();
            OnReset();
        }

        private void CreateGridLines()
        {
            float worldWidth = mViewer.GetWorldWidth();
            float worldHeight = mViewer.GetWorldHeight();
            float gridSize = mGridSize;
            int horizontalGridCount = Mathf.CeilToInt(worldWidth / gridSize);
            int verticalGridCount = Mathf.CeilToInt(worldHeight / gridSize);
            if (mGridLines == null)
            {
                mGridLines = new Vector3[horizontalGridCount * 2 + verticalGridCount * 2];
            }
            int k = 0;
            for (int i = 0; i < verticalGridCount; ++i)
            {
                Vector2 start = World2Window(new Vector2(-worldWidth * 0.5f, i * gridSize - worldHeight * 0.5f));
                Vector2 end = World2Window(new Vector2(worldWidth * 0.5f, i * gridSize - worldHeight * 0.5f));
                mGridLines[k] = start;
                mGridLines[k + 1] = end;
                k += 2;
            }
            for (int i = 0; i < horizontalGridCount; ++i)
            {
                Vector2 start = World2Window(new Vector2(i * gridSize - worldWidth * 0.5f, -worldHeight * 0.5f));
                Vector2 end = World2Window(new Vector2(i * gridSize - worldWidth * 0.5f, worldHeight * 0.5f));
                mGridLines[k] = start;
                mGridLines[k + 1] = end;
                k += 2;
            }
        }

        public void Render(float windowContentWidth, float windowContentHeight)
        {
            bool windowSizeChanged = !Mathf.Approximately(mWindowContentWidth, windowContentWidth) || !Mathf.Approximately(mWindowContentHeight, windowContentHeight);
            mWindowContentWidth = windowContentWidth;
            mWindowContentHeight = windowContentHeight;

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
            mViewer.SetFrustum((float)viewportHeight, ratio);
        }

        protected Vector2 AlignToGrid(Vector2 pos)
        {
            if (mAlignToGrid)
            {
                float gridSize = mGridSize * mViewer.GetZoom();
                int gx = Mathf.FloorToInt(pos.x / gridSize);
                int gy = Mathf.FloorToInt(pos.y / gridSize);
                return new Vector2(gx * gridSize, gy * gridSize);
            }
            return pos;
        }

        protected Vector2 World2Window(Vector2 pos)
        {
            mViewer.WorldToWindow(mWindowContentWidth, mWindowContentHeight, pos, out float x, out float y);
            return new Vector2(x, y);
        }

        protected Vector2 Window2World(Vector2 pos)
        {
            mViewer.WindowToWorld(mWindowContentWidth, mWindowContentHeight, pos, out float x, out float y);
            return new Vector2(x, y);
        }

        protected Vector2 World2Window(float px, float py)
        {
            mViewer.WorldToWindow(mWindowContentWidth, mWindowContentHeight, new Vector2(px, py), out float x, out float y);
            return new Vector2(x, y);
        }

        protected Vector2 Window2World(float px, float py)
        {
            mViewer.WindowToWorld(mWindowContentWidth, mWindowContentHeight, new Vector2(px, py), out float x, out float y);
            return new Vector2(x, y);
        }

        protected float WorldLengthToWindowLength(float length)
        {
            return length * mViewer.GetZoom();
        }

        protected float WindowLengthToWorldLength(float length)
        {
            return length / mViewer.GetZoom();
        }

        protected Vector2 WorldLengthToWindowLength(Vector2 length)
        {
            return length * mViewer.GetZoom();
        }

        protected Vector2 WindowLengthToWorldLength(Vector2 length)
        {
            return length / mViewer.GetZoom();
        }

        public Vector2Int WorldPositionToGridCoordinateFloor(float x, float y)
        {
            float worldWidth = mViewer.GetWorldWidth();
            float worldHeight = mViewer.GetWorldHeight();
            int gridX = Mathf.FloorToInt((x + worldWidth * 0.5f) / mGridSize);
            int gridY = Mathf.FloorToInt((y + worldHeight * 0.5f) / mGridSize);
            return new Vector2Int(gridX, gridY);
        }

        public Vector2Int WorldPositionToGridCoordinateCeil(float x, float y)
        {
            float worldWidth = mViewer.GetWorldWidth();
            float worldHeight = mViewer.GetWorldHeight();
            int gridX = Mathf.CeilToInt((x + worldWidth * 0.5f) / mGridSize);
            int gridY = Mathf.CeilToInt((y + worldHeight * 0.5f) / mGridSize);
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
            float worldWidth = mViewer.GetWorldWidth();
            float worldHeight = mViewer.GetWorldHeight();
            float posX = x * mGridSize - worldWidth * 0.5f;
            float posY = y * mGridSize - worldHeight * 0.5f;
            return new Vector2(posX, posY);
        }

        public Vector2 GridCoordinateToWorldPosition(Vector2Int coord)
        {
            return GridCoordinateToWorldPosition(coord.x, coord.y);
        }

        void DrawGrid()
        {
            if (mDrawGrid)
            {
                CreateGridLines();
                float val = 120;
                Color color = new Color(val / 255.0f, val / 255.0f, val / 255.0f, 0.2f);
                Handles.BeginGUI();
                Handles.color = color;
                Handles.DrawLines(mGridLines);
                Handles.EndGUI();
            }
        }

        void DrawGUI()
        {
            DrawGrid();

            OnDrawGUI();
        }

        void UpdateInput()
        {
            if (mMenuShow)
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
                    mMover.Reset();
                }

                mMover.Update(mousePos);
                var movement = mMover.GetMovement();
                mViewer.Move((float)-movement.x, (float)movement.y);
            }
            if (e.type == EventType.MouseUp && e.button == 2)
            {
                mMover.Reset();
                OnMouseButtonReleased(2);
            }
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                mMover.Reset();
                OnMouseButtonReleased(0);
            }
            float scrollDelta = 0;
            if (e.isScrollWheel)
            {
                scrollDelta = -e.delta.y;
            }
            float zoomDelta = 0.1f * GetZoomSpeed();
            if (scrollDelta > 0)
            {
                mViewer.Zoom(-zoomDelta, mWindowContentWidth, mWindowContentHeight, new Vector2(mousePos.x, mousePos.y));
            }
            else if (scrollDelta < 0)
            {
                mViewer.Zoom(zoomDelta, mWindowContentWidth, mWindowContentHeight, new Vector2(mousePos.x, mousePos.y));
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
                mMover.Update(mousePos);
                var movement = mMover.GetMovement();
                OnMouseDrag(0, new Vector2(movement.x * mViewer.GetZoom(), movement.y * mViewer.GetZoom()));
            }
        }

        public void OccupyGrids(Vector2Int min, Vector2Int max)
        {
            for (int i = min.y; i <= max.y; ++i)
            {
                for (int j = min.x; j <= max.x; ++j)
                {
                    var coord = new Vector2Int(j, i);
                    bool found = mOccupiedCoordinates.TryGetValue(coord, out int refCount);
                    if (!found)
                    {
                        mOccupiedCoordinates.Add(coord, 1);
                    }
                    else
                    {
                        mOccupiedCoordinates[coord] = refCount + 1;
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
                    bool found = mOccupiedCoordinates.TryGetValue(coord, out int refCount);
                    if (found)
                    {
                        refCount -= 1;
                        if (refCount == 0)
                        {
                            mOccupiedCoordinates.Remove(coord);
                        }
                        else
                        {
                            mOccupiedCoordinates[coord] = refCount;
                        }
                    }
                }
            }
        }

        protected void DrawRect(Vector2 min, Vector2 max, Color color)
        {
            EditorGUI.DrawRect(new Rect(min, max - min), color);
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
        protected abstract void OnMouseButtonReleased(int button);
        protected abstract void OnMouseDrag(int button, Vector2 movement);
        protected virtual void OnWindowChanged() { }
        protected virtual float GetZoomSpeed() { return 1.0f; }
        protected virtual void OnReset() { }

        protected float mWindowContentWidth;
        protected float mWindowContentHeight;

        protected float mGridSize = 20;
        protected VirtualViewer mViewer;

        protected bool mMenuShow = false;
        protected bool mAlignToGrid = false;
        protected bool mDrawGrid = true;

        protected IMover mMover = IMover.Create();

        protected Vector2 mCursorPos;

        Vector3[] mGridLines;

        protected Dictionary<Vector2Int, int> mOccupiedCoordinates = new Dictionary<Vector2Int, int>();
    };

}

