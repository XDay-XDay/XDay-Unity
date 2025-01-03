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



using Priority_Queue;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using XDay.UtilityAPI;

namespace XDay.NavigationAPI
{
    internal partial class GridBasedAStarPathFinder
    {
        internal class ContextManager
        {
            public Context GetContext(int horizontalResolution)
            {
                var context = m_Pool.Get();
                context.Init(horizontalResolution);
                return context;
            }

            public void ReleaseContext(Context context)
            {
                Debug.Assert(context != null);
                context.Clear();
                m_Pool.Release(context);
            }

            private readonly IConcurrentObjectPool<Context> m_Pool = IConcurrentObjectPool<Context>.Create(() => { return new Context(); });
        }

        internal class Context
        {
            public PathNode AddToOpenList(Vector2Int coordinate, float totalCost = 0, float totalPathCost = 0)
            {
                var node = NodePool.Get();
                node.X = coordinate.x;
                node.Y = coordinate.y;
                node.TotalPathCost = totalPathCost;
                OpenSet.Enqueue(node, totalCost);

                var index = coordinate.y * m_HorizontalResolution + coordinate.x;
                m_OpenNodes.Add(index, node);
                return node;
            }

            public PathNode GetPreviousNode(PathNode childNode)
            {
                return GetOpenNode(childNode.PrevY * m_HorizontalResolution + childNode.PrevX);
            }

            public PathNode GetOpenNode(int index)
            {
                m_OpenNodes.TryGetValue(index, out var node);
                return node;
            }

            public void Init(int horizontalResolution)
            {
                m_HorizontalResolution = horizontalResolution;
            }

            public void Clear()
            {
                foreach (var kv in m_OpenNodes)
                {
                    NodePool.Release(kv.Value);
                }
                m_OpenNodes.Clear();
                ClosedSet.Clear();
                OpenSet.Clear();
            }

            public HashSet<int> ClosedSet = new();
            public FastPriorityQueue<PathNode> OpenSet = new(maxNodes: 10000);
            public IObjectPool<PathNode> NodePool = new ObjectPool<PathNode>(() => { return new PathNode(); }, actionOnGet: null, actionOnRelease: (node) => { node.Clear(); });
            private Dictionary<int, PathNode> m_OpenNodes = new();
            private int m_HorizontalResolution;
        }

        internal class TaskCalculatePath : TaskBase
        {
            public void Init(GridBasedAStarPathFinder pathFinder,
                Vector3 source,
                Vector3 target,
                PathFlags flags,
                System.Action<List<Vector3>> onTaskCompleted,
                Func<int, float> costOverride,
                IConcurrentStructListPool<Vector3> pathPool)
            {
                m_PathFinder = pathFinder;
                m_Source = source;
                m_Target = target;
                m_PathFlags = flags;
                m_OnTaskCompleted = onTaskCompleted;
                m_CostOverride = costOverride;
                m_PathPool = pathPool;
            }

            public override void OnTaskCompleted(ITaskOutput output)
            {
                m_OnTaskCompleted?.Invoke(m_Path);
                m_Path.Clear();
                m_PathPool.Release(m_Path);
            }

            public override ITaskOutput Run()
            {
                m_Path = m_PathPool.Get();
                m_PathFinder.CalculatePath(m_Source, m_Target, m_Path, m_CostOverride, m_PathFlags);
                return null;
            }

            private PathFlags m_PathFlags;
            private IGridBasedPathFinder m_PathFinder;
            private Action<List<Vector3>> m_OnTaskCompleted;
            private IConcurrentStructListPool<Vector3> m_PathPool;
            private List<Vector3> m_Path;
            private Vector3 m_Source;
            private Vector3 m_Target;
            private Func<int, float> m_CostOverride;
        }

        internal class PathNode : FastPriorityQueueNode
        {
            public void Clear()
            {
                PrevX = -1;
                PrevY = -1;
            }

            public static bool operator !=(PathNode a, PathNode b)
            {
                return !(a == b);
            }

            public static bool operator ==(PathNode a, PathNode b)
            {
                return a.X == b.X &&
                    a.Y == b.Y;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(X, Y);
            }

            public override bool Equals(object obj)
            {
                return (obj as PathNode) == this;
            }


            public float TotalPathCost = 0;
            public int PrevX = -1;
            public int PrevY = -1;
            public int X;
            public int Y;
        }
    }
}


//XDay