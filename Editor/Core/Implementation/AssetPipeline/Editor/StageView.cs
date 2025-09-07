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

namespace XDay.AssetPipeline.Editor
{
    internal class StageView
    {
        public Vector2 WorldPosition
        {
            get { return m_Stage.WorldPosition; }
            set
            {
                m_Stage.WorldPosition = value;
                m_Stage.RealPosition = value;
            }
        }
        public Vector2 Size { get { return m_Size; } set { m_Size = value; } }
        public AssetPipelineStage Stage => m_Stage;
        public Texture2D Icon => m_Icon;

        internal StageView(AssetPipelineStage stage, Texture2D icon, Func<Vector2, Vector2> alignFunc)
        {
            m_Stage = stage;
            m_Icon = icon;
            m_AlignFunc = alignFunc;
        }

        public void OnDestroy()
        {
        }

        public void Move(Vector2 offset)
        {
            m_Stage.RealPosition.x += offset.x;
            m_Stage.RealPosition.y -= offset.y;
            if (m_AlignFunc != null)
            {
                m_Stage.WorldPosition = m_AlignFunc.Invoke(m_Stage.RealPosition);
            }
            else
            {
                m_Stage.WorldPosition = m_Stage.RealPosition;
            }
        }

        internal Part HitTest(Vector2 worldPos)
        {
            var maxPos = m_Size + m_Stage.WorldPosition;
            if (InRect(worldPos, ref m_Stage.WorldPosition, ref maxPos))
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
            var center = m_Stage.WorldPosition + m_Size * 0.5f;
            min.x = center.x - m_ButtonSize.x / 2;
            max.x = center.x + m_ButtonSize.x / 2;
            min.y = m_Stage.WorldPosition.y - m_ButtonSize.y;
            max.y = m_Stage.WorldPosition.y;
        }

        public void GetTopRect(out Vector2 min, out Vector2 max)
        {
            var center = m_Stage.WorldPosition + m_Size * 0.5f;
            min.x = center.x - m_ButtonSize.x / 2;
            max.x = center.x + m_ButtonSize.x / 2;
            min.y = m_Stage.WorldPosition.y + m_Size.y;
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

        private Vector2 m_Size = new(100, 100);
        private readonly AssetPipelineStage m_Stage;
        private static Vector2 m_ButtonSize = new Vector2(30, 15);
        private readonly Texture2D m_Icon;
        private readonly Func<Vector2, Vector2> m_AlignFunc;
    }
}

