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
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.City.Editor
{
    /// <summary>
    /// 建筑内部编辑
    /// </summary>
    internal partial class RoomEditor
    {
        enum Mode
        {
            Room,
            Facility,
        }

        public List<RoomPrefab> Rooms { get { return m_Rooms; } }

        public string[] RoomNames
        {
            get
            {
                if (m_RoomNames == null || m_RoomNames.Length != m_Rooms.Count)
                {
                    m_RoomNames = new string[m_Rooms.Count];
                }

                for (var i = 0; i < m_RoomNames.Length; i++)
                {
                    m_RoomNames[i] = m_Rooms[i].Name;
                }

                return m_RoomNames;
            }
        }

        public void Initialize(Grid grid)
        {
            m_Grid = grid;

            m_Root = new GameObject("房间内部编辑");
            m_Root.transform.SetParent(grid.RootGameObject.transform, false);

            SubscribeEvents();

            foreach (var building in m_Rooms)
            {
                building.Initialize(m_Root.transform, grid);
            }

            if (m_Rooms.Count > 0)
            {
                SetMainBuildingSelection(0);
            }
        }

        public void OnDestroy()
        {
            UnsubscribeEvents();

            Object.DestroyImmediate(m_Root);
        }

        public void Start()
        {
            m_Grid.ShowRoots(false);
            m_Root.SetActive(true);
        }

        public void Stop()
        {
            m_Grid.ShowRoots(true);
            m_Root.SetActive(false);
        }

        public void DrawSceneGUI()
        {
            foreach (var room in m_Rooms)
            {
                room.DrawHandle();
            }
        }

        public int GetRoomIndex(int id)
        {
            for (var i = 0; i < m_Rooms.Count; ++i)
            {
                if (m_Rooms[i].ID == id)
                {
                    return i;
                }
            }

            return -1;
        }

        public RoomPrefab GetRoomPrefab(int index)
        {
            if (index >= 0 && index < m_Rooms.Count)
            {
                return m_Rooms[index];
            }

            return null;
        }

        public RoomPrefab GetRoomPrefabByID(int id)
        {
            foreach (var prefab in m_Rooms)
            {
                if (prefab.ID == id)
                {
                    return prefab;
                }
            }

            return null;
        }

        public void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "RoomEditor.Version");
            writer.WriteBoolean(m_ShowRooms, "Show Rooms");

            writer.WriteList(m_Rooms, "Rooms", (room, index) =>
            {
                writer.WriteStructure($"Room {index}", () =>
                {
                    room.Save(writer, converter);
                });
            });
        }

        public void Load(IDeserializer reader)
        {
            var version = reader.ReadInt32("RoomEditor.Version");
            m_ShowRooms = reader.ReadBoolean("Show Rooms");

            m_Rooms = reader.ReadList("Rooms", (int index) =>
            {
                var room = new RoomPrefab();
                reader.ReadStructure($"Room {index}", () =>
                {
                    room.Load(reader);
                });
                return room;
            });
        }

        private void SubscribeEvents()
        {
            WorldEditor.EventSystem.Register(this, (BuildingPositionChangeEvent e) => {
                var building = FindBuildingInstance(e.BuildingID);
                building?.OnPositionChanged();
            });
            WorldEditor.EventSystem.Register(this, (UpdateBuildingMovementEvent e) => {
                var building = FindBuildingInstance(e.BuildingID);
                building?.UpdateMovement(changePosition: true, forceUpdate: false, e.IsSelectionValid);
            });
            WorldEditor.EventSystem.Register(this, (DrawGridEvent e) => {
                var building = FindBuildingInstance(e.BuildingID);
                building?.DrawHandle();
            });
        }

        private void UnsubscribeEvents()
        {
            WorldEditor.EventSystem.Unregister(this);
        }

        private RoomInstanceBase FindBuildingInstance(int id)
        {
            foreach (var mainBuilding in m_Rooms)
            {
                if (mainBuilding.Instance != null &&
                    mainBuilding.Instance.ID == id)
                {
                    return mainBuilding.Instance;
                }

                var instance = mainBuilding.FindFacilityInstance(id);
                if (instance != null)
                {
                    return instance;
                }
            }

            return null;
        }

        private Mode m_Mode = Mode.Room;
        private string[] m_ModeNames = new string[]
        {
            "房间",
            "设施",
        };
        private Grid m_Grid;
        private GameObject m_Root;

        [SerializeField]
        private List<RoomPrefab> m_Rooms = new();

        private string[] m_RoomNames;

        [SerializeField]
        private bool m_ShowRooms = true;

        private int m_SelectedRoomIndex = -1;

        private const int m_Version = 1;
    }
}
