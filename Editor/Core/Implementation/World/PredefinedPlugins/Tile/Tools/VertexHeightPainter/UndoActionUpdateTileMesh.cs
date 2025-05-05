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

using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    public class UndoActionUpdateTileMesh : CustomUndoAction
    {
        public override bool CanJoin => true;
        public override int Size => 0;

        public UndoActionUpdateTileMesh(
            string displayName,
            UndoActionGroup group,
            int tileSystemID,
            int tileX,
            int tileY,
            bool alwaysCreateMesh)
            : base(displayName, group)
        {
            m_TileSystemID = tileSystemID;
            m_TileX = tileX;
            m_TileY = tileY;
            m_AlwaysCreateMesh = alwaysCreateMesh;
        }

        public override bool Redo()
        {
            var tileSystem = GetTileSystem();
            if (tileSystem != null)
            {
                tileSystem.UpdateMesh(m_TileX, m_TileY, m_AlwaysCreateMesh);
                return true;
            }
            return false;
        }

        public override bool Undo()
        {
            return true;
        }

        protected override bool JoinInternal(CustomUndoAction action)
        {
            return false;
        }

        private TileSystem GetTileSystem()
        {
            return WorldEditor.WorldManager.FirstWorld.QueryObject<TileSystem>(m_TileSystemID);
        }

        private int m_TileX;
        private int m_TileY;
        private int m_TileSystemID;
        private bool m_AlwaysCreateMesh;
    }
}

