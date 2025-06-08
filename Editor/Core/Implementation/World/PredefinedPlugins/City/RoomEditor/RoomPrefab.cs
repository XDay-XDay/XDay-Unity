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
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.City.Editor
{
    internal class RoomPrefab : RoomPrefabBase
    {
        public float FacilityLocalY
        {
            get => m_FacilityLocalY;
            set
            {
                if (!Mathf.Approximately(m_FacilityLocalY, value))
                {
                    m_FacilityLocalY = value;
                    foreach (var facility in m_Facilities)
                    {
                        facility.SetLocalY(m_FacilityLocalY);
                    }
                }
            }
        }
        public List<FacilityPrefab> Facilities => m_Facilities;
        public int SelectedFacilityIndex { get => m_SelectedFacilityIndex; set => m_SelectedFacilityIndex = value; }
        public RoomInstance Instance => m_Instance;
        public bool IsVisible
        {
            get
            {
                if (m_Instance == null)
                {
                    return false;
                }
                return m_Instance.Visible;
            }
            set
            {
                if (m_Instance != null)
                {
                    m_Instance.Visible = value;
                }
            }
        }

        public RoomPrefab()
        {
        }

        public RoomPrefab(int id) : base(id)
        {
        }

        public void Initialize(Transform parent, Grid grid)
        {
            m_Grid = grid;
            m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(m_PrefabGUID));
            m_Instance?.Initialize(parent, grid, m_Prefab, this, 0);

            foreach (var facility in m_Facilities)
            {
                facility.Initialize(m_Instance.GameObject.transform, grid, this);
            }

            if (m_Facilities.Count > 0)
            {
                m_SelectedFacilityIndex = 0;
            }
        }

        public void OnDestroy()
        {
            m_Instance?.OnDestroy();
            foreach (var facility in m_Facilities)
            {
                facility.OnDestroy();
            }
            m_Facilities.Clear();
        }

        public void DrawHandle()
        {
            if (m_Instance != null && 
                m_Instance.GameObject != null && 
                m_Instance.GameObject.activeInHierarchy)
            {
                m_Instance.DrawHandle();

                foreach (var facility in m_Facilities)
                {
                    facility.Instance?.DrawHandle();
                }
            }
        }

        public void CreateInstance(int id, float posX, float posZ, Grid grid, Transform parent, RoomPrefabBase prefab)
        {
            var isInRange = grid.CalculateCoordinateBoundsByCenterPosition(new Vector3(posX, 0, posZ), m_Size, out var min, out var max);
            if (!isInRange)
            {
                return;
            }

            m_Instance = new RoomInstance(id, m_Size, min, max, grid.Rotation, prefab);
            m_Instance.Initialize(parent, grid, prefab.Prefab, prefab, 0);
        }

        public void ChangeModel()
        {
            ChangeSize(m_Size.x, m_Size.y);
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

                foreach (var facility in m_Facilities)
                {
                    facility.Instance.GameObject.transform.SetParent(null, worldPositionStays: true);
                }

                m_Instance.OnDestroy();

                var deltaWidth = width - m_Size.x;
                var deltaHeight = height - m_Size.y;
                min.x -= deltaWidth / 2;
                min.y -= deltaHeight / 2;
                m_Instance = new RoomInstance(id, newSize, min, min + newSize - Vector2Int.one, rotation, this);
                m_Instance.Initialize(parent, m_Grid, Prefab, this, 0);

                m_Size = newSize;

                foreach (var facility in m_Facilities)
                {
                    facility.Instance.GameObject.transform.SetParent(m_Instance.GameObject.transform, worldPositionStays: true);
                }
            }
            else
            {
                m_Size = newSize;
            }
        }

        public void AddFacility(FacilityPrefab facility)
        {
            m_Facilities.Add(facility);

            if (m_SelectedFacilityIndex < 0)
            {
                m_SelectedFacilityIndex = 0;
            }
            else
            {
                m_SelectedFacilityIndex = m_Facilities.Count - 1;
            }
        }

        public void RemoveFacility(int index)
        {
            m_Facilities[index].OnDestroy();
            m_Facilities.RemoveAt(index);

            --m_SelectedFacilityIndex;
            if (m_SelectedFacilityIndex < 0 && m_Facilities.Count > 0)
            {
                m_SelectedFacilityIndex = 0;
            }
        }

        public FacilityPrefab GetFacilityPrefab(int index)
        {
            if (index >= 0 && index < m_Facilities.Count)
            {
                return m_Facilities[index];
            }
            return null;
        }

        public RoomFacilityInstance FindFacilityInstance(int id)
        {
            foreach (var facility in m_Facilities)
            {
                if (facility.Instance != null &&
                    facility.Instance.ID == id)
                {
                    return facility.Instance;
                }
            }

            return null;
        }

        public bool HasFacilityAtLocalCoordinate(int x, int y)
        {
            foreach (var facility in m_Facilities)
            {
                if (facility.Instance != null)
                {
                    var localMin = facility.LocalCoordinate;
                    var localMax = localMin + facility.Size;
                    if (x >= localMin.x && x < localMax.x &&
                        y >= localMin.y && y < localMax.y)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override void Save(ISerializer writer, IObjectIDConverter translater)
        {
            base.Save(writer, translater);

            writer.WriteInt32(m_Version, "RoomPrefab.Version");

            writer.WriteSingle(m_FacilityLocalY, "Facility Local Y");

            writer.WriteBoolean(m_Instance != null, "Has Instance");
            m_Instance?.Save(writer, translater);

            writer.WriteList(m_Facilities, "Facilities", (facility, index) => {
                writer.WriteStructure($"Facility {index}", () => {
                    facility.Save(writer, translater);
                });
            });
        }

        public override void Load(IDeserializer reader)
        {
            base.Load(reader);

            var version = reader.ReadInt32("RoomPrefab.Version");

            if (version >= 2)
            {
                m_FacilityLocalY = reader.ReadSingle("Facility Local Y");
            }

            var hasInstance = reader.ReadBoolean("Has Instance");
            if (hasInstance)
            {
                m_Instance = new RoomInstance();
                m_Instance.Load(reader);
            }

            m_Facilities = reader.ReadList("Facilities", (index) => {
                var facility = new FacilityPrefab();
                reader.ReadStructure($"Facility {index}", () => {
                    facility.Load(reader);
                });
                return facility;
            });
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

        protected override void OnPrefabChange()
        {
            ChangeModel();
        }

        [SerializeField]
        private List<FacilityPrefab> m_Facilities = new();
        private int m_SelectedFacilityIndex = -1;
        [SerializeField]
        private RoomInstance m_Instance;
        private const int m_Version = 2;
        [SerializeField]
        private float m_FacilityLocalY;
    }
}