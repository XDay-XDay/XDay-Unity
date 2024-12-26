/*
 * Copyright (c) 2024 XDay
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



using XDay.UtilityAPI;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    internal class MeshIndicator : IMeshIndicator
    {
        public string Prefab
        {
            set
            {
                if (m_Path != value)
                {
                    if (m_Mesh != null)
                    {
                        m_World.GameObjectPool.Release(m_Path, m_Mesh);
                    }

                    m_Path = value;

                    m_Mesh = m_World.GameObjectPool.Get(m_Path);
                    m_Mesh.name = $"Mesh Indicator[{m_Path}]";
                    Helper.HideGameObject(m_Mesh, true);
                }
            }
        }
        public Vector3 Position { get => m_Mesh.transform.position; set => m_Mesh.transform.position = value; }
        public Quaternion Rotation { get => m_Mesh.transform.rotation; set => m_Mesh.transform.rotation = value; }
        public bool Visible
        {
            get => m_Mesh == null ? false : m_Mesh.activeInHierarchy;
            set
            {
                if (m_Mesh != null)
                {
                    m_Mesh.SetActive(value);
                }
            }
        }

        public MeshIndicator(IWorld world)
        {
            m_World = world;
        }

        public void OnDestroy()
        {
            if (m_Mesh != null)
            {
                Helper.HideGameObject(m_Mesh, false);
                m_World.GameObjectPool.Release(m_Path, m_Mesh);
                m_Mesh = null;
            }
        }

        private GameObject m_Mesh;
        private string m_Path;
        private IWorld m_World;
    }
}

//XDay