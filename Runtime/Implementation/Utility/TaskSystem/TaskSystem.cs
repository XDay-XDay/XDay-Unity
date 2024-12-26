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



using System.Collections.Generic;
using UnityEngine;

namespace XDay.UtilityAPI
{
    internal class TaskSystem : ITaskSystem
    {
        public TaskSystem(TaskSystemCreateInfo initInfo)
        {
            foreach (var layerInfo in initInfo.LayerInfo)
            {
                for (var i = 0; i < layerInfo.ThreadCount; ++i)
                {
                    m_Threads.Add(new TaskThread(this, layerInfo.Layer));
                }
            }
        }

        public void OnDestroy()
        {
            m_TaskPool.OnDestroy();
            foreach (var thread in m_Threads)
            {
                thread.OnDestroy();
            }
            m_Threads.Clear();
        }

        public void Update()
        {
            foreach (var thread in m_Threads)
            {
                thread.Update();
            }
        }

        public void ReleaseTask<T>(T task) where T : class, ITask
        {
            m_TaskPool.ReleaseTask(task);
        }

        public T GetTask<T>() where T : class, ITask, new()
        {
            return m_TaskPool.GetTask<T>();
        }

        public void ScheduleTask(ITask task)
        {
            var thread = QueryThread(task);
            if (thread != null)
            {
                thread.ScheduleTask(task);
            }
            else
            {
                Debug.LogError("Schedule task failed!");
            }
        }

        private TaskThread QueryThread(ITask task)
        {
            for (var i = 0; i < m_Threads.Count; ++i)
            {
                if (m_Threads[i].Layer == task.Layer)
                {
                    return m_Threads[i];
                }
            }
            return null;
        }

        private TaskPool m_TaskPool = new();
        private List<TaskThread> m_Threads = new();
    }
}

//XDay