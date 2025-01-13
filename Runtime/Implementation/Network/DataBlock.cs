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

using System.Net.Sockets;

namespace XDay.NetworkAPI
{
    internal class DataBlock : IDataBlock
    {
        public SocketError Error { get => m_Error; set => m_Error = value; }
        public byte[] Buffer => m_BlockReader.Buffer;
        public bool Full => m_BlockReader.Full;
        public int Size => m_BlockReader.Size;
        public int LeftSize => m_BlockReader.LeftSize;
        public int ReadCount => m_BlockReader.ReadCount;

        public DataBlock(int size, IArrayPool pool)
        {
            m_BlockReader = new(size, pool);
            Error = SocketError.Success;
        }

        public void OnDestroy()
        {
            m_BlockReader.OnDestroy();
        }

        public bool Skip(int n)
        {
            return m_BlockReader.Skip(n);
        }

        public int ReadFrom(byte[] buffer, int offset, int count)
        {
            return m_BlockReader.Read(buffer, offset, count);
        }

        public void SetSize(int size)
        {
            m_BlockReader.SetSize(size);
        }

        private SocketError m_Error = SocketError.Success;
        private readonly BlockReader m_BlockReader;
    }
}

//XDay