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
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.City.Editor
{
    interface ILocatorOwner
    {
        void AddLocator(Locator locator);
        void RemoveLocator(int index);

        bool ShowLocatorInInspector { get; set; }
        List<Locator> Locators { get; }
        int SelectedLocatorIndex { get; set; }
    }

    class Locator
    {
        public Locator()
        {
        }

        public Locator(string name, Vector3 position, Vector3 size)
        {
            m_Name = name;
            m_Position = position;
            m_Size = size;
        }

        public void Initialize(Transform parent, int gridID)
        {
            m_GameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_GameObject.transform.SetParent(parent);
            m_GameObject.transform.position = m_Position;
            m_GameObject.transform.localScale = m_Size;
            m_GameObject.name = m_Name;
            m_GameObject.AddComponent<NoKeyDeletion>();
            var behaviour = m_GameObject.AddComponent<LocatorBehaviour>();
            behaviour.Initialize(gridID, (e) => { WorldEditor.EventSystem.Broadcast(e); });
            var renderer = m_GameObject.GetComponent<MeshRenderer>();
            m_Material = new Material(Shader.Find("Unlit/Color"));
            m_Material.SetColor("_MainColor", Color.yellow);
            renderer.sharedMaterial = m_Material;
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_GameObject);
            m_GameObject = null;

            Helper.DestroyUnityObject(m_Material);
            m_Material = null;
        }

        public void Save(ISerializer writer)
        {
            writer.WriteInt32(m_Version, "Locator.Version");

            writer.WriteString(Name, "Name");
            writer.WriteVector3(Position, "Position");
            writer.WriteVector3(Size, "Size");
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("Locator.Version");

            m_Name = reader.ReadString("Name");
            m_Position = reader.ReadVector3("Position");
            m_Size = reader.ReadVector3("Size");
        }

        public string Name { get => m_GameObject.name; set { m_Name = value; m_GameObject.name = value; } }
        public Vector3 Position
        {
            get => m_GameObject.transform.position;
            set
            {
                m_Position = value;
                if (m_GameObject != null)
                {
                    m_GameObject.transform.position = value;
                }
            }
        }
        public Vector3 Size
        {
            get => m_GameObject.transform.localScale;
            set
            {
                m_Size = value;
                if (m_GameObject != null)
                {
                    m_GameObject.transform.localScale = value;
                }
            }
        }
        public GameObject GameObject => m_GameObject;

        GameObject m_GameObject;
        string m_Name;
        Vector3 m_Position;
        Vector3 m_Size;
        Material m_Material;
        const int m_Version = 1;
    }
}
