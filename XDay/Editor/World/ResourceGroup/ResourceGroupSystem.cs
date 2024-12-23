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



using XDay.SerializationAPI;
using XDay.UtilityAPI.Editor;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    public delegate void SelectGroup(int oldGroupIndex, int newGroupIndex);
    public delegate void SelectResource(GameObject prefab);
    public delegate void SetResource(int groupIndex, GameObject prefab, int index);
    public delegate void AddResource(int groupIndex, GameObject prefab);
    public delegate void RemoveResource(int groupIndex, string prefabPath, int index);

    internal partial class ResourceGroupSystem : ISerializable, IResourceGroupSystem
    {
        public event SelectGroup EventSelectGroup;
        public event SelectResource EventSelectResource;
        public event SetResource EventSetResource;
        public event AddResource EventAddResource;
        public event RemoveResource EventRemoveResource;
        public string[] GroupNames => m_GroupNames;
        public int GroupCount => m_Groups.Count;
        public bool CheckTransform => m_CheckTransform;
        public int SelectedGroupIndex => m_SelectedGroupIndex;
        public string RandomResourcePath => SelectedGroup?.RandomPath;
        public string SelectedResourcePath => SelectedGroup?.SelectedPath;
        public GameObject SelectedPrefab => SelectedGroup?.SelectedPrefab;
        public IResourceGroup SelectedGroup
        {
            get
            {
                if (m_SelectedGroupIndex >= 0 && m_SelectedGroupIndex < m_Groups.Count)
                {
                    return m_Groups[m_SelectedGroupIndex];
                }
                return null;
            }
        }
        public IResourceData SelectedData => SelectedGroup?.SelectedData;
        public string TypeName => "ResourceGroupSystem";

        public ResourceGroupSystem()
        {
        }

        public ResourceGroupSystem(bool checkTransform)
        {
            m_CheckTransform = checkTransform;

            AddGroup("Group");
        }

        public void Init(Func<IResourceData> creator)
        {
            m_ResourceDataCreator = creator;

            var callbacks = CreateEventCallbacks();
            foreach (var group in m_Groups)
            {
                group.Init(this, creator, callbacks);
            }
        }

        public void Uninit()
        {
            for (var i = 0; i < m_Groups.Count; i++)
            {
                m_Groups[i].Uninit();
            }
            m_Groups.Clear();

            m_SelectedGroupIndex = -1;
            m_GroupNames = new string[0];
        }

        private ResourceGroup QueryGroup(string name)
        {
            foreach (var group in m_Groups)
            {
                if (group.Name == name)
                {
                    return group;
                }
            }
            return null;
        }

        public string QueryPath(string name)
        {
            foreach (var group in m_Groups)
            {
                var path = group.QueryPath(name);
                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }
            return null;
        }

        public void RemoveEmptyResource()
        {
            for (var i = 0; i < m_Groups.Count; i++)
            {
                m_Groups[i].RemoveEmptyResource();
            }
        }

        public void RemoveGroup(int index)
        {
            if (index >= 0 && index < m_Groups.Count)
            {
                m_Groups[index].Uninit();
                m_Groups.RemoveAt(index);

                int selectedIndex = m_Groups.Count == 0 ? -1 : Mathf.Clamp(index - 1, 0, m_Groups.Count);
                SetSelectedGroup(selectedIndex);
            }
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "ResourceGroupSystem.Version");
            serializer.WriteBoolean(m_CheckTransform, "Check Transform");
            serializer.WriteInt32(m_SelectedGroupIndex, "Selected Group");
            serializer.WriteList(m_Groups, "Groups", (group, index) =>
            {
                serializer.WriteSerializable(group, $"Group {index}", converter, false);
            });
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("ResourceGroupSystem.Version");
            m_CheckTransform = deserializer.ReadBoolean("Check Transform");
            m_SelectedGroupIndex = deserializer.ReadInt32("Selected Group");
            m_Groups = deserializer.ReadList("Groups", (index) =>
            {
                return deserializer.ReadSerializable<ResourceGroup>($"Group {index}", false);
            });
        }

        internal int QueryGroupIndex(ResourceGroup group)
        {
            return m_Groups.IndexOf(group);
        }

        internal ResourceGroup GetGroup(int index)
        {
            if (index >= 0 && index < m_Groups.Count)
            {
                return m_Groups[index];
            }
            return null;
        }

        private void SetSelectedGroup(int index)
        {
            var old = m_SelectedGroupIndex;
            var oldSelection = SelectedGroup as ResourceGroup;
            oldSelection?.OnUnselected();

            m_SelectedGroupIndex = index;

            var newSelection = SelectedGroup as ResourceGroup;
            newSelection?.OnSelected();

            EventSelectGroup?.Invoke(old, index);
        }

        private ResourceGroup.EventCallbacks CreateEventCallbacks()
        {
            var events = new ResourceGroup.EventCallbacks
            {
                OnResourceSelected = (prefab) => { EventSelectResource?.Invoke(prefab); },
                OnResourceChanged = (groupIndex, prefab, index) => { EventSetResource?.Invoke(groupIndex, prefab, index); },
                OnResourceAdded = (groupIndex, prefab) => { EventAddResource?.Invoke(groupIndex, prefab); },
                OnResourceRemoved = (groupIndex, path, index) => { EventRemoveResource?.Invoke(groupIndex, path, index); },
            };
            return events;
        }

        private ResourceGroup AddGroup(string name)
        {
            if (QueryGroup(name) == null)
            {
                var group = new ResourceGroup(name);
                group.Init(this, m_ResourceDataCreator, CreateEventCallbacks());
                m_Groups.Add(group);

                SetSelectedGroup(m_Groups.Count - 1);
                return group;
            }
            return null;
        }

        private void ChangeGroupName(int index)
        {
            if (index >= 0)
            {
                var group = m_Groups[index];

                var parameters = new List<ParameterWindow.Parameter>()
                {
                    new ParameterWindow.StringParameter("New Name", "", group.Name),
                };
                ParameterWindow.Open("Rename", parameters, (p) =>
                {
                    var ok = ParameterWindow.GetString(p[0], out var name);
                    if (ok)
                    {
                        if (SelectedGroup != null)
                        {
                            (SelectedGroup as ResourceGroup).Name = name;
                            return true;
                        }
                    }
                    return false;
                });
            }
        }

        private int m_SelectedGroupIndex = -1;
        private Func<IResourceData> m_ResourceDataCreator;
        private List<ResourceGroup> m_Groups = new();
        private string[] m_GroupNames = new string[0];
        private bool m_ShowGroup = true;
        private bool m_CheckTransform;
        private const int m_Version = 1;
    }
}


//XDay