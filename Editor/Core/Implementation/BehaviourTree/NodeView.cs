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


using System;
using UnityEngine;

namespace XDay.BehaviourTreeAPI.Editor
{
    internal class NodeView
    {
        public bool ShowComment { get => m_ShowComment; set => m_ShowComment = value; }
        public Vector2 WorldPosition { get { return m_WorldPosition; } 
            set
            { 
                m_WorldPosition = value;
                m_RealPosition = value;
            }
        }
        public Vector2 Size { get { return m_Size; } set { m_Size = value; } }
        public Node Node => m_Node;
        public Texture2D Icon => m_Icon;

        internal NodeView(Node node, Texture2D icon, Func<Vector2, Vector2> alignFunc, BehaviourTreeView treeView)
        {
            m_Node = node;
            m_Icon = icon;
            m_AlignFunc = alignFunc;
            m_TreeView = treeView;
            node.Event += OnNodeEvent;
        }

        public void OnDestroy()
        {
            Node.Event -= OnNodeEvent;
        }

        public void Move(Vector2 offset)
        {
            m_RealPosition.x += offset.x;
            m_RealPosition.y -= offset.y;
            if (m_AlignFunc != null)
            {
                m_WorldPosition = m_AlignFunc.Invoke(m_RealPosition);
            }
            else
            {
                m_WorldPosition = m_RealPosition;
            }
        }

        internal Part HitTest(Vector2 worldPos)
        {
            var maxPos = m_Size + m_WorldPosition;
            if (InRect(worldPos, ref m_WorldPosition, ref maxPos))
            {
                return Part.Center;
            }

            GetBottomRect(out var min, out var max);
            if (InRect(worldPos, ref min, ref max))
            {
                return Part.Bottom;
            }

            GetTopRect(out min, out max);
            if (InRect(worldPos, ref min, ref max))
            {
                return Part.Top;
            }

            return Part.None;
        }

        public Vector2 GetBottomCenter()
        {
            GetBottomRect(out var min, out var max);
            return (min + max) * 0.5f;
        }

        public Vector2 GetTopCenter()
        {
            GetTopRect(out var min, out var max);
            return (min + max) * 0.5f;
        }

        public void GetBottomRect(out Vector2 min, out Vector2 max)
        {
            var center = m_WorldPosition + m_Size * 0.5f;
            min.x = center.x - m_ButtonSize.x / 2;
            max.x = center.x + m_ButtonSize.x / 2;
            min.y = m_WorldPosition.y - m_ButtonSize.y;
            max.y = m_WorldPosition.y;
        }

        public void GetTopRect(out Vector2 min, out Vector2 max)
        {
            var center = m_WorldPosition + m_Size * 0.5f;
            min.x = center.x - m_ButtonSize.x / 2;
            max.x = center.x + m_ButtonSize.x / 2;
            min.y = m_WorldPosition.y + m_Size.y;
            max.y = min.y + m_ButtonSize.y;
        }

        private bool InRect(Vector2 pos, ref Vector2 min, ref Vector2 max)
        {
            if (pos.x >= min.x && pos.x < max.x &&
                pos.y >= min.y && pos.y < max.y)
            {
                return true;
            }
            return false;
        }

        private void OnNodeEvent(Node node, NodeEvent e)
        {
            if (e == NodeEvent.Start && node == Node)
            {
                m_TreeView.AddStartEffect(this);
            }
        }

        private BehaviourTreeView m_TreeView;
        private Vector2 m_WorldPosition;
        private Vector2 m_RealPosition;
        private Vector2 m_Size = new(100, 100);
        private readonly Node m_Node;
        private static Vector2 m_ButtonSize = new Vector2(30, 15);
        private readonly Texture2D m_Icon;
        private readonly Func<Vector2, Vector2> m_AlignFunc;
        private bool m_ShowComment = false;
    }
}

