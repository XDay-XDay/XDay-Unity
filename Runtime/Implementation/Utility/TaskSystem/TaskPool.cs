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
using UnityEngine.Pool;

namespace XDay.UtilityAPI
{
    internal class TaskPool
    {
        public void OnDestroy()
        {
            foreach (var kv in m_TaskPools)
            {
                kv.Value.Dispose();
            }
            m_TaskPools.Clear();
        }

        public void ReleaseTask<T>(T task) where T : class, ITask
        {
            m_TaskPools[task.GetType()].Release(task);
        }

        public T GetTask<T>() where T : class, ITask, new()
        {
            m_TaskPools.TryGetValue(typeof(T), out var pool);
            if (pool == null)
            {
                pool = new ObjectPool<ITask>(
                    createFunc:() => { return new T(); },
                    actionOnGet: (task) => { task.OnGetFromPool(); },
                    actionOnRelease: (task) => { task.OnReleaseToPool(); },
                    actionOnDestroy:(task) => { task.OnDestroy(); });

                m_TaskPools[typeof(T)] = pool;
            }

            return pool.Get() as T;
        }

        private Dictionary<Type, ObjectPool<ITask>> m_TaskPools = new();
    }
}

//XDay