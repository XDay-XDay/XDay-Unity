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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.House.Editor
{
    internal class HouseObject
    {
        public HouseObject()
        {
        }

        public HouseObject(int id, Vector2Int size, Vector2Int min, Vector2Int max, Quaternion rotation)
        {
            m_ID = id;
            m_Size = size;
            m_Min = min;
            m_Max = max;
            m_ModelRotation = rotation;
        }

        public void Initialize(Transform parent, House house, GameObject prefab, string name)
        {
            House = house;

            CreateGameObject(prefab, parent);

            House.Occupy(m_Min.x, m_Min.y, m_Max.x, m_Max.y, m_ID);

            Name = name;

            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        public virtual void OnDestroy()
        {
            House.Free(m_Min.x, m_Min.y, m_Max.x, m_Max.y);

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
                //TODO:不同Size使用同一种算法
                if (m_Size == Vector2Int.one)
                {
                    House.CalculateCoordinateBoundsByCenterPosition(Position, m_Size, out min, out max);
                }
                else
                {
                    House.CalculateCoordinateBoundsByLowerLeftPosition(Position, m_Size, out min, out max);
                }

                if (!isSelectionValid)
                {
                    Position = CalculateCenterPosition(m_Min, m_Max);
                }
                else if (!House.CanPlaceGridObjectAt(min, max, m_Min, m_Max))
                {
                    //不能放置，恢复原来的坐标
                    if (changePosition)
                    {
                        Position = CalculateCenterPosition(m_Min, m_Max);
                    }
                }
                else
                {
                    if (changePosition)
                    {
                        Position = CalculateCenterPosition(min, max);
                    }
                    if (min != m_Min || max != m_Max)
                    {
                        House.Free(m_Min.x, m_Min.y, m_Max.x, m_Max.y);
                        m_Min = min;
                        m_Max = max;
                        House.Occupy(min.x, min.y, max.x, max.y, ID);
                    }
                }

                OnUpdateMovement();
            }
        }

        protected virtual void OnUpdateMovement() { }

        public virtual void Update(float dt)
        {
        }

        //计算中心点坐标
        protected Vector3 CalculateCenterPosition(Vector2Int min, Vector2Int max)
        {
            return (House.CoordinateToGridPosition(min.x, min.y) + House.CoordinateToGridPosition(max.x + 1, max.y + 1)) * 0.5f;
        }

        protected GameObject CreateErrorPlaceholder()
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var size = m_Max - m_Min + Vector2.one;
            obj.transform.localScale = new Vector3(size.x * House.GridSize, 1, size.y * House.GridSize);
            return obj;
        }

        public virtual void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "GridObject.Version");

            writer.WriteObjectID(m_ID, "ID", converter);
            writer.WriteVector2Int(m_Min, "Min");
            writer.WriteVector2Int(m_Max, "Max");
            writer.WriteVector2Int(m_Size, "Size");
            writer.WriteQuaternion(Rotation, "Rotation");
        }

        public virtual void Load(IDeserializer reader)
        {
            reader.ReadInt32("GridObject.Version");

            m_ID = reader.ReadInt32("ID");
            m_Min = reader.ReadVector2Int("Min");
            m_Max = reader.ReadVector2Int("Max");
            m_Size = reader.ReadVector2Int("Size");
            m_ModelRotation = reader.ReadQuaternion("Rotation");
        }

        protected void CreateGameObject(GameObject prefab, Transform parent)
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
            m_GameObject.transform.SetPositionAndRotation(pos, m_ModelRotation);
            m_GameObject.transform.SetParent(parent, worldPositionStays: true);
            m_GameObject.AddComponent<NoKeyDeletion>();
        }

        public int ID => m_ID;
        public Vector2Int Min { get => m_Min; set => m_Min = value; }
        public Vector2Int Max { get => m_Max; set => m_Max = value; }
        public Vector2Int Size => (Max - Min) + Vector2Int.one;
        public Vector3 Position
        {
            get => m_GameObject.transform.position;
            set => m_GameObject.transform.position = value;
        }
        public Quaternion Rotation
        {
            get => m_GameObject.transform.rotation;
            set => m_GameObject.transform.rotation = value;
        }
        public bool IsPositionDirty { get; set; }
        public virtual bool CanUpdateMovement => true;
        public House House { get; private set; }
        public GameObject GameObject => m_GameObject;
        public string Name
        {
            set
            {
                if (m_GameObject != null)
                {
                    m_GameObject.name = value;
                }
            }
            get
            {
                if (m_GameObject != null)
                {
                    return m_GameObject.name;
                }
                return "";
            }
        }

        [SerializeField]
        private int m_ID;
        [SerializeField]
        protected Vector2Int m_Min;
        [SerializeField]
        protected Vector2Int m_Max;
        [SerializeField]
        private Vector2Int m_Size;
        //grid rotation
        [SerializeField]
        protected Quaternion m_ModelRotation;
        protected GameObject m_GameObject;
        private const int m_Version = 1;
    }
}

