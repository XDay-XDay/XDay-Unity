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

namespace XDay.WorldAPI.House.Editor
{
    internal class HouseInteractivePoint
    {
        public int ID => m_ID;
        public string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                if (m_Root != null)
                {
                    m_Root.name = value;
                }
            }
        }
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public bool Visible { get => m_Root.activeSelf; set => m_Root.SetActive(value); }
        public InteractivePointStartCoordinate Start { get => m_Start; set => m_Start = value; }
        public InteractivePointEndCoordinate End { get => m_End; set => m_End = value; }
        public Vector3 Position { set => m_Root.transform.position = value; get => m_Root.transform.position; }

        public HouseInteractivePoint()
        {
        }

        public HouseInteractivePoint(int id)
        {
            m_ID = id;
        }

        public virtual void Initialize(House house)
        {
            if (m_ID == 0)
            {
                m_ID = house.World.AllocateObjectID();
            }

            m_House = house;
            m_Root = new GameObject(m_Name);
            m_Root.transform.SetParent(house.InteractivePointRoot.transform);
            m_Root.AddComponent<NoKeyDeletion>();
            var behaviour = m_Root.AddComponent<HouseInteractivePointBehaviour>();
            behaviour.Initialize(this);

            m_Start.Initialize(m_Root.transform, "起点");
            m_End.Initialize(m_Root.transform, "终点");
            Start.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldHelper.GetPrefabPath("Coin/CoinYellow.prefab"));
            Start.PrefabInstance.name = "起点";
            End.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldHelper.GetPrefabPath("Coin/CoinGreen.prefab"));
            End.PrefabInstance.name = "终点";
        }

        public void OnDestroy()
        {
            Object.DestroyImmediate(m_Root);
        }

        public void CopyStartCoordinate()
        {
            if (m_End.PrefabInstance == null ||
                m_Start.PrefabInstance == null)
            {
                EditorUtility.DisplayDialog("出错了", "没有设置起点或终点的模型", "确定");
            }
            else
            {
                m_End.Position = m_Start.Position;
                m_End.Rotation = m_Start.Rotation;
            }
        }

        public void RotateAround(Vector3 worldPos)
        {
            var rot = Quaternion.Euler(0, 180, 0);
            {
                var local = m_Start.Position - worldPos;
                local = rot * local;
                m_Start.Position = worldPos + local;
                m_Start.Rotation *= rot;
            }

            {
                var local = m_End.Position - worldPos;
                local = rot * local;
                m_End.Position = worldPos + local;
                m_End.Rotation *= rot;
            }
        }

        public void SetLocalStartPosition(Vector3 pos)
        {
            m_Start.Position = m_House.Root.transform.TransformPoint(pos);
        }

        public void SetLocalEndPosition(Vector3 pos)
        {
            m_End.Position = m_House.Root.transform.TransformPoint(pos);
        }

        public void SetLocalStartRotation(Quaternion rot)
        {
            m_Start.Rotation = rot;
        }

        public void SetLocalEndRotation(Quaternion rot)
        {
            m_End.Rotation = rot;
        }

        public Vector3 GetLocalStartPosition()
        {
            return m_House.Root.transform.InverseTransformPoint(m_Start.Position);
        }

        public Vector3 GetLocalEndPosition()
        {
            return m_House.Root.transform.InverseTransformPoint(m_End.Position);
        }

        public virtual void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "InteractivePoint.Version");

            writer.WriteObjectID(m_ID, "ID", converter);
            writer.WriteString(m_Name, "Name");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");

            m_Start.Save(writer);
            m_End.Save(writer);
        }

        public virtual void Load(IDeserializer reader)
        {
            reader.ReadInt32("InteractivePoint.Version");

            m_ID = reader.ReadInt32("ID");
            m_Name = reader.ReadString("Name");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");

            m_Start.Load(reader);
            m_End.Load(reader);
        }

        private int m_ID;
        private string m_Name;
        private InteractivePointStartCoordinate m_Start = new();
        private InteractivePointEndCoordinate m_End = new();
        private bool m_ShowInInspector = true;
        protected GameObject m_Root;
        private House m_House;
        private const int m_Version = 1;
    }
}
