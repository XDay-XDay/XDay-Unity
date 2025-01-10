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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using UnityEngine;

namespace XDay.LogAPI
{
    internal class Compressor
    {
        public Compressor()
        {
            m_Event = new AutoResetEvent(initialState: false);
            var thread = new Thread(new ThreadStart(ThreadFunc))
            {
                IsBackground = true,
                Name = "Log File Compression Thread",
            };
            thread.Start();
        }

        public void OnDestroy()
        {
            m_Finish = true;
            m_Event.Set();
        }

        public void Compress(string originalFilePath, Action<long> onCompressFinished)
        {
            var compressFilePath = $"{originalFilePath}.gz";
            try
            {
                using (var originalFileStream = new FileStream(originalFilePath, FileMode.Open, FileAccess.Read))
                {
                    using var compressedFileStream = new FileStream(compressFilePath, FileMode.Create);
                    using var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress);
                    while (true)
                    {
                        var n = originalFileStream.Read(m_TempBuffer);
                        if (n <= 0)
                        {
                            break;
                        }
                        compressionStream.Write(m_TempBuffer, 0, n);
                    }
                }

                onCompressFinished?.Invoke(new FileInfo(compressFilePath).Length);
                File.Delete(originalFilePath);
            }
            catch (Exception e)
            {
                Debug.Assert(false, $"compress file {originalFilePath} failed: {e.Message}");
            }
        }

        public void Enqueue(string filePath, Action<long> onCompressFinished)
        {
            lock (m_Lock)
            {
                m_Tasks.Add(new Task() { OnCompressFinished = onCompressFinished, FilePath = filePath });
            }
            m_Event.Set();
        }

        private void ThreadFunc()
        {
            while (!m_Finish)
            {
                m_Event.WaitOne();

                lock (m_Lock)
                {
                    foreach (var task in m_Tasks)
                    {
                        Compress(task.FilePath, task.OnCompressFinished);
                    }
                    m_Tasks.Clear();
                }
            }

            m_Event.Dispose();
        }

        private bool m_Finish = false;
        private readonly byte[] m_TempBuffer = new byte[4096];
        private readonly object m_Lock = new();
        private readonly List<Task> m_Tasks = new();
        private AutoResetEvent m_Event;

        private struct Task
        {
            public Action<long> OnCompressFinished;
            public string FilePath;
        }
    }
}
