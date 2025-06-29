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


namespace XDay.GamePlayAPI
{
    public class UnitCreateInfo
    {
        public long ID;
        public string Name;
    }

    public abstract class Unit
    {
        public long ID => m_ID;
        public string Name { get => m_Name; set => m_Name = value; }
        /// <summary>
        /// 能否渲染
        /// </summary>
        public abstract bool Renderable { get; }
        /// <summary>
        /// 高优先级的物体会立即更新渲染
        /// </summary>
        public virtual bool IsHighPriority { get; }
        public int CurrentLOD => m_CurrentLOD;
        public int LastLOD => m_LastLOD;

        public virtual void Init(UnitCreateInfo info)
        {
            if (info != null)
            {
                m_ID = info.ID;
                m_Name = info.Name;
            }
        }

        public void UpdateLOD(int lod)
        {
            m_LastLOD = m_CurrentLOD;
            m_CurrentLOD = lod;
        }

        public abstract void Uninit();

        private string m_Name;
        protected long m_ID;
        private int m_CurrentLOD = -1;
        private int m_LastLOD = -1;
    }
}
