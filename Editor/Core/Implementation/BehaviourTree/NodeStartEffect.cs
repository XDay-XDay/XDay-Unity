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

namespace XDay.BehaviourTreeAPI.Editor
{
    internal class NodeStartEffect
    {
        public NodeStartEffect(NodeView nodeView, float duration, BehaviourTreeView treeView)
        {
            m_NodeView = nodeView;
            m_Ticker.Start(duration);
            m_TreeView = treeView;
        }

        public bool Update(float dt)
        {
            var finished = m_Ticker.Step(dt);
            return finished;
        }

        public void Draw()
        {
            var pos = m_NodeView.WorldPosition;
            var size = m_NodeView.Size;
            float expandWidth = 10;
            float expandHeight = 10;
            float outlineMinX = pos.x - expandWidth;
            float outlineMinY = pos.y - expandHeight;
            float outlineMaxX = outlineMinX + size.x + expandWidth * 2;
            float outlineMaxY = outlineMinY + size.y + expandHeight * 2;
            m_TreeView.DrawRect(m_TreeView.World2Window(new Vector2(outlineMinX, outlineMinY)), m_TreeView.World2Window(new Vector2(outlineMaxX, outlineMaxY)), new Color(0, 1, 0, 0.5f));
        }

        private readonly Ticker m_Ticker = new();
        private readonly NodeView m_NodeView;
        private readonly BehaviourTreeView m_TreeView;
    }
}
