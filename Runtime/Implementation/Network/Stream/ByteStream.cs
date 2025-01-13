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
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;

namespace XDay.NetworkAPI
{
    public sealed class ByteStream : Stream
    {
        public override long Position { get => m_Position; set => m_Position = value < 0 ? throw new ArgumentOutOfRangeException() : (int)value; }
        public override bool CanRead => m_Inited;
        public override bool CanSeek => m_Inited;
        public override bool CanWrite => m_Inited;
        public override long Length => m_Length;
        public byte[] Buffer => m_Bytes;
        public int Capacity
        {
            get => m_Bytes.Length;
            set
            {
                if (m_Inited &&
                    value > 0 &&
                    value != m_Bytes.Length)
                {
                    if (value < Length)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    var buffer = m_Pool.Get(value);
                    if (m_Length > 0)
                    {
                        System.Buffer.BlockCopy(m_Bytes, 0, buffer, 0, m_Length);
                    }
                    m_Pool.Release(m_Bytes);
                    m_Bytes = buffer;
                }
            }
        }

        internal ByteStream(IArrayPool arrayPool)
        {
            m_Pool = arrayPool;
        }

        public void Init()
        {
            if (m_Bytes != null)
            {
                Debug.LogError("stream already inited");
                return;
            }
            m_Position = 0;
            m_Length = 0;
            m_Inited = true;
            m_Bytes = m_Pool.Get(0);
        }

        public void Uninit()
        {
            if (m_Bytes == null)
            {
                Debug.LogError("stream already uninited");
                return;
            }
            m_Pool.Release(m_Bytes);
            m_Bytes = null;
            m_Length = 0;
            m_Position = 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        if (offset >= 0)
                        {
                            m_Position = (int)offset;
                        }
                        else
                        {
                            Debug.LogError($"Invalid seek offset {offset}");
                        }
                        break;
                    }
                case SeekOrigin.End:
                    {
                        var newPos = m_Length + (int)offset;
                        if (newPos >= 0)
                        {
                            m_Position = newPos;
                        }
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        var newPos = m_Position + (int)offset;
                        if (newPos >= 0)
                        {
                            m_Position = newPos;
                        }
                        break;
                    }
                default:
                    throw new ArgumentException();
            }
            return m_Position;
        }

        public byte[] ToArray()
        {
            var ret = new byte[m_Length];
            System.Buffer.BlockCopy(m_Bytes, 0, ret, 0, m_Length);
            return ret;
        }

        public int ReadAndDiscard(int n)
        {
            Debug.Assert(n > 0);
            var availableSize = Mathf.Clamp(m_Length - m_Position, 0, n);
            m_Position += availableSize;
            return availableSize;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Debug.Assert(m_Inited &&
                        buffer != null &&
                         offset >= 0 &&
                         count >= 0 &&
                         buffer.Length >= offset + count);

            var readCount = Mathf.Clamp(m_Length - m_Position, 0, count);
            if (readCount > 8)
            {
                System.Buffer.BlockCopy(m_Bytes, m_Position, buffer, offset, readCount);
            }
            else
            {
                for (var i = 0; i < readCount; ++i)
                {
                    buffer[offset + i] = m_Bytes[m_Position + i];
                }
            }
            m_Position += readCount;
            return readCount;
        }

        public int ReadInt32()
        {
            m_Position += 4;
            if (m_Position > m_Length)
            {
                m_Position = m_Length;
                throw new IndexOutOfRangeException();
            }

            return m_Bytes[m_Position - 4] | (m_Bytes[m_Position - 3] << 8) | (m_Bytes[m_Position - 2] << 16) | (m_Bytes[m_Position - 1] << 24);
        }

        public override int ReadByte()
        {
            if (m_Position < m_Length)
            {
                return m_Bytes[m_Position++];
            }
            return -1;
        }

        public override void SetLength(long len)
        {
            Debug.Assert(len >= 0);
            var length = (int)len;
            if (!SetCapacityInternal(length) &&
                length > m_Length)
            {
                Array.Clear(m_Bytes, m_Length, length - m_Length);
            }
            m_Length = length;
            if (m_Position > length)
            {
                m_Position = length;
            }
        }

        public void WriteInt32(int value)
        {
            if (m_Position >= m_Length)
            {
                var newLength = m_Position + 4;
                var overflow = m_Position > m_Length;
                if (newLength >= m_Bytes.Length && 
                    SetCapacityInternal(newLength))
                {
                    overflow = false;
                }
                if (overflow)
                {
                    Array.Clear(m_Bytes, m_Length, m_Position - m_Length);
                }
                m_Length = newLength;
            }

            m_Bytes[m_Position + 0] = (byte)((value >> 24) & 0xff);
            m_Bytes[m_Position + 1] = (byte)((value >> 16) & 0xff);
            m_Bytes[m_Position + 2] = (byte)((value >> 8) & 0xff);
            m_Bytes[m_Position + 3] = (byte)(value & 0xff);
            m_Position += 4;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Debug.Assert(count >= 0 && offset >= 0 && buffer != null && buffer.Length >= offset + count);

            var newPos = m_Position + count;
            if (newPos > m_Length)
            {
                var overflow = m_Position > m_Length;
                if (newPos > m_Bytes.Length && 
                    SetCapacityInternal(newPos))
                {
                    overflow = false;
                }
                if (overflow)
                {
                    Array.Clear(m_Bytes, m_Length, newPos - m_Length);
                }
                m_Length = newPos;
            }

            if (count <= 8 && 
                buffer != m_Bytes)
            {
                for (var i = 0; i < 8; ++i)
                {
                    if (offset + i < buffer.Length)
                    {
                        m_Bytes[m_Position + i] = buffer[offset + i];
                    }
                }
            }
            else
            {
                System.Buffer.BlockCopy(buffer, offset, m_Bytes, m_Position, count);
            }
            m_Position = newPos;
        }

        public override void WriteByte(byte value)
        {
            if (m_Position >= m_Length)
            {
                var newLength = m_Position + 1;
                var overflow = m_Position > m_Length;
                if (newLength >= m_Bytes.Length && SetCapacityInternal(newLength))
                {
                    overflow = false;
                }

                if (overflow)
                {
                    Array.Clear(m_Bytes, m_Length, m_Position - m_Length);
                }

                m_Length = newLength;
            }

            m_Bytes[m_Position++] = value;
        }

        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        public void WriteTo(Stream destination, int offset)
        {
            destination.Write(m_Bytes, offset, m_Length - offset);
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
        }

        private bool SetCapacityInternal(int capacity)
        {
            if (capacity > m_Bytes.Length)
            {
                if (capacity < m_MinCapacity)
                {
                    capacity = m_MinCapacity;
                }
                if (capacity < m_Bytes.Length * 2)
                {
                    capacity = m_Bytes.Length * 2;
                }
                Capacity = capacity;
                return true;
            }
            return false;
        }

        private bool m_Inited = false;
        private readonly IArrayPool m_Pool;
        private byte[] m_Bytes;
        private int m_Length;
        private int m_Position;
        private const int m_MinCapacity = 256;
    }

    public class ByteStreamPool
    {
        public ByteStreamPool(IArrayPool arrayPool)
        {
            m_Pool = new ObjectPool<ByteStream>(
                createFunc: () =>
                {
                    return new ByteStream(arrayPool);
                });
        }

        public ByteStream Get()
        {
            lock (m_Lock)
            {
                var stream = m_Pool.Get();
                stream.Init();
                return stream;
            }
        }

        public void Release(ByteStream stream)
        {
            lock (m_Lock)
            {
                m_Pool.Release(stream);
                stream.Uninit();
            }
        }

        private readonly object m_Lock = new();
        private readonly ObjectPool<ByteStream> m_Pool;
    }
}

//XDay