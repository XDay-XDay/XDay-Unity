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

using System.IO;
using System;

namespace XDay.SerializationAPI
{
    internal class BinaryWriter
    {
        public Stream Stream
        {
            get
            {
                Sync();
                return m_Stream;
            }
        }

        public BinaryWriter(Stream stream)
        {
            Init(stream);
        }

        public void Init(Stream stream)
        {
            m_Stream = stream;
            
            if (stream != null)
            {
                m_Cache ??= new byte[16];
            }
        }

        public void Uninit()
        {
            m_Stream.Close();
        }

        public void Write(bool value)
        {
            m_Cache[0] = (byte)(value ? 1 : 0);
            m_Stream.Write(m_Cache, 0, 1);
        }

        public unsafe void Write(float value)
        {
            var v = *(uint*)&value;
            Write(v);
        }

        public void Write(short value)
        {
            Write((ushort)value);
        }

        public void Write(ushort value)
        {
            m_Cache[0] = (byte)value;
            m_Cache[1] = (byte)(value >> 8);
            m_Stream.Write(m_Cache, 0, 2);
        }

        public void Write(int value)
        {
            Write((uint)value);
        }

        public void Write(uint value)
        {
            m_Cache[0] = (byte)value;
            m_Cache[1] = (byte)(value >> 8);
            m_Cache[2] = (byte)(value >> 16);
            m_Cache[3] = (byte)(value >> 24);

            m_Stream.Write(m_Cache, 0, 4);
        }

        public void Write(long value)
        {
            Write((ulong)value);
        }

        public void Write(byte[] buffer)
        {
            m_Stream.Write(buffer, 0, buffer.Length);
        }

        public unsafe void Write(double value)
        {
            ulong v = *(ulong*)&value;

            Write(v);
        }

        public void Write(ulong value)
        {
            m_Cache[0] = (byte)value;
            m_Cache[1] = (byte)(value >> 8);
            m_Cache[2] = (byte)(value >> 16);
            m_Cache[3] = (byte)(value >> 24);
            m_Cache[4] = (byte)(value >> 32);
            m_Cache[5] = (byte)(value >> 40);
            m_Cache[6] = (byte)(value >> 48);
            m_Cache[7] = (byte)(value >> 56);

            m_Stream.Write(m_Cache, 0, 8);
        }

        public void Write(byte[] buffer, int index, int count)
        {
            m_Stream.Write(buffer, index, count);
        }

        public void Write(byte value)
        {
            m_Stream.WriteByte(value);
        }

        public void Write(sbyte value)
        {
            m_Stream.WriteByte((byte)value);
        }

        public long Seek(int offset, SeekOrigin origin)
        {
            return m_Stream.Seek(offset, origin);
        }

        public void Sync()
        {
            m_Stream.Flush();
        }

        private byte[] m_Cache;
        private Stream m_Stream;
    }
}

//XDay