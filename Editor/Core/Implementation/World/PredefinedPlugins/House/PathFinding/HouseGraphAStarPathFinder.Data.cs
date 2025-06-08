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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace XDay.WorldAPI.House.Editor
{
    internal partial class HouseGraphAStarPathFinder
    {
        internal class ContextManager
        {
            public Context GetContext()
            {
                var context = m_Pool.Get();
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
            public PathNode AddToOpenList(IGraphNode graphNode, float totalCost = 0, float totalPathCost = 0)
            {
                var node = NodePool.Get();
                node.GraphNode = graphNode;
                node.TotalPathCost = totalPathCost;
                node.PrevGraphNode = null;
                OpenSet.Enqueue(node, totalCost);

                m_OpenNodes.Add(graphNode.ID, node);
                return node;
            }

            public PathNode GetPreviousNode(PathNode childNode)
            {
                if (childNode.PrevGraphNode == null)
                {
                    return null;
                }
                return GetOpenNode(childNode.PrevGraphNode.ID);
            }

            public PathNode GetOpenNode(int index)
            {
                m_OpenNodes.TryGetValue(index, out var node);
                return node;
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
            public ObjectPool<PathNode> NodePool = new ObjectPool<PathNode>(() => { return new PathNode(); }, actionOnGet: null, actionOnRelease: (node) => { node.Clear(); });
            private Dictionary<int, PathNode> m_OpenNodes = new();
        }

        //internal class TaskCalculatePath : TaskBase
        //{
        //    public void Init(HouseGraphAStarPathFinder pathFinder,
        //        IGraphNode source,
        //        IGraphNode target,
        //        System.Action<List<IGraphEdge>> onTaskCompleted,
        //        IConcurrentValueListPool<IGraphEdge> pathPool)
        //    {
        //        m_PathFinder = pathFinder;
        //        m_Source = source;
        //        m_Target = target;
        //        m_OnTaskCompleted = onTaskCompleted;
        //        m_PathPool = pathPool;
        //    }

        //    public override void OnTaskCompleted(ITaskOutput output)
        //    {
        //        m_OnTaskCompleted?.Invoke(m_Path);
        //        m_Path.Clear();
        //        m_PathPool.Release(m_Path);
        //    }

        //    public override ITaskOutput Run()
        //    {
        //        m_Path = m_PathPool.Get();
        //        m_PathFinder.CalculatePath(m_Source, m_Target, m_Path);
        //        return null;
        //    }

        //    private HouseGraphAStarPathFinder m_PathFinder;
        //    private Action<List<Vector3>> m_OnTaskCompleted;
        //    private IConcurrentStructListPool<IGraphEdge> m_PathPool;
        //    private List<IGraphEdge> m_Path;
        //    private IGraphNode m_Source;
        //    private IGraphNode m_Target;
        //}

        internal class PathNode : FastPriorityQueueNode
        {
            public void Clear()
            {
                PrevGraphNode = null;
            }

            public static bool operator !=(PathNode a, PathNode b)
            {
                return !(a == b);
            }

            public static bool operator ==(PathNode a, PathNode b)
            {
                return a.GraphNode == b.GraphNode;
            }

            public override int GetHashCode()
            {
                return GraphNode.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return (obj as PathNode) == this;
            }

            public float TotalPathCost = 0;
            public IGraphNode GraphNode;
            public IGraphNode PrevGraphNode;
        }
    }
}


//XDay