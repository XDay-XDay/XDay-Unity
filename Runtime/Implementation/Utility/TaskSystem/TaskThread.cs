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
using System.Threading;
using UnityEngine;

namespace XDay.UtilityAPI
{
    internal class TaskThread
    {
        public int Layer => m_Layer;

        public TaskThread(TaskSystem system, int layer)
        {
            m_Notification = new AutoResetEvent(false);
            m_TaskSystem = system;
            m_Layer = layer;
            m_ThreadHandle = new Thread(ThreadFunc)
            {
                IsBackground = true,
            };
            m_ThreadHandle.Start();
        }

        public void OnDestroy()
        {
            m_Notification.Dispose();
            m_Notification = null;

            if (m_TaskQueue.Count > 0 ||
                m_ResultQueue.Count > 0)
            {
                Debug.LogError("has pending work not cleaned");
            }

            m_Exit = true;
        }

        public void Update()
        {
            CancelTasks();
            TaskCompleted();
        }

        public void ScheduleTask(ITask task)
        {
            lock (m_TaskQueueLock)
            {
                m_TaskQueue.Enqueue(task);
            }

            m_Notification.Set();
        }

        private void CancelTasks()
        {
            lock (m_CancelQueueLock)
            {
                for (var i = 0; i < m_CancelQueue.Count; ++i)
                {
                    m_TempCancelQueue.Add(m_CancelQueue[i]);
                }
                m_CancelQueue.Clear();
            }

            foreach (var task in m_TempCancelQueue)
            {
                task.OnTaskCancelled();
            }
            m_TempCancelQueue.Clear();
        }

        private void ThreadFunc()
        {
            while (!m_Exit)
            {
                m_Notification?.WaitOne();

                while (true)
                {
                    ITask task = null;
                    lock (m_TaskQueueLock)
                    {
                        if (m_TaskQueue.Count > 0)
                        {
                            task = m_TaskQueue.Dequeue();
                        }
                    }

                    if (task == null)
                    {
                        break;
                    }

                    ITaskOutput output = null;
                    if (!task.RequestCancel)
                    {
                        output = task.Run();
                    }
                    lock (m_ResultQueueLock)
                    {
                        m_ResultQueue.Add(new Item(task, output));
                    }
                }
            }
        }

        private void TaskCompleted()
        {
            lock (m_ResultQueueLock)
            {
                for (var i = 0; i < m_ResultQueue.Count; ++i)
                {
                    m_TempResultQueue.Add(m_ResultQueue[i]);
                }
                m_ResultQueue.Clear();
            }

            foreach (var taskDone in m_TempResultQueue)
            {
                taskDone.Task.OnTaskCompleted(taskDone.Result);
                m_TaskSystem.ReleaseTask(taskDone.Task);
            }
            m_TempResultQueue.Clear();
        }

        private struct Item
        {
            public Item(ITask task, ITaskOutput result)
            {
                Task = task;
                Result = result;
            }

            public ITask Task;
            public ITaskOutput Result;
        }

        private TaskSystem m_TaskSystem;
        private List<Item> m_TempResultQueue = new();
        private bool m_Exit = false;
        private Queue<ITask> m_TaskQueue = new();
        private int m_Layer;
        private object m_CancelQueueLock = new();
        private object m_TaskQueueLock = new();
        private object m_ResultQueueLock = new();
        private List<Item> m_ResultQueue = new();
        private List<ITask> m_CancelQueue = new();
        private AutoResetEvent m_Notification;
        private List<ITask> m_TempCancelQueue = new();
        private Thread m_ThreadHandle;
    }
}

//XDay