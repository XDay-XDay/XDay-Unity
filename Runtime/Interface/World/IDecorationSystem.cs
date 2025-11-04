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
using UnityEngine;

namespace XDay.WorldAPI.Decoration
{
    /// <summary>
    /// 装饰物类型
    /// </summary>
    [System.Flags]
    public enum DecorationTagType
    {
        None = 0,
        /// <summary>
        /// 可被建筑物覆盖隐藏的物体
        /// </summary>
        Hideable = 1,

        /// <summary>
        /// 不可被建筑物覆盖隐藏的物体
        /// </summary>
        Obstacle = 2,

        /// <summary>
        /// 可被建筑物覆盖隐藏并且改造前的物体
        /// </summary>
        HideableBefore = 4,

        /// <summary>
        /// 可被建筑物覆盖隐藏并且改造后的物体
        /// </summary>
        HideableAfter = 8,

        /// <summary>
        /// 不可被建筑物覆盖隐藏并且改造前的物体
        /// </summary>
        ObstacleBefore = 16,

        /// <summary>
        /// 不可被建筑物覆盖隐藏并且改造后的物体
        /// </summary>
        ObstacleAfter = 32,

        /// <summary>
        /// 所有可隐藏物体
        /// </summary>
        AllHideable = Hideable | HideableBefore | HideableAfter,

        /// <summary>
        /// 改造前物体
        /// </summary>
        Before = HideableBefore | ObstacleBefore,

        /// <summary>
        /// 改造后物体
        /// </summary>
        After = HideableAfter | ObstacleAfter,

        /// <summary>
        /// 所有物体
        /// </summary>
        All = -1,
    }

    /// <summary>
    /// 装饰物状态
    /// </summary>
    [Flags]
    public enum DecorationState : byte
    {
        /// <summary>
        /// 暂时不可见,还可以改为可见
        /// </summary>
        Invisible = 1,

        /// <summary>
        /// 暂时可见,可以改为NotVisible或NeverVisible
        /// </summary>
        Visible = 2,

        /// <summary>
        /// 永远不可见,也不能改为可见了
        /// </summary>
        NeverVisible = 4,
    }

    /// <summary>
    /// decoration system interface
    /// </summary>
    public interface IDecorationSystem : IWorldPlugin
    {
        /// <summary>
        /// play animation on decoration object
        /// </summary>
        /// <param name="decorationID"></param>
        /// <param name="animationName"></param>
        /// <param name="alwaysPlay"></param>
        void PlayAnimation(int decorationID, string animationName, bool alwaysPlay = false);

        /// <summary>
        /// find decorations in a circle
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="decorationIDs"></param>
        void QueryDecorationIDsInCircle(Vector3 center, float radius, List<int> decorationIDs, DecorationTagType type);

        /// <summary>
        /// show/hide decoration
        /// </summary>
        /// <param name="decorationID"></param>
        /// <param name="state"></param>
        void ShowDecoration(int decorationID, DecorationState state);

        /// <summary>
        /// show/hide decoration in circle
        /// </summary>
        /// <param name="circleCenter"></param>
        /// <param name="circleRadius"></param>
        /// <param name="show"></param>
        void ShowDecoration(Vector3 circleCenter, float circleRadius, DecorationState state, DecorationTagType type);

        /// <summary>
        /// show decorations in specified range and filtered by type
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minZ"></param>
        /// <param name="maxX"></param>
        /// <param name="maxZ"></param>
        /// <param name="show"></param>
        /// <param name="type">只影响指定类型的物体type</param>
        void ShowDecoration(float minX, float minZ, float maxX, float maxZ, DecorationState state, DecorationTagType type);
    }
}
