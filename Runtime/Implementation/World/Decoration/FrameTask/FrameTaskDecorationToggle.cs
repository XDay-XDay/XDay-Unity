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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Decoration
{
    internal class FrameTaskDecorationToggle : FrameTask
    {
        public override bool End => m_NextObjectIndex == m_ObjectCount;
        public override int Order => m_Type == Type.Activate ? 0 : 1;

        public FrameTaskDecorationToggle(DecorationSystem system, int objectCount, int lod, int gridX, int gridY)
        {
            m_System = system;
            m_ObjectCount = objectCount;
            m_LOD = lod;
            m_GridX = gridX;
            m_GridY = gridY;
        }

        public void Reset()
        {
            m_PrevLOD = -1;
            m_Type = Type.Undefined;
            m_NextObjectIndex = 0;
        }

        public void Init(int prevLOD, Type type)
        {
            Debug.Assert(type != Type.Undefined);
            m_PrevLOD = prevLOD;
            if (m_Type != type)
            {
                var gridData = m_System.GetGrid(m_GridX, m_GridY);
                if (m_Type == Type.Activate)
                {
                    m_System.HideGrid(gridData);
                }
                else
                {
                    m_System.ShowGrid(gridData);
                }
                m_Type = type;
                m_NextObjectIndex = 0;
            }
        }

        public override bool Run(SimpleTimer timer)
        {
            Debug.Assert(m_Type != Type.Undefined);
            if (m_Type == Type.Activate)
            {
                return Activate(timer); 
            }
            else
            {
                return Deactivate(timer);
            }
        }

        private bool Deactivate(SimpleTimer watch)
        {
            Debug.Assert(!End);
            var gridData = m_System.GetGrid(m_GridX, m_GridY);
            var objectCount = gridData.GetObjectCount(m_LOD);
            while (m_NextObjectIndex < objectCount)
            {
                var obj = m_System.QueryVisibleObject(m_GridX, m_GridY, m_LOD, m_NextObjectIndex);
                if (obj != null)
                {
                    m_System.DestroyDecoration(obj, m_PrevLOD, m_LOD);
                }
                ++m_NextObjectIndex;
                if (watch.ElapsedSeconds > GameDefine.MAX_TASK_TIME_SECONDS_PER_FRAME)
                {
                    return true;
                }
            }
            return false;
        }

        private bool Activate(SimpleTimer watch)
        {
            Debug.Assert(!End);
            var gridData = m_System.GetGrid(m_GridX, m_GridY);
            var objectCount = gridData.GetObjectCount(m_LOD);
            while (m_NextObjectIndex < objectCount)
            {
                m_System.CreateDecoration(m_NextObjectIndex, m_GridX, m_GridY, m_PrevLOD, m_LOD);
                ++m_NextObjectIndex;
                if (watch.ElapsedSeconds > GameDefine.MAX_TASK_TIME_SECONDS_PER_FRAME)
                {
                    return true;
                }
            }
            return false;
        }


        internal enum Type
        {
            Undefined,
            Deactivate,
            Activate,
        }

        private Type m_Type = Type.Undefined;
        private readonly DecorationSystem m_System;
        private readonly int m_ObjectCount;
        private int m_PrevLOD;
        private readonly int m_LOD;
        private int m_NextObjectIndex = 0;
        private readonly int m_GridX;
        private readonly int m_GridY;
    }
}

//XDay