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
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    public class UndoActionSetTileVertexHeights : CustomUndoAction
    {
        public override bool CanJoin => true;
        public override int Size => (m_OldHeights.Count + m_NewHeights.Count) * 4;

        public UndoActionSetTileVertexHeights(
            string displayName, 
            UndoActionGroup group,
            int tileSystemID, 
            int tileX, 
            int tileY, 
            int minX, 
            int minY, 
            int maxX, 
            int maxY, 
            int resolution,
            List<float> newVertexHeights,
            float allVertexHeight,
            bool keepEdgeVertexHeight, 
            bool updateMesh)
            : base (displayName, group)
        {
            m_TileSystemID = tileSystemID;
            m_MinX = minX;
            m_MaxX = maxX;
            m_MinY = minY;
            m_MaxY = maxY;
            m_TileX = tileX;
            m_TileY = tileY;
            m_Resolution = resolution;
            m_UpdateMesh = updateMesh;
            m_OldHeights = new();

            var tileSystem = GetTileSystem();
            var tile = tileSystem.GetTile(m_TileX, m_TileY);
            for (var i = minY; i <= maxY; ++i)
            {
                for (var j = minX; j <= maxX; ++j)
                {
                    m_OldHeights.Add(tile.GetHeight(j, i));
                }
            }

            if (newVertexHeights != null)
            {
                m_NewHeights = new(newVertexHeights);
            }
            else
            {
                var n = (m_MaxX - m_MinX + 1) * (m_MaxY - m_MinY + 1);
                m_NewHeights = new List<float>(n);
                for (var i = 0; i < n; ++i)
                {
                    m_NewHeights.Add(allVertexHeight);
                }
            }
            m_KeepEdgeVertexHeight = keepEdgeVertexHeight;
        }

        public override bool Redo()
        {
            var ok = SetHeights(m_NewHeights);
            //只有第一次需要keep edge vertex height
            if (m_FirstRedo)
            {
                m_FirstRedo = false;
                m_KeepEdgeVertexHeight = false;
                GetNewHeights();
            }
            return ok;
        }

        public override bool Undo()
        {
            return SetHeights(m_OldHeights);
        }

        private bool SetHeights(List<float> heights)
        {
            var tileSystem = GetTileSystem();
            if (tileSystem != null)
            {
                tileSystem.SetHeights(m_TileX, m_TileY, m_MinX, m_MinY, m_MaxX, m_MaxY, m_Resolution, heights, m_UpdateMesh, m_KeepEdgeVertexHeight);
                return true;
            }
            return false;
        }

        protected override bool JoinInternal(CustomUndoAction action)
        {
            return false;
        }

        private TileSystem GetTileSystem()
        {
            return WorldEditor.WorldManager.FirstWorld.QueryObject<TileSystem>(m_TileSystemID);
        }

        private void GetNewHeights()
        {
            m_NewHeights.Clear();
            var tileSystem = GetTileSystem();
            var tile = tileSystem.GetTile(m_TileX, m_TileY);
            for (int i = m_MinY; i <= m_MaxY; ++i)
            {
                for (int j = m_MinX; j <= m_MaxX; ++j)
                {
                    m_NewHeights.Add(tile.GetHeight(j, i));
                }
            }
        }

        private List<float> m_OldHeights;
        private List<float> m_NewHeights;
        private int m_MinX;
        private int m_MinY;
        private int m_MaxX;
        private int m_MaxY;
        private int m_TileX;
        private int m_TileY;
        private int m_TileSystemID;
        private int m_Resolution;
        private bool m_KeepEdgeVertexHeight;
        private bool m_FirstRedo = true;
        private bool m_UpdateMesh;
    }
}

