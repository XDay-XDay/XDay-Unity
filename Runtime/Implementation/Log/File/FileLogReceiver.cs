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



using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.LogAPI
{
    internal class FileLogReceiver : LogReceiver
    {
        public override void Init(IAspectContainer setting)
        {
            m_UserName = setting.GetString("UserName", Environment.UserName);
            m_Group = setting.GetString("Group", "Default");
            m_FileFolder = $"{Application.persistentDataPath}/XDayLog/{m_Group}/{m_UserName}";
            if (!Directory.Exists(m_FileFolder))
            {
                Directory.CreateDirectory(m_FileFolder);
            }
            m_OneFileMaxSize = setting.GetInt64("OneFileMaxSize", 2 * MB);
            m_Capacity = setting.GetInt64("Capacity", 100 * MB);
            m_FileName = $"{DateTime.Now:yyyy-MM-dd HH-mm-ss}";

            m_Writer = new FileWriter(QueryFilePath(m_FileIndex));

            m_Compressor = new Compressor();
            m_Event = new AutoResetEvent(false);

            var thread = new Thread(new ThreadStart(ThreadUpdate))
            {
                Name = "File Log Flush Thread",
                IsBackground = true
            };
            thread.Start();

            m_CurrentCapacity = 0;
            foreach (var file in Directory.EnumerateFiles(m_FileFolder))
            {
                var fileInfo = new FileInfo(file);
                m_CurrentCapacity += fileInfo.Length;
            }

            Debug.Log($"Log File Path: {m_FileFolder}");
        }

        public override void OnDestroy()
        {
            var path = m_Writer.Path;
            m_Writer?.OnDestroy();
            m_Compressor.Compress(path, null);
            m_Compressor.OnDestroy();
            m_Exit = true;
        }

        public override void OnLogReceived(Utf16ValueStringBuilder builder, LogType type, bool fromUnityDebug)
        {
            if (builder.Length > m_MaxLength)
            {
                m_MaxLength = builder.Length;
            }

            lock (m_Lock)
            {
                m_Queue.Add(builder);
            }
            m_Event.Set();
        }

        private void CompressFinishedCallback(long compressedSize)
        {
            m_CurrentCapacity += compressedSize;
            if (m_CurrentCapacity > m_Capacity)
            {
                try
                {
                    SortFiles();

                    foreach (var filePath in m_ExistedLogFiles)
                    {
                        m_CurrentCapacity -= new FileInfo(filePath).Length;
                        File.Delete(filePath);
                        if (m_CurrentCapacity <= m_Capacity)
                        {
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Assert(false, $"error: {e}");
                }
            }
        }

        private void ThreadUpdate()
        {
            while (!m_Exit)
            {
                m_Event.WaitOne();

                lock (m_Lock)
                {
                    foreach (var builder in m_Queue)
                    {
                        m_Writer.Write(builder.AsSpan());
                        Interlocked.Add(ref m_WriteSize, 2 * builder.Length);
                        builder.Dispose();

                        CheckOverflow();
                    }
                    m_Queue.Clear();

                    m_Writer.Sync();
                }
            }
            m_Event.Dispose();
        }

        private void CheckOverflow()
        {
            if (m_WriteSize >= m_OneFileMaxSize)
            {
                //create new log file
                Interlocked.Add(ref m_FileIndex, 1);
                Helper.InterlockedSet(ref m_WriteSize, 0);

                //compress log file
                m_Compressor.Enqueue(m_Writer.Path, CompressFinishedCallback);
                m_Writer.OnDestroy();
                m_Writer = new FileWriter(QueryFilePath(m_FileIndex));
            }
        }

        private void SortFiles()
        {
            m_ExistedLogFiles.Clear();
            foreach (var file in Directory.EnumerateFiles(m_FileFolder))
            {
                m_ExistedLogFiles.Add(file);
            }
            m_ExistedLogFiles.Sort();
        }

        private string QueryFilePath(int fileIndex)
        {
            return $"{m_FileFolder}/{m_FileName}_{fileIndex}.xdaylog";
        }

        private string m_FileName;
        private Compressor m_Compressor;
        private int m_FileIndex = 0;
        private long m_CurrentCapacity;
        private int m_WriteSize = 0;
        private bool m_Exit = false;
        private string m_Group;
        private readonly List<Utf16ValueStringBuilder> m_Queue = new();
        private AutoResetEvent m_Event;
        private long m_OneFileMaxSize;
        private List<string> m_ExistedLogFiles = new();
        private FileWriter m_Writer;
        private string m_FileFolder;
        private int m_MaxLength = 0;
        private long m_Capacity;
        private readonly object m_Lock = new();
        private string m_UserName;
        private const int MB = 1024 * 1024;
    }
}


//XDay