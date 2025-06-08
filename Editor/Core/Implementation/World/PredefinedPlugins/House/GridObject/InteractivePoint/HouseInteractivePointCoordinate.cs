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
using XDay.WorldAPI.City.Editor;

namespace XDay.WorldAPI.House.Editor
{
    internal class HouseInteractivePointCoordinate : IScenePrefabSetter
    {
        public GameObject Prefab { get => m_Prefab.Prefab; set => m_Prefab.Prefab = value; }
        public GameObject PrefabInstance => m_Prefab.Instance;
        public bool Visible { get => m_Prefab.Visible; set => m_Prefab.Visible = value; }
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public Vector3 Position
        {
            get
            {
                if (PrefabInstance != null)
                {
                    return PrefabInstance.transform.position;
                }
                return Vector3.zero;
            }
            set
            {
                if (PrefabInstance != null)
                {
                    PrefabInstance.transform.position = value;
                }
            }
        }

        public Quaternion Rotation
        {
            get
            {
                if (PrefabInstance != null)
                {
                    return PrefabInstance.transform.rotation;
                }
                return Quaternion.identity;
            }
            set
            {
                if (PrefabInstance != null)
                {
                    PrefabInstance.transform.rotation = value;
                }
            }
        }

        public void Initialize(Transform parent)
        {
            m_Prefab.Initialize(parent, false);
        }

        public void OnDestroy()
        {
            m_Prefab.OnDestroy();
        }

        public virtual void Save(ISerializer writer)
        {
            writer.WriteInt32(m_Version, "InteractivePointCoordinate.Version");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");

            m_Prefab.Save(writer);
        }

        public virtual void Load(IDeserializer reader)
        {
            reader.ReadInt32("InteractivePointCoordinate.Version");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");

            m_Prefab.Load(reader);
        }

        private ScenePrefab m_Prefab = new();
        private bool m_ShowInInspector = true;
        private const int m_Version = 1;
    }

    internal class InteractivePointStartCoordinate : InteractivePointCoordinate
    {
    }

    internal class InteractivePointEndCoordinate : InteractivePointCoordinate
    {
    }
}
