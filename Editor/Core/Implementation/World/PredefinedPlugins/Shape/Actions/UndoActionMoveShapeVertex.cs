﻿/*
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
    public class UndoActionMoveShapeVertex : CustomUndoAction
    {
        public override bool CanJoin => true;
        public override int Size => 0;

        public UndoActionMoveShapeVertex(
            string displayName,
            UndoActionGroup group,
            int shapeSystemID,
            int shapeID,
            int vertexIndex,
            Vector3 offset)
            : base(displayName, group)
        {
            m_ShapeID = shapeID;
            m_ShapeSystemID = shapeSystemID;
            m_VertexIndex = vertexIndex;
            m_Offset = offset;
        }

        public override bool Redo()
        {
            return Move(m_Offset);
        }

        public override bool Undo()
        {
            return Move(-m_Offset);
        }

        protected override bool JoinInternal(CustomUndoAction action)
        {
            return false;
        }

        private ShapeSystem GetShapeSystem()
        {
            return WorldEditor.WorldManager.FirstWorld.QueryObject<ShapeSystem>(m_ShapeSystemID);
        }

        private bool Move(Vector3 offset)
        {
            var shapeSystem = GetShapeSystem();
            if (shapeSystem != null)
            {
                var shape = shapeSystem.QueryObjectUndo(m_ShapeID) as ShapeObject;
                if (shape != null)
                {
                    shape.MoveVertex(m_VertexIndex, offset);
                    shapeSystem.UpdateRenderer(m_ShapeID);
                    return true;
                }
            }
            return false;
        }

        private readonly int m_ShapeID;
        private readonly int m_ShapeSystemID;
        private readonly int m_VertexIndex;
        private readonly Vector3 m_Offset;
    }
}

