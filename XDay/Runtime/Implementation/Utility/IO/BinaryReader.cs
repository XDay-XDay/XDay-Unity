/*
 * Copyright (c) 2024 XDay
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
using System.IO;

namespace XDay.SerializationAPI
{
    internal sealed class BinaryReader
    {
        public Stream Stream => m_Stream;

        public BinaryReader(Stream stream)
        {
            Init(stream);
        }

        public void Init(Stream stream)
        {
            m_Dummy ??= new byte[0];
            m_Cache ??= new byte[16];
            m_Stream = stream;
        }

        public void Uninit()
        {
            m_Stream?.Close();
            m_Stream = null;
            m_Cache = null;
        }

        public bool ReadBoolean()
        {
            ReadCount(1);

            return m_Cache[0] != 0;
        }

        public sbyte ReadSByte()
        {
            ReadCount(1);

            return (sbyte)m_Cache[0];
        }

        public byte ReadByte()
        {
            var v = m_Stream.ReadByte();
            return (byte)v;
        }

        public short ReadInt16()
        {
            ReadCount(2);

            return (short)
                (m_Cache[0] |
                m_Cache[1] << 8);
        }

        public ushort ReadUInt16()
        {
            return (ushort)ReadInt16();
        }

        public int ReadInt32()
        {
            ReadCount(4);

            return 
                m_Cache[0] |
                m_Cache[1] << 8 |
                m_Cache[2] << 16 |
                m_Cache[3] << 24;
        }

        public uint ReadUInt32()
        {
            return (uint)ReadInt32();
        }

        public long ReadInt64()
        {
            ReadCount(8);

            var low32 = (uint)(
                m_Cache[0] |
                m_Cache[1] << 8 | 
                m_Cache[2] << 16 | 
                m_Cache[3] << 24);

            var high32 = (uint)
                (m_Cache[4] | 
                m_Cache[5] << 8 | 
                m_Cache[6] << 16 | 
                m_Cache[7] << 24);

            return (long)((ulong)high32 << 32) | low32;
        }

        public ulong ReadUInt64()
        {
            return (ulong)ReadInt64();
        }

        public unsafe float ReadFloat()
        {
            var v = ReadUInt32();
            return *(float*)&v;
        }

        public unsafe double ReadDouble()
        {
            var v = ReadUInt64();
            return *(double*)&v;
        }

        public byte[] Read(int n)
        {
            if (n == 0)
            {
                return m_Dummy;
            }

            var bytes = new byte[n];
            var pointer = 0;
            do
            {
                var read = m_Stream.Read(bytes, pointer, n);
                if (read == 0)
                {
                    break;
                }

                n -= read;
                pointer += read;
            } while (n > 0);

            if (n != pointer)
            {
                var newBuffer = new byte[pointer];
                Buffer.BlockCopy(bytes, 0, newBuffer, 0, pointer);
                bytes = newBuffer;
            }

            return bytes;
        }

        public int Read(byte[] buffer, int index, int count)
        {
            return m_Stream.Read(buffer, index, count);
        }

        private void ReadCount(int n)
        {
            if (n == 1)
            {
                var v = m_Stream.ReadByte();
                m_Cache[0] = (byte)v;
                return;
            }

            var pointer = 0;
            do
            {
                var v = m_Stream.Read(m_Cache, pointer, n - pointer);
                if (v == 0)
                {
                    break;
                }

                pointer += v;
            }
            while (pointer < n);
        }

        private Stream m_Stream;
        static byte[] m_Dummy;
        private byte[] m_Cache;
    }
}

//XDay
