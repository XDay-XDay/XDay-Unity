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
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Shape.Editor
{
    public class UndoActionInsertShapeVertex : CustomUndoAction
    {
        public override bool CanJoin => true;
        public override int Size => 0;

        public UndoActionInsertShapeVertex(
            string displayName,
            UndoActionGroup group,
            int shapeSystemID,
            int shapeID,
            int vertexIndex,
            Vector3 localPosition)
            : base(displayName, group)
        {
            m_ShapeID = shapeID;
            m_ShapeSystemID = shapeSystemID;
            m_VertexIndex = vertexIndex;
            m_LocalPosition = localPosition;
        }

        public override bool Redo()
        {
            var shapeSystem = GetShapeSystem();
            if (shapeSystem != null)
            {
                var shape = shapeSystem.QueryObjectUndo(m_ShapeID) as ShapeObject;
                if (shape != null)
                {
                    shape.InsertVertex(m_VertexIndex, m_LocalPosition);
                    shapeSystem.UpdateRenderer(m_ShapeID);
                    return true;
                }
            }
            return false;   
        }

        public override bool Undo()
        {
            var shapeSystem = GetShapeSystem();
            if (shapeSystem != null)
            {
                var shape = shapeSystem.QueryObjectUndo(m_ShapeID) as ShapeObject;
                if (shape != null)
                {
                    shape.DeleteVertex(m_VertexIndex);
                    shapeSystem.UpdateRenderer(m_ShapeID);
                    return true;
                }
            }
            return false;
        }

        protected override bool JoinInternal(CustomUndoAction action)
        {
            return false;
        }

        private ShapeSystem GetShapeSystem()
        {
            return WorldEditor.WorldManager.FirstWorld.QueryObject<ShapeSystem>(m_ShapeSystemID);
        }

        private readonly int m_ShapeID;
        private readonly int m_ShapeSystemID;
        private readonly int m_VertexIndex;
        private readonly Vector3 m_LocalPosition;
    }
}