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
    public class UndoActionSetTileMeshResolution : CustomUndoAction
    {
        public override bool CanJoin => false;
        public override int Size => m_OldHeights.Count * 4;

        public UndoActionSetTileMeshResolution(
            string displayName, 
            UndoActionGroup group, 
            int tileSystemID, 
            int tileX, 
            int tileY, 
            int newResolution, 
            bool updateMesh)
            : base(displayName, group)
        {
            m_TileSystemID = tileSystemID;
            var tileSystem = GetTileSystem();
            var tile = tileSystem.GetTile(tileX, tileY);
            m_OldHeights = new List<float>();
            if (tile.VertexHeights != null)
            {
                m_OldHeights.AddRange(tile.VertexHeights);
            }
            m_TileX = tileX;
            m_TileY = tileY;
            m_OldResolution = tile.MeshResolution;
            m_NewResolution = newResolution;
            m_UpdateMesh = updateMesh;
        }

        public override bool Redo()
        {
            var tileSystem = GetTileSystem();
            if (tileSystem != null)
            {
                tileSystem.SetTileResolution(m_TileX, m_TileY, m_NewResolution, m_UpdateMesh);
                return true;
            }
            return false;
        }

        public override bool Undo()
        {
            TileSystem tileSystem = GetTileSystem();
            if (tileSystem != null)
            {
                tileSystem.SetTileResolution(m_TileX, m_TileY, m_OldResolution, m_UpdateMesh);
                tileSystem.SetHeights(m_TileX, m_TileY, 0, 0, m_OldResolution, m_OldResolution, m_OldResolution, m_OldHeights, true, false);
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

        private List<float> m_OldHeights;
        private int m_OldResolution;
        private int m_NewResolution;
        private int m_TileX;
        private int m_TileY;
        private int m_TileSystemID;
        private bool m_UpdateMesh;
    }
}
