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
using UnityEngine;

namespace XDay.BehaviourTreeAPI.Editor
{
    internal partial class BehaviourTreeView
    {
        private void DrawStatus(NodeView nodeView)
        {
            var pos = nodeView.WorldPosition;
            var size = nodeView.Size;
            float iconMaxX = pos.x + size.x;
            float iconMinX = iconMaxX - m_StatusIconSize;
            float iconMinY = pos.y;
            float iconMaxY = iconMinY + m_StatusIconSize;

            var min = World2Window(new Vector2(iconMinX, iconMinY));
            var max = World2Window(new Vector2(iconMaxX, iconMaxY));

            var node = nodeView.Node;

            if (Application.isPlaying)
            {
                if (node.Status == NodeStatus.Success)
                {
                    DrawTexture(min, max, m_SuccessTexture);
                }
                else if (node.Status == NodeStatus.Fail)
                {
                    DrawTexture(min, max, m_FailTexture);
                }
                else if (node.Status == NodeStatus.Running && node.Started)
                {
                    DrawTexture(min, max, m_RunningTexture);
                }
            }

            //绘制dynamic图标
            if (node is DynamicNode dyNode && dyNode.Dynamic)
            {
                min = World2Window(new Vector2(pos.x + size.x - m_DynamicIconSize, pos.y + size.y - m_NameHeight - m_DynamicIconSize));
                max = World2Window(new Vector2(pos.x + size.x, pos.y + size.y - m_NameHeight));
                DrawTexture(min, max, m_DynamicTexture);
            }

            if (node.HasBreakpoint == true)
            {
                min = World2Window(new Vector2(pos.x, pos.y + size.y - m_NameHeight - m_DynamicIconSize));
                max = World2Window(new Vector2(pos.x + m_DynamicIconSize, pos.y + size.y - m_NameHeight));
                DrawTexture(min, max, m_BreakpointTexture);
            }
        }

        public void AddStartEffect(NodeView nodeView)
        {
            var effect = new NodeStartEffect(nodeView, 0.5f, this);
            m_Effects.Add(effect);
        }

        private void DrawNodeEffects()
        {
            for (var i = 0; i < m_Effects.Count; ++i)
            {
                m_Effects[i].Draw(); 
            }
        }

        private void UpdateNodeEffects()
        {
            if (Application.isPlaying)
            {
                float dt = Time.deltaTime;

                for (var i = m_Effects.Count - 1; i >= 0; --i)
                {
                    if (m_Effects[i].Update(dt))
                    {
                        m_Effects.RemoveAt(i);
                    }
                }
            }
        }

        private List<NodeStartEffect> m_Effects = new();
    }
}
