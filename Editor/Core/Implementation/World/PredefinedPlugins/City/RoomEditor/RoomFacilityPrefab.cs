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

using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.City.Editor
{
    internal class FacilityPrefab : RoomPrefabBase
    {
        public int ConfigID { get => m_ConfigID; set => m_ConfigID = value; }
        public RoomFacilityInstance Instance => m_Instance;
        public Vector2Int LocalCoordinate
        {
            get
            {
                if (m_Instance == null)
                {
                    return Vector2Int.zero;
                }
                return m_Instance.Min - m_Room.Instance.Min;
            }
        }

        public void SetLocalY(float y)
        {
            if (m_Instance != null)
            {
                var pos = m_Instance.GameObject.transform.position;
                pos.y = y;
                m_Instance.GameObject.transform.position = pos;
            }
        }

        public void CreateInstance(int id, float posX, float posZ, Grid grid, Transform parent, float localY)
        {
            var isInRange = grid.CalculateCoordinateBoundsByCenterPosition(new Vector3(posX, 0, posZ), m_Size, out var min, out var max);
            if (!isInRange)
            {
                return;
            }

            m_Instance = new RoomFacilityInstance(id, m_Size, min, max, grid.Rotation, this);
            m_Instance.Initialize(parent, grid, Prefab, this, localY);
        }

        public void Initialize(Transform parent, Grid grid, RoomPrefab room)
        {
            m_Grid = grid;
            m_Room = room;
            m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(m_PrefabGUID));
            m_Instance?.Initialize(parent, grid, m_Prefab, this, room.FacilityLocalY);
        }

        public void OnDestroy()
        {
            m_Instance?.OnDestroy();
        }

        public void ChangeSize(int width, int height)
        {
            var newSize = new Vector2Int(width, height);

            if (m_Instance != null)
            {
                var id = m_Instance.ID;
                var min = m_Instance.Min;
                var rotation = m_Instance.Rotation;
                var parent = m_Instance.GameObject.transform.parent;
                m_Instance.OnDestroy();

                var deltaWidth = width - m_Size.x;
                var deltaHeight = height - m_Size.y;
                min.x -= deltaWidth / 2;
                min.y -= deltaHeight / 2;

                m_Instance = new RoomFacilityInstance(id, newSize, min, min + newSize - Vector2Int.one, rotation, this);
                m_Instance.Initialize(parent, m_Grid, Prefab, this, m_Room.FacilityLocalY);
            }

            m_Size = newSize;
        }

        public void ChangeModel()
        {
            ChangeSize(m_Size.x, m_Size.y);
        }

        protected override void OnPrefabChange()
        {
            ChangeModel();
        }

        public override void Save(ISerializer writer, IObjectIDConverter translater)
        {
            base.Save(writer, translater);

            writer.WriteInt32(m_Version, "FacilityPrefab.Version");
            writer.WriteInt32(m_ConfigID, "Config ID");

            writer.WriteBoolean(m_Instance != null, "Has Instance");
            m_Instance?.Save(writer, translater);
        }

        public override void Load(IDeserializer reader)
        {
            base.Load(reader);

            reader.ReadInt32("FacilityPrefab.Version");
            m_ConfigID = reader.ReadInt32("Config ID");

            var hasInstance = reader.ReadBoolean("Has Instance");
            if (hasInstance)
            {
                m_Instance = new RoomFacilityInstance();
                m_Instance.Load(reader);
            }
        }

        protected override void OnSetName()
        {
            if (m_Instance != null)
            {
                m_Instance.GameObject.name = m_Name;
                var displayName = m_Instance.GameObject.GetComponent<DisplayName>();
                if (displayName != null)
                {
                    displayName.SetText(m_Name);
                }
            }
        }

        private int m_ConfigID;
        private RoomFacilityInstance m_Instance;
        private RoomPrefab m_Room;
        private const int m_Version = 1;
    }
}
