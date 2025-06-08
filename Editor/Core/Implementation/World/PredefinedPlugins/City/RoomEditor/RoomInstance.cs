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

using UnityEngine;
using UnityEditor;
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.City.Editor
{
    internal abstract class RoomInstanceBase
    {
        public RoomInstanceBase()
        {
        }

        public RoomInstanceBase(int id, Vector2Int size, Vector2Int min, Vector2Int max, Quaternion rotation, RoomPrefabBase buildingPrefab)
        {
            m_ID = id;
            m_Size = size;
            m_Min = min;
            m_Max = max;
            m_Rotation = rotation;
            m_BuildingPrefab = buildingPrefab;
        }

        public void Initialize(Transform parent, Grid ownerGrid, GameObject prefab, RoomPrefabBase buildingPrefab, float localY)
        {
            //fix id
            if (m_ID == 0)
            {
                m_ID = ownerGrid.CityEditor.NextObjectID;
            }

            m_Grid = ownerGrid;
            m_BuildingPrefab = buildingPrefab;

            CreateGameObject(prefab, parent, localY);
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_GameObject);
        }

        public void OnPositionChanged()
        {
            IsPositionDirty = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="changePosition"></param>
        /// <param name="forceUpdate"></param>
        /// <param name="isSelectionValid">是否选中的有效物体</param>
        public void UpdateMovement(bool changePosition, bool forceUpdate, bool isSelectionValid)
        {
            if ((IsPositionDirty && CanUpdateMovement) || forceUpdate)
            {
                IsPositionDirty = false;

                Vector2Int min, max;
                if (m_Size == Vector2Int.one)
                {
                    m_Grid.CalculateCoordinateBoundsByCenterPosition(Position, m_Size, out min, out max);
                }
                else
                {
                    m_Grid.CalculateCoordinateBoundsByLowerLeftPosition(Position, m_Size, out min, out max);
                }

                if (!isSelectionValid)
                {
                    Position = CalculateCenterPosition(m_Min, m_Max);
                }
                else
                {
                    if (changePosition)
                    {
                        Position = CalculateCenterPosition(min, max);
                    }
                    if (min != m_Min || max != m_Max)
                    {
                        m_Min = min;
                        m_Max = max;
                    }
                }
            }
        }

        //计算中心点坐标
        Vector3 CalculateCenterPosition(Vector2Int min, Vector2Int max)
        {
            return (m_Grid.CoordinateToGridPosition(min.x, min.y) + m_Grid.CoordinateToGridPosition(max.x + 1, max.y + 1)) * 0.5f;
        }

        GameObject CreateErrorPlaceholder()
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var size = m_Max - m_Min + Vector2.one;
            obj.transform.localScale = new Vector3(size.x * m_Grid.GridSize, 1, size.y * m_Grid.GridSize);
            return obj;
        }

        private void CreateGameObject(GameObject prefab, Transform parent, float localY)
        {
            if (prefab != null)
            {
                m_GameObject = Object.Instantiate(prefab);
            }
            else
            {
                m_GameObject = CreateErrorPlaceholder();
            }

            var pos = CalculateCenterPosition(m_Min, m_Max);
            pos.y += localY;
            m_GameObject.transform.SetPositionAndRotation(pos, m_Rotation);
            m_GameObject.transform.SetParent(parent, worldPositionStays: true);
            m_GameObject.AddComponent<NoKeyDeletion>();
            m_GameObject.name = m_BuildingPrefab.Name;

            var behaviour = m_GameObject.AddComponent<BuildingBehaviour>();
            behaviour.Initialize(ID, m_Grid.ID, (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); });

            var name = m_GameObject.AddComponent<DisplayName>();
            name.Create(m_BuildingPrefab.Name, m_Grid.CityEditor.World.CameraManipulator.Camera, m_Grid.CityEditor.ShowName);
        }

        public void ChangeModel(GameObject prefab)
        {
            Transform parent = null;
            if (m_GameObject != null)
            {
                parent = m_GameObject.transform.parent;
            }

            var behaviour = m_GameObject.GetComponent<BuildingBehaviour>();
            behaviour.ProcessDestroyEvent = false;

            Object.DestroyImmediate(m_GameObject);

            CreateGameObject(prefab, parent, 0);

            Selection.activeGameObject = m_GameObject;
        }

        public void DrawHandle()
        {
            if (m_Grid.RootGameObject != null)
            {
                var size = Size;
                Handles.matrix = Matrix4x4.TRS(Position, m_Grid.RootGameObject.transform.rotation, new Vector3(size.x * m_Grid.GridSize, 0.2f, size.y * m_Grid.GridSize));

                var oldColor = Handles.color;
                Handles.color = new Color(1, 0, 0, 0.5f);

                Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1, EventType.Repaint);
                Handles.color = oldColor;
                Handles.matrix = Matrix4x4.identity;
            }
        }

        public virtual void Save(ISerializer writer, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_Version, "RoomInstanceBase.Version");

            writer.WriteObjectID(m_ID, "ID", translator);
            writer.WriteVector2Int(m_Min, "Min");
            writer.WriteVector2Int(m_Max, "Max");
            writer.WriteVector2Int(m_Size, "Size");
            writer.WriteQuaternion(m_Rotation, "Rotation");
        }

        public virtual void Load(IDeserializer reader)
        {
            reader.ReadInt32("RoomInstanceBase.Version");

            m_ID = reader.ReadInt32("ID");
            m_Min = reader.ReadVector2Int("Min");
            m_Max = reader.ReadVector2Int("Max");
            m_Size = reader.ReadVector2Int("Size");
            m_Rotation = reader.ReadQuaternion("Rotation");
        }

        public int ID => m_ID;
        public Vector2Int Min { get => m_Min; set => m_Min = value; }
        public Vector2Int Max { get => m_Max; set => m_Max = value; }
        public Vector2Int Size => (Max - Min) + Vector2Int.one;
        public Vector3 Position
        {
            get {
                if (m_GameObject == null)
                {
                    return Vector3.zero;
                }
                return m_GameObject.transform.position;
            }
                
            set => m_GameObject.transform.position = value;
        }
        public Quaternion Rotation
        {
            get => m_GameObject.transform.rotation;
            set => m_GameObject.transform.rotation = value;
        }
        public bool IsPositionDirty { get; set; }
        public virtual bool CanUpdateMovement => true;
        public GameObject GameObject => m_GameObject;
        public RoomPrefabBase BuildingPrefab => m_BuildingPrefab;
        public abstract bool IsFacility { get; }
        public bool Visible { get
            {
                if (m_GameObject == null)
                {
                    return false;
                }
                return m_GameObject.activeSelf;
            }
            set
            {
                if (m_GameObject != null)
                {
                    m_GameObject.SetActive(value);
                }
            }
        }

        [SerializeField]
        private int m_ID;
        [SerializeField]
        private Vector2Int m_Min;
        [SerializeField]
        private Vector2Int m_Max;
        [SerializeField]
        private Vector2Int m_Size;
        [SerializeField]
        private Quaternion m_Rotation;
        private GameObject m_GameObject;
        private RoomPrefabBase m_BuildingPrefab;
        private Grid m_Grid;

        private const int m_Version = 1;
    }

    internal class RoomInstance : RoomInstanceBase
    {
        public override bool IsFacility => false;

        public RoomInstance()
        {
        }

        public RoomInstance(int id, Vector2Int size, Vector2Int min, Vector2Int max, Quaternion rotation, RoomPrefabBase prefab)
            : base(id, size, min, max, rotation, prefab)
        {
        }
    }

    internal class RoomFacilityInstance : RoomInstanceBase
    {
        public override bool IsFacility => true;

        public RoomFacilityInstance()
        {
        }

        public RoomFacilityInstance(int id, Vector2Int size, Vector2Int min, Vector2Int max, Quaternion rotation, RoomPrefabBase prefab)
            : base(id, size, min, max, rotation, prefab)
        {
        }
    }
}
