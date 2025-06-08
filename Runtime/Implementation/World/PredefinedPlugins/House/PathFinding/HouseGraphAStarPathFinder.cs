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
using UnityEngine;
using XDay.NavigationAPI;

namespace XDay.WorldAPI.House
{
    public interface IGraphNode
    {
        int ID { get; }
        string Name { get; }
        List<IGraphEdge> Edges { get; }
        Vector3 Position { get; }
        IGridData GridData { get; }
        bool IsEnabled { get; }
    }

    public interface IGraphEdge
    {
        IGraphNode From { get; }
        IGraphNode To { get; }
        bool IsVirtual { get; }
    }

    internal partial class HouseGraphAStarPathFinder
    {
        public HouseGraphAStarPathFinder(List<IGraphEdge> allEdges, ITaskSystem taskSystem)
        {
            m_TaskSystem = taskSystem;
            m_AllEdges = allEdges;
            m_ContextManager = new();
        }

        public void CalculatePath(IGraphNode start, IGraphNode target, List<IGraphEdge> result)
        {
            var context = m_ContextManager.GetContext();
            CalculatePathInternal(context, start, target, result);
        }

        //public async UniTask CalculatePathAsync(Vector3 source, Vector3 target, List<Vector3> result, Func<int, float> costOverride, PathFlags flags)
        //{
        //    var completionSource = AutoResetUniTaskCompletionSource.Create();

        //    var task = m_TaskSystem.GetTask<TaskCalculatePath>();
        //    task.Init(this, source, target, flags, (path) =>
        //    {
        //        result.Clear();
        //        result.AddRange(path);
        //    }, costOverride, m_PathPool);
        //    m_TaskSystem.ScheduleTask(task);

        //    await completionSource.Task;
        //}

        private void MakePath(Context context,
            PathNode targetNode,
            List<IGraphEdge> result)
        {
            while (true)
            {
                var edge = GetEdge(targetNode.PrevGraphNode, targetNode.GraphNode);
                if (edge != null)
                {
                    result.Add(edge);
                }
                targetNode = context.GetPreviousNode(targetNode);
                if (targetNode is null)
                {
                    break;
                }

            }

            result.Reverse();
        }

        private IGraphEdge GetEdge(IGraphNode from, IGraphNode to)
        {
            foreach (var edge in m_AllEdges)
            {
                if (edge.From == from && edge.To == to)
                {
                    return edge;
                }
            }
            return null;
        }

        private float CalculatePathCost(IGraphNode curNode, IGraphNode toNode, IGraphNode targetNode, out float pathCost)
        {
            var heuristicalCost = Vector3.Distance(toNode.Position, targetNode.Position);
            pathCost = Vector3.Distance(curNode.Position, toNode.Position);
            return heuristicalCost + pathCost;
        }

        private void ConnectNode(PathNode fromNode, PathNode toNode)
        {
            toNode.PrevGraphNode = fromNode.GraphNode;
        }

        private void CalculatePathInternal(Context context,
                    IGraphNode source,
                    IGraphNode target,
                    List<IGraphEdge> result)
        {
            result.Clear();

            var openSet = context.OpenSet;
            var closedSet = context.ClosedSet;
            context.AddToOpenList(source);

            while (openSet.Count != 0)
            {
                PathNode minCostNode = openSet.Dequeue();
                //var minName = minCostNode.GraphNode.Name;
                if (minCostNode.GraphNode == target)
                {
                    MakePath(context, minCostNode, result);
                    break;
                }

                closedSet.Add(minCostNode.GraphNode.ID);

                for (var i = 0; i < minCostNode.GraphNode.Edges.Count; ++i)
                {
                    IGraphEdge edge = minCostNode.GraphNode.Edges[i];
                    if (edge.From.IsEnabled)
                    {
                        IGraphNode neighbour = edge.To;
                        //var name = neighbour.Name;
                        if (!closedSet.Contains(neighbour.ID))
                        {
                            var totalCost = CalculatePathCost(
                                minCostNode.GraphNode,
                                neighbour,
                                target,
                                out var pathCost) + minCostNode.TotalPathCost;

                            var pathCostUntilNow = pathCost + minCostNode.TotalPathCost;
                            PathNode neighbourNode = context.GetOpenNode(neighbour.ID);
                            if (neighbourNode is null)
                            {
                                neighbourNode = context.AddToOpenList(neighbour, totalCost, pathCostUntilNow);
                                ConnectNode(minCostNode, neighbourNode);
                            }
                            else
                            {
                                if (pathCostUntilNow < neighbourNode.TotalPathCost)
                                {
                                    neighbourNode.TotalPathCost = pathCostUntilNow;
                                    openSet.UpdatePriority(neighbourNode, totalCost);
                                    ConnectNode(minCostNode, neighbourNode);
                                }
                            }
                        }
                    }
                }
            }

            m_ContextManager.ReleaseContext(context);
        }

        private ITaskSystem m_TaskSystem;
        private ContextManager m_ContextManager;
        private List<IGraphEdge> m_AllEdges;
    }
}

//XDay