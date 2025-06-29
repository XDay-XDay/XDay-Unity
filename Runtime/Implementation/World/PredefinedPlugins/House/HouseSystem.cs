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
    [Preserve]
    internal partial class HouseSystem : WorldPlugin, IHouseSystem
    {
        public override List<string> GameFileNames => new() { "house" };
        public override string TypeName => "HouseSystem";
        public override string Name { get => m_Name; set => throw new System.NotImplementedException(); }
        public int HouseCount => m_Houses.Count;

        public HouseSystem()
        {
        }

        protected override void InitInternal()
        {
            var taskSystem = World.WorldManager.TaskSystem;
            m_Graph = new(m_Houses, taskSystem);

            foreach (var house in m_Houses)
            {
                house.Init(taskSystem);
            }
        }

        protected override void UninitInternal()
        {
            UninitRendererInternal();
        }

        public void ShowRenderer(bool show)
        {
            InitRendererInternal();
            m_Renderer.SetActive(show);
        }

        protected override void InitRendererInternal()
        {
            m_Renderer ??= new HouseSystemRenderer(this);
        }

        protected override void UninitRendererInternal()
        {
            m_Renderer?.OnDestroy();
            m_Renderer = null;
        }

        protected override void UpdateInternal(float dt)
        {
        }

        protected override void LoadGameDataInternal(string pluginName, IWorld world)
        {
            var reader = world.QueryGameDataDeserializer(world.ID, $"house@{pluginName}");

            reader.ReadInt32("HouseSystem.Version");

            m_Name = reader.ReadString("Name");
            var houseCount = reader.ReadInt32("House Count");
            for (var i = 0; i < houseCount; i++)
            {
                var id = reader.ReadInt32("House Config ID");
                var houseName = reader.ReadString("House Name");
                var position = reader.ReadVector3("House Position");
                var prefabPath = reader.ReadString("House Prefab Path");
                float gridSize = reader.ReadSingle("Grid Size");
                float gridHeight = reader.ReadSingle("Grid Height");
                var worldBounds = reader.ReadBounds("World Bounds");
                var horizontalGridCount = reader.ReadInt32("Horizontal Grid Count");
                var verticalGridCount = reader.ReadInt32("Vertical Grid Count");
                bool[] walkable = reader.ReadBooleanArray("Walkable");

                List<Teleporter> teleporters = new();
                var teleporterCount = reader.ReadInt32("Teleporter Count");
                for (var t = 0; t < teleporterCount; t++)
                {
                    var configID = reader.ReadInt32("Teleporter Config ID");
                    var connectedID = reader.ReadInt32("Teleporter Connected ID");
                    var pos = reader.ReadVector3("Teleporter World Position");
                    var name = reader.ReadString("Teleporter Name");
                    var enabled = reader.ReadBoolean("Teleporter State");
                    var teleporter = new Teleporter(configID, connectedID, name, pos, enabled);
                    teleporters.Add(teleporter);
                }

                List<IInteractivePoint> points = new();
                var pointCount = reader.ReadInt32("Interactive Point Count");
                for (var k = 0; k < pointCount; k++)
                {
                    var pointID = reader.ReadInt32("Interactive Point Config ID");
                    var startPos = reader.ReadVector3("Start World Position");
                    var endPos = reader.ReadVector3("End World Position");
                    var startRot = reader.ReadQuaternion("Start Rotation");
                    var endRot = reader.ReadQuaternion("End Rotation");
                    var point = new InteractivePoint(pointID, startPos, startRot, endPos, endRot);
                    points.Add(point);
                    m_AllInteractivePoints[pointID] = point;
                }

                var house = new HouseData(id, houseName, horizontalGridCount, verticalGridCount, gridSize, gridHeight, worldBounds, position, prefabPath, points, teleporters, walkable);
                m_Houses.Add(house);
                if (!m_HousesDic.ContainsKey(id))
                {
                    m_HousesDic.Add(id, house);
                }
            }

            reader.Uninit();
        }

        public IHouse GetHouseDataByID(int configID)
        {
            if (m_HousesDic.TryGetValue(configID, out var houseDic))
            {
                return houseDic;
            }

            //Debug.LogError($"房间ID{configID}没找到");
            return null;
        }

        public HouseData GetHouseDataByIndex(int index)
        {
            if (index >= 0 && index < m_Houses.Count)
            {
                return m_Houses[index];
            }
            return null;
        }

        public Vector3 GetHousePositionByIndex(int index)
        {
            var house = GetHouseDataByIndex(index);
            if (house != null)
            {
                return house.Position;
            }
            return Vector3.zero;
        }

        public Vector3 GetHousePositionByID(int configID)
        {
            var house = GetHouseDataByID(configID);
            if (house != null)
            {
                return house.Position;
            }
            return Vector3.zero;
        }

        public IInteractivePoint GetInteractivePoint(int configID)
        {
            m_AllInteractivePoints.TryGetValue(configID, out var point);
            return point;
        }

        /// <summary>
        /// 判断坐标在哪个房间内
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public IHouse SearchHouse(Vector3 position)
        {
            foreach (var house in m_Houses)
            {
                if (house.Contains(position))
                {
                    return house;
                }
            }
            return null;
        }

        public bool FindPath(Vector3 start, Vector3 end, List<List<Vector3>> pathInRooms)
        {
            var startHouse = SearchHouse(start) as HouseData;
            var endHouse = SearchHouse(end) as HouseData;

            return m_Graph.FindPath(start, end, startHouse, endHouse, pathInRooms);
        }

        public void SetTeleporterState(int configID, bool on)
        {
            foreach (var house in m_Houses)
            {
                if (house.SetTeleporterStateWithCheck(configID, on))
                {
                    return;
                }
            }
            Debug.LogError($"设置传送点{configID}状态失败");
        }

        public bool GetTeleporterState(int configID)
        {
            foreach (var house in m_Houses)
            {
                if (house.GetTeleporterStateWithCheck(configID, out var state))
                {
                    return state;
                }
            }
            Debug.LogError($"获取传送点{configID}状态失败");
            return false;
        }

        private string m_Name;
        private readonly List<HouseData> m_Houses = new();
        private readonly Dictionary<int, HouseData> m_HousesDic = new();
        private readonly Dictionary<int, InteractivePoint> m_AllInteractivePoints = new();
        private HouseSystemRenderer m_Renderer;
        private HouseGraph m_Graph;
    }
}

