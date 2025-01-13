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

namespace XDay.NetworkAPI
{
    internal class BlockReader
    {
        public byte[] Buffer => m_Buffer;
        public bool Full => m_ReadCount >= m_BlockSize;
        public int Size => m_BlockSize;
        public int LeftSize => m_BlockSize - m_ReadCount;
        public int ReadCount => m_ReadCount;

        public BlockReader(int size, IArrayPool pool)
        {
            m_Pool = pool;
            SetSize(size);
        }

        public void OnDestroy()
        {
            if (m_Buffer != null)
            {
                m_Pool.Release(m_Buffer);
            }
        }

        public void SetSize(int size)
        {
            m_ReadCount = 0;

            if (m_Buffer == null ||
                m_BlockSize != size)
            {
                if (m_Buffer != null)
                {
                    m_Pool.Release(m_Buffer);
                }
                m_Buffer = m_Pool.Get(size);
            }

            m_BlockSize = size;
        }

        public bool Skip(int n)
        {
            m_ReadCount += n;
            return m_ReadCount >= m_BlockSize;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            count = Mathf.Clamp(count, 0, m_BlockSize - m_ReadCount);
            System.Buffer.BlockCopy(buffer, offset, m_Buffer, m_ReadCount, count);
            m_ReadCount += count;
            return count;
        }

        private readonly IArrayPool m_Pool;
        private byte[] m_Buffer;
        private int m_BlockSize;
        private int m_ReadCount;
    }
}

//XDay
