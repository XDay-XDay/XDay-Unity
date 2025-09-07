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
using UnityEngine.Scripting;

namespace XDay.WorldAPI.House
{
    /// <summary>
    /// 获取房间数据
    /// </summary>
    [Preserve]
    public interface IHouseSystem : IWorldPlugin
    {
        /// <summary>
        /// 房间数量
        /// </summary>
        int HouseCount { get; }

        /// <summary>
        /// 根据索引获取房间位置
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Vector3 GetHousePositionByIndex(int index);

        /// <summary>
        /// 根据配置ID获取房间位置
        /// </summary>
        /// <param name="configID"></param>
        /// <returns></returns>
        Vector3 GetHousePositionByID(int configID);

        /// <summary>
        /// 获取房间数据
        /// </summary>
        /// <param name="configID"></param>
        /// <returns></returns>
        IHouse GetHouseDataByID(int configID);

        /// <summary>
        /// 获取交互点数据
        /// </summary>
        /// <param name="configID"></param>
        /// <returns></returns>
        IInteractivePoint GetInteractivePoint(int configID);

        /// <summary>
        /// 获取途经的各个房间中的路径分段
        /// </summary>
        /// <param name="start">房间内的任意世界坐标</param>
        /// <param name="end">房间内的任意世界坐标</param>
        /// <param name="pathInRooms">各个房间内的路径</param>
        /// <returns></returns>
        bool FindPath(Vector3 start, Vector3 end, List<List<Vector3>> pathInRooms);

        /// <summary>
        /// 获取途经的各个房间中的路径分段
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        bool FindPath(Vector3 start, Vector3 end, List<Vector3> paths);

        /// <summary>
        /// 设置传送点开关状态
        /// </summary>
        /// <param name="configID"></param>
        /// <param name="on"></param>
        void SetTeleporterState(int configID, bool on);

        /// <summary>
        /// 获取传送点开关状态
        /// </summary>
        /// <param name="configID"></param>
        /// <returns></returns>
        bool GetTeleporterState(int configID);

        /// <summary>
        /// 是否显示房间调试渲染
        /// </summary>
        /// <param name="show"></param>
        void ShowRenderer(bool show);
    }

    /// <summary>
    /// 交互点
    /// </summary>
    public interface IInteractivePoint
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        int ConfigID { get; }

        /// <summary>
        /// 起点坐标
        /// </summary>
        Vector3 StartPosition { get; }

        /// <summary>
        /// 起点旋转
        /// </summary>
        Quaternion StartRotation { get; }

        /// <summary>
        /// 终点坐标
        /// </summary>
        Vector3 EndPosition { get; }

        /// <summary>
        /// 终点旋转
        /// </summary>
        Quaternion EndRotation { get; }
    }

    public interface IHouse
    {
        /// <summary>
        /// 房间配置ID
        /// </summary>
        int ConfigID { get; }

        /// <summary>
        /// 房间坐标
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// 房间范围,由房间的BoxCollider确定大小
        /// </summary>
        Bounds WorldBounds { get; }

        /// <summary>
        /// 获取房间内所有交互点
        /// </summary>
        List<IInteractivePoint> InteractivePoints { get; }

        /// <summary>
        /// 判断坐标是否在房间内
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        bool Contains(Vector3 position);

        /// <summary>
        /// 获取左边传送点坐标
        /// </summary>
        /// <returns></returns>
        Vector3 GetLeftTeleporterPosition();

        /// <summary>
        /// 获取房间内随机坐标
        /// </summary>
        /// <param name="curPos">当前坐标,不会生成当前坐标所在格子的坐标</param>
        /// <returns></returns>
        Vector3 GetRandomWalkablePosition(Vector3 curPos);
    }
}
