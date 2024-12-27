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

using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using XDay.UtilityAPI;

namespace XDay.NavigationAPI
{
    internal partial class GridBasedAStarPathFinder : IGridBasedPathFinder
    {
        public GridBasedAStarPathFinder(ITaskSystem taskSystem, IGridData gridData, int neighbourCount)
        {
            m_TaskSystem = taskSystem;
            m_GridData = gridData;
            m_ContextManager = new();
            
            m_HorizontalResolution = gridData.HorizontalResolution;
            m_NeighbourCount = neighbourCount;
        }

        public async UniTask CalculatePathAsync(Vector3 source, Vector3 target, List<Vector3> result, Func<int, float> costOverride, PathFlags flags)
        {
            var completionSource = AutoResetUniTaskCompletionSource.Create();

            var task = m_TaskSystem.GetTask<TaskCalculatePath>();
            task.Init(this, source, target, flags, (path) =>
            {
                result.Clear();
                result.AddRange(path);
            }, costOverride, m_PathPool);
            m_TaskSystem.ScheduleTask(task);

            await completionSource.Task;
        }

        public void CalculatePath(Vector3 source, Vector3 target, List<Vector3> result, Func<int, float> costOverride, PathFlags flags)
        {
            var context = m_ContextManager.GetContext(m_HorizontalResolution);
            CalculatePathInternal(context, source, target, result, flags, costOverride, 0);
        }

        private void MakePath(Context context, 
            Vector2Int sourceCoord, 
            Vector2Int targetCoord, 
            Vector3 source, 
            Vector3 target, 
            PathNode targetNode,
            List<Vector3> result)
        {
            while (true)
            {
                if (targetNode.X == targetCoord.x && 
                    targetNode.Y == targetCoord.y)
                {
                    result.Add(target);
                }
                else if (targetNode.X == sourceCoord.x &&
                    targetNode.Y == sourceCoord.y)
                {
                    result.Add(source);
                }
                else
                {
                    result.Add(m_GridData.CoordinateToGridCenterPosition(targetNode.X, targetNode.Y));
                }
                targetNode = context.GetPreviousNode(targetNode);
                if (targetNode == null)
                {
                    break;
                }
            }

            result.Reverse();
        }

        private bool CalculateCoordinates(Vector3 source, Vector3 target, PathFlags flags, 
            out Vector2Int sourceCoord, out Vector2Int targetCoord, out Vector3 sourcePos, out Vector3 targetPos)
        {
            sourceCoord = m_GridData.PositionToCoordinate(source.x, source.z);
            targetCoord = m_GridData.PositionToCoordinate(target.x, target.z);
            sourcePos = Vector3.zero;
            targetPos = Vector3.zero;

            if (!m_GridData.IsWalkable(sourceCoord.x, sourceCoord.y))
            {
                if (!flags.HasFlag(PathFlags.SourceSearchNearestCoordinate))
                {
                    Debug.LogWarning($"CalculatePath failed, source is not walkable. start:{source}, end:{target}, flags:{flags}");
                    return false;
                }

                sourceCoord = m_GridData.FindNearestWalkableCoordinate(sourceCoord.x, sourceCoord.y, targetCoord, 10);
                sourcePos = m_GridData.CoordinateToGridCenterPosition(sourceCoord.x, sourceCoord.y);
            }
            if (!m_GridData.IsWalkable(sourceCoord.x, sourceCoord.y))
            {
                Debug.LogWarning($"CalculatePath failed, search nearest source coordinate failed! source:{source}, target:{target}, flags:{flags}");
                return false;
            }

            if (!m_GridData.IsWalkable(targetCoord.x, targetCoord.y))
            {
                if (!flags.HasFlag(PathFlags.SourceSearchNearestCoordinate))
                {
                    Debug.LogWarning($"CalculatePath failed, target is not walkable. start:{source}, end:{target}, attribute:{flags}");
                    return false;
                }
                targetCoord = m_GridData.FindNearestWalkableCoordinate(targetCoord.x, targetCoord.y, 10);
                targetPos = m_GridData.CoordinateToGridCenterPosition(targetCoord.x, targetCoord.y);
            }

            if (!m_GridData.IsWalkable(targetCoord.x, targetCoord.y))
            {
                Debug.LogWarning($"CalculatePath failed, search nearest target coordinate failed! source:{source}, target:{target}, flags:{flags}");
                return false;
            }

            return true;
        }

        private float CalculatePathCost(Vector2Int curNode, Vector2Int toNode, Vector2Int targetNode, Func<int, float> costOverride, out float pathCost)
        {
            var heuristicalCost = Vector2Int.Distance(toNode, targetNode);
            var pathDistance = Vector2Int.Distance(curNode, toNode);
            pathCost = m_GridData.GetGridCost(toNode.x, toNode.y, costOverride) * pathDistance;
            return heuristicalCost + pathCost;
        }

        private void ConnectNode(PathNode fromNode, PathNode toNode)
        {
            toNode.PrevX = fromNode.X;
            toNode.PrevY = fromNode.Y;
        }

        private void CalculatePathInternal(Context context,
                    Vector3 source,
                    Vector3 target,
                    List<Vector3> result,
                    PathFlags flags,
                    Func<int, float> costOverride,
                    int loopIndex)
        {
            result.Clear();

            bool ok = CalculateCoordinates(source, target, flags, out var sourceCoord, out var targetCoord, out var sourcePos, out var targetPos);
            if (!ok)
            {
                return;
            }

            var openSet = context.OpenSet;
            var closedSet = context.ClosedSet;
            context.AddToOpenList(sourceCoord, m_HorizontalResolution);

            while (openSet.Count != 0)
            {
                var minCostNode = openSet.Dequeue();

                if (minCostNode.X == targetCoord.x &&
                    minCostNode.Y == targetCoord.y)
                {
                    MakePath(context, sourceCoord, targetCoord, sourcePos, targetPos, minCostNode, result);
                    break;
                }

                if (m_GridData.IsTeleporter(minCostNode.X, minCostNode.Y))
                {
                    if (loopIndex == 0 || 
                        minCostNode.X != sourceCoord.x || 
                        minCostNode.Y != sourceCoord.y)
                    {
                        MakePath(context, sourceCoord, targetCoord, sourcePos, targetPos, minCostNode, result);

                        var teleporterCoord = m_GridData.GetConnectedTeleporterCoordinate(minCostNode.X, minCostNode.Y);
                        var teleporterSource = m_GridData.CoordinateToGridCenterPosition(teleporterCoord.x, teleporterCoord.y);
                        var teleporterToTarget = m_PathPool.Get();

                        m_ContextManager.ReleaseContext(context);
                        context = m_ContextManager.GetContext(m_HorizontalResolution);
                        CalculatePathInternal(context, teleporterSource, target, teleporterToTarget, flags, costOverride, loopIndex + 1);
                        result.AddRange(teleporterToTarget);
                        m_PathPool.Release(teleporterToTarget);
                        return;
                    }
                }

                closedSet.Add(minCostNode.Y * m_HorizontalResolution + minCostNode.X);

                for (var i = 0; i < m_NeighbourCount; ++i)
                {
                    var neighbourCoordinate = m_GridData.GetNeighbourCoordinate(i, minCostNode.X, minCostNode.Y);

                    var connectedTeleportCoordinate = m_GridData.GetConnectedTeleporterCoordinate(neighbourCoordinate.x, neighbourCoordinate.y);

                    if (m_GridData.IsWalkable(neighbourCoordinate.x, neighbourCoordinate.y))
                    {
                        var neighbourIndex = neighbourCoordinate.y * m_HorizontalResolution + neighbourCoordinate.x;
                        if (!closedSet.Contains(neighbourIndex))
                        {
                            var totalCost = CalculatePathCost(
                                new Vector2Int(minCostNode.X, minCostNode.Y),
                                connectedTeleportCoordinate,
                                targetCoord,
                                costOverride,
                                out var pathCost) + minCostNode.TotalPathCost;

                            var pathCostUntilNow = pathCost + minCostNode.TotalPathCost;
                            var neighbourNode = context.GetOpenNode(neighbourIndex);
                            if (neighbourNode == null)
                            {
                                neighbourNode = context.AddToOpenList(neighbourCoordinate, totalCost, pathCostUntilNow);
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

            if (result.Count > 0 && result[0] != source)
            {
                result.Insert(0, source);
            }

            m_ContextManager.ReleaseContext(context);
        }

        private IConcurrentValueListPool<Vector3> m_PathPool = IConcurrentValueListPool<Vector3>.Create();
        private ITaskSystem m_TaskSystem;
        private int m_HorizontalResolution;
        private ContextManager m_ContextManager;
        private int m_NeighbourCount;
        private IGridData m_GridData;
    }
}

//XDay