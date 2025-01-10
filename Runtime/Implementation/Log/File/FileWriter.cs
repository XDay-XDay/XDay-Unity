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
using UnityEngine;
using System.Runtime.InteropServices;

namespace XDay.LogAPI
{
    internal class FileWriter
    {
        public string Path => m_Path;

        public FileWriter(string path)
        {
            m_Path = path;
            try
            {
                m_FileStream = new FileStream(path, FileMode.Append);
            }
            catch (Exception e)
            {
                Debug.LogError($"open file {path} failed :{e}");
            }
        }

        public void OnDestroy()
        {
            Sync();
            m_FileStream?.Dispose();
        }

        public void Sync()
        {
            m_FileStream.Flush();
        }

        public void Write(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty)
            {
                return;
            }

            lock (m_Lock)
            {
                unsafe
                {
                    fixed (void* ptr = &MemoryMarshal.GetReference(span))
                    {
                        m_FileStream.Write(new ReadOnlySpan<byte>(ptr, span.Length * 2));
                    }
                }
            }
        }

        private readonly object m_Lock = new();
        private readonly FileStream m_FileStream;
        private readonly string m_Path;
    }
}

//XDay
