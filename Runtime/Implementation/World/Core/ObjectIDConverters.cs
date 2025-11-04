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

namespace XDay.WorldAPI
{
    /// <summary>
    /// 将临时ID转换为序列化ID,不同类型的ID分段设置
    /// </summary>
    public class ToPersistentID : IObjectIDConverter
    {
        public ToPersistentID(int idOffset)
        {
            m_IDOffset = idOffset;
        }

        public int Convert(int id)
        {
            if (id != 0)
            {
                var found = m_ObjectIDToPersistentID.TryGetValue(id, out var fileID);
                if (found)
                {
                    return fileID;
                }

                fileID = ++m_NextPersistentID + m_IDOffset;
                m_ObjectIDToPersistentID.Add(id, fileID);
                return fileID;
            }

            return 0;
        }

        private int m_NextPersistentID;
        private int m_IDOffset;
        private Dictionary<int, int> m_ObjectIDToPersistentID = new();
    }

    public class NewObjectID : IObjectIDConverter
    {
        public NewObjectID(World world)
        {
            m_World = world;
        }

        public int Convert(int id)
        {
            return m_World.AllocateObjectID();
        }

        private World m_World;
    }
}

//XDay