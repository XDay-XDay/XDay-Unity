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

using XDay.SerializationAPI;
using XDay.UtilityAPI;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    internal partial class ResourceGroup : ISerializable, IResourceGroup
    {
        public string Name { get => m_Name; set => m_Name = value; }
        public int Count => m_Resources.Count;
        public int SelectedIndex => m_SelectedIndex;
        public string SelectedPath => m_SelectedIndex == -1 ? null : m_Resources[m_SelectedIndex].Path;
        public IResourceData SelectedData => m_SelectedIndex == -1 ? null : m_Resources[m_SelectedIndex].Data;
        public GameObject SelectedPrefab => m_SelectedIndex == -1 ? null : AssetDatabase.LoadAssetAtPath<GameObject>(m_Resources[m_SelectedIndex].Path);
        
        public string RandomPath
        {
            get
            {
                if (m_Resources.Count > 0)
                {
                    var validPaths = new List<string>();
                    foreach (var model in m_Resources)
                    {
                        if (model != null)
                        {
                            validPaths.Add(model.Path);
                        }
                    }

                    if (validPaths.Count > 0)
                    {
                        var idx = UnityEngine.Random.Range(0, validPaths.Count);
                        return validPaths[idx];
                    }
                }
                return null;
            }
        }
        public string TypeName => "ResourceGroup";

        public class EventCallbacks
        {
            public Action<GameObject> OnResourceSelected;
            public Action<int, GameObject, int> OnResourceChanged;
            public Action<int, string, int> OnResourceRemoved;
            public Action<int, GameObject> OnResourceAdded;
        }

        public ResourceGroup()
        {
        }

        public ResourceGroup(string name)
        {
            m_Name = name;
        }

        public void Init(ResourceGroupSystem groupSystem, Func<IResourceData> dataCreator, EventCallbacks callbacks)
        {
            m_GroupSystem = groupSystem;
            m_ResourceDataCreator = dataCreator;
            m_EventCallbacks = callbacks;

            for (var i = 0; i < m_Resources.Count; i++)
            {
                m_Resources[i].Init();
            }
        }

        public void Uninit()
        {
            OnUnselected();

            foreach (var resource in m_Resources)
            {
                resource?.Uninit();
            }
            m_Resources.Clear();
        }

        public void Reset()
        {
            Uninit();
        }

        public string QueryPath(string name)
        {
            foreach (var resource in m_Resources)
            {
                if (resource != null)
                {
                    if (string.Equals(name, resource.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return resource.Path;
                    }
                }
            }
            return null;
        }

        public void SelectRandom()
        {
            if (Count > 0)
            {
                var idx = UnityEngine.Random.Range(0, Count);
                SetSelection(idx);
            }
        }

        public int QueryResourceIndex(string prefabPath)
        {
            for (var i = 0; i < m_Resources.Count; i++)
            {
                if (m_Resources[i] != null &&
                    m_Resources[i].Path == prefabPath)
                {
                    return i;
                }
            }
            return -1;
        }

        public Resource GetResource(int index)
        {
            if (index >= 0 && index < m_Resources.Count)
            {
                return m_Resources[index];
            }
            return null;
        }

        public void RemoveEmptyResource()
        {
            for (var i = m_Resources.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(GetResourcePath(i)))
                {
                    Remove(i);
                }
            }
        }

        public bool Add(string prefabPath, bool checkTransform, bool showError)
        {
            if (QueryResourceIndex(prefabPath) < 0)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    m_Resources.Add(new Resource(prefabPath, null));
                }
                else
                {
                    if (checkTransform &&
                        !EditorHelper.IsIdentity(prefab))
                    {
                        EditorUtility.DisplayDialog("Error", $"prefab transform is not identity", "OK");
                        return false;
                    }

                    var resource = new Resource(prefabPath, m_ResourceDataCreator?.Invoke());
                    resource.Init();
                    m_Resources.Add(resource);

                    SetSelection(m_Resources.Count - 1);

                    m_EventCallbacks?.OnResourceAdded(m_GroupSystem.QueryGroupIndex(this), prefab);
                }

                return true;
            }

            if (showError)
            {
                EditorUtility.DisplayDialog("Error", "prefab already added!", "OK");
            }
            return false;
        }

        public void Remove(int index)
        {
            if (index >= 0 && index < m_Resources.Count)
            {
                if (m_Resources[index] != null)
                {
                    m_EventCallbacks.OnResourceRemoved?.Invoke(m_GroupSystem.QueryGroupIndex(this), m_Resources[index].Path, index);
                }

                m_Resources.RemoveAt(index);

                if (m_Resources.Count == 0)
                {
                    SetSelection(-1);
                }
                else
                {
                    SetSelection(Mathf.Clamp(index, 0, m_Resources.Count - 1));
                }
            }
        }

        public string GetResourcePath(int index)
        {
            if (index >= 0 && index < m_Resources.Count && m_Resources[index] != null)
            {
                return m_Resources[index].Path;
            }
            return "";
        }

        public GameObject GetPrefab(int index)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(GetResourcePath(index));
        }

        public void OnSelected()
        {
            if (m_Resources.Count > 1)
            {
                SetSelection(1);
            }
        }

        public void OnUnselected()
        {
            m_SelectedPrefab = null;
            SetSelection(-1);
        }

        private void SetSelection(int index)
        {
            m_SelectedIndex = index;
            GameObject prefab = null;
            if (index >= 0 && 
                m_Resources[index] != null)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_Resources[index].Path);
            }
            if (prefab != null)
            {
                m_EventCallbacks.OnResourceSelected?.Invoke(prefab);
            }
        }

        private void ChangeIndex(int oldIndex, int newIndex)
        {
            m_DrawResource = false;

            newIndex = Mathf.Min(newIndex, m_Resources.Count - 1);
            var resource = m_Resources[oldIndex];
            if (newIndex <= oldIndex)
            {
                m_Resources.Insert(newIndex, resource);
                m_Resources.RemoveAt(oldIndex + 1);
            }
            else
            {
                m_Resources.Insert(newIndex + 1, resource);
                m_Resources.RemoveAt(oldIndex);
            }
        }

        private void ClearRemoveQueue()
        {
            for (var i = m_RemoveQueue.Count - 1; i >= 0; --i)
            {
                Remove(m_RemoveQueue[i]);
            }
            m_RemoveQueue.Clear();
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "ResourceGroup.Version");
            serializer.WriteString(m_Name, "Name");
            serializer.WriteInt32(m_SelectedIndex, "Selected Index");
            serializer.WriteList(m_Resources, "Resources", (model, index) => {
                serializer.WriteSerializable(model, $"Resource {index}", converter, false);
            });
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("ResourceGroup.Version");
            m_Name = deserializer.ReadString("Name");
            m_SelectedIndex = deserializer.ReadInt32("Selected Index");
            m_Resources = deserializer.ReadList("Resources", (index) => {
                return deserializer.ReadSerializable<Resource>($"Resource {index}", false);
            });
        }

        private string m_Name;
        private GameObject m_SelectedPrefab;
        private EventCallbacks m_EventCallbacks;
        private ResourceGroupSystem m_GroupSystem;
        private int m_SelectedIndex = -1;
        private List<Resource> m_Resources = new();
        private List<int> m_RemoveQueue = new();
        private Func<IResourceData> m_ResourceDataCreator;
        private const int m_Version = 1;
    }
}

//XDay