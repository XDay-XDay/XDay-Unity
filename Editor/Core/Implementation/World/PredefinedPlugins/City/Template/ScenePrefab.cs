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

namespace XDay.WorldAPI.City.Editor
{
    internal interface IScenePrefabSetter
    {
        bool Visible { get; set; }
        GameObject Prefab { get; set; }
        GameObject PrefabInstance { get; }
        Vector3 Position { get; set; }
        Quaternion Rotation { get; set; }
    }

    internal class ScenePrefab
    {
        public bool Visible
        {
            get
            {
                if (m_PrefabInstance != null)
                {
                    return m_PrefabInstance.activeSelf;
                }
                return false;
            }

            set
            {
                if (value != m_PrefabVisible)
                {
                    m_PrefabVisible = value;
                    if (m_PrefabInstance != null)
                    {
                        m_PrefabInstance.SetActive(m_PrefabVisible);
                    }
                }
            }
        }
        public GameObject Prefab
        {
            get => m_Prefab;

            set => SetPrefab(value);
        }
        public GameObject Instance => m_PrefabInstance;
        public Vector3 Position
        {
            get
            {
                if (m_PrefabInstance != null)
                {
                    return m_PrefabInstance.transform.position;
                }
                return Vector3.zero;
            }
            set
            {
                m_Position = value;
                if (m_PrefabInstance != null)
                {
                    m_PrefabInstance.transform.position = value;
                }
            }
        }

        public Vector3 Scale
        {
            get
            {
                if (m_PrefabInstance != null)
                {
                    return m_PrefabInstance.transform.localScale;
                }
                return Vector3.one;
            }
            set
            {
                m_Scale = value;
                if (m_PrefabInstance != null)
                {
                    m_PrefabInstance.transform.localScale = value;
                }
            }
        }

        public Quaternion Rotation
        {
            get
            {
                if (m_PrefabInstance != null)
                {
                    return m_PrefabInstance.transform.rotation;
                }
                return Quaternion.identity;
            }
            set
            {
                m_Rotation = value;
                if (m_PrefabInstance != null)
                {
                    m_PrefabInstance.transform.rotation = value;
                }
            }
        }

        public void Initialize(Transform parent, bool addCollider)
        {
            m_Parent = parent;
            m_AddCollider = addCollider;

            var path = AssetDatabase.GUIDToAssetPath(m_PrefabGUID);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            SetPrefab(prefab);
        }

        public void OnDestroy()
        {
            Object.DestroyImmediate(m_PrefabInstance);
            m_PrefabInstance = null;
        }

        public void Save(ISerializer writer)
        {
            writer.WriteInt32(m_Version, "ScenePrefab.Version");

            var prefabGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_Prefab));
            writer.WriteString(prefabGUID, "GUID");
            writer.WriteBoolean(Visible, "Visible");
            writer.WriteVector3(Position, "Position");
            writer.WriteVector3(Scale, "Scale");
            writer.WriteQuaternion(Rotation, "Rotation");
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("ScenePrefab.Version");

            m_PrefabGUID = reader.ReadString("GUID");
            m_PrefabVisible = reader.ReadBoolean("Visible");
            m_Position = reader.ReadVector3("Position");
            m_Scale = reader.ReadVector3("Scale");
            m_Rotation = reader.ReadQuaternion("Rotation");
        }

        private void SetPrefab(GameObject prefab)
        {
            if (m_Prefab != prefab)
            {
                Object.DestroyImmediate(m_PrefabInstance);
                m_PrefabInstance = null;

                m_Prefab = prefab;

                if (m_Prefab != null)
                {
                    m_PrefabInstance = new GameObject(m_Prefab.name);
                    m_PrefabInstance.AddComponent<SelectionBehaviour>();
                    var prefabInstance = Object.Instantiate(m_Prefab);
                    prefabInstance.transform.SetParent(m_PrefabInstance.transform, true);
                    m_PrefabInstance.SetActive(true);
                    if (m_Position != null)
                    {
                        m_PrefabInstance.transform.position = m_Position.GetValueOrDefault();
                    }
                    if (m_Scale != null)
                    {
                        m_PrefabInstance.transform.localScale = m_Scale.GetValueOrDefault();
                    }
                    if (m_Rotation != null)
                    {
                        m_PrefabInstance.transform.rotation = m_Rotation.GetValueOrDefault();
                    }
                    m_PrefabInstance.transform.SetParent(m_Parent, true);
                    Selection.activeGameObject = m_PrefabInstance;

                    if (m_AddCollider)
                    {
                        AddMeshCollider(m_PrefabInstance);

                        Physics.SyncTransforms();
                    }
                }
            }
        }

        private void AddMeshCollider(GameObject obj)
        {
            if (obj != null)
            {
                var meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    if (obj.GetComponent<MeshCollider>() == null)
                    {
                        var collider = obj.AddComponent<MeshCollider>();
                        collider.sharedMesh = meshFilter.sharedMesh;
                    }
                    obj.layer = LayerMask.NameToLayer("Mask Object");
                }

                for (var i = 0; i < obj.transform.childCount; i++)
                {
                    AddMeshCollider(obj.transform.GetChild(i).gameObject);
                }
            }
        }

        private GameObject m_Prefab;
        private GameObject m_PrefabInstance;

        [SerializeField]
        private bool m_PrefabVisible = true;

        [SerializeField]
        private string m_PrefabGUID;

        [SerializeField]
        private Vector3? m_Position;
        [SerializeField]
        private Vector3? m_Scale;
        [SerializeField]
        private Quaternion? m_Rotation;

        private Transform m_Parent;

        private const int m_Version = 1;

        private bool m_AddCollider;
    }
}
