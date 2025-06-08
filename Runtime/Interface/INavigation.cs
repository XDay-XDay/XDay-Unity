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

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XDay.NavigationAPI
{
    [Flags]
    public enum PathFlags
    {
        None = 0,
        SourceSearchNearestCoordinate = 1,
        TargetSearchNearestCoordinate = 2,
    }

    /// <summary>
    /// grid based path finder
    /// </summary>
    public interface IGridBasedPathFinder
    {
        static IGridBasedPathFinder Create(ITaskSystem taskSystem, IGridData gridData, int neighbourCount)
        {
            return new GridBasedAStarPathFinder(taskSystem, gridData, neighbourCount);
        }

        UniTask CalculatePathAsync(Vector3 source, 
            Vector3 target, 
            List<Vector3> path, 
            Func<int, float> costOverride = null, 
            PathFlags flags = PathFlags.SourceSearchNearestCoordinate | PathFlags.TargetSearchNearestCoordinate);

        void CalculatePath(Vector3 source, 
            Vector3 target, 
            List<Vector3> path, 
            Func<int, float> costOverride = null, 
            PathFlags flags = PathFlags.SourceSearchNearestCoordinate | PathFlags.TargetSearchNearestCoordinate);
    }

    /// <summary>
    /// grid data needed by path finder
    /// </summary>
    public interface IGridData
    {
        int HorizontalGridCount { get; }
        int VerticalGridCount { get; }

        bool IsTeleporter(int x, int y);
        Vector2Int GetConnectedTeleporterCoordinate(int x, int y);
        void SetTeleporterState(int id, bool on);
        bool GetTeleporterState(int id);
        float GetGridCost(int x, int y, Func<int, float> costOverride = null);
        bool IsWalkable(int x, int y);
        Vector2Int GetNeighbourCoordinate(int selfX, int selfY, int neighbourIndex);
        Vector3 CoordinateToGridCenterPosition(int x, int y);
        Vector2Int PositionToCoordinate(Vector3 worldPos);
        Vector2Int FindNearestWalkableCoordinate(int x, int y, Vector2Int referencePoint, int searchDistance);
        Vector2Int FindNearestWalkableCoordinate(int x, int y, int searchDistance);
    }

    public interface INavigationManager
    {
        static INavigationManager Create()
        {
            return new NavigationManager();
        }

        void OnDestroy();

        /// <summary>
        /// create a grid based path finder
        /// </summary>
        /// <param name="taskSystem"></param>
        /// <param name="gridData"></param>
        /// <param name="neighbourCount"></param>
        /// <returns></returns>
        IGridBasedPathFinder CreateGridPathFinder(ITaskSystem taskSystem, IGridData gridData, int neighbourCount);
    }

    public interface IGridNavigationAgent
    {
        static IGridNavigationAgent Create(GameObject overrideGameObject, Transform parent, Vector3 position, Quaternion rotation)
        {
            return new GridBasedNavAgent(overrideGameObject, parent, position, rotation);
        }

        bool IsMoving { get; }
        IGridBasedPathFinder PathFinder { set; get; }
        float MoveSpeed { get; set; }
        float RotateSpeed { get; set; }
        event Action EventStartMove; 
        event Action EventStopMove; 

        void OnDestroy();
        void Update(float dt);
        bool MoveTo(Vector3 target);
        void MoveTo(List<Vector3> path);
    }
}

//XDay