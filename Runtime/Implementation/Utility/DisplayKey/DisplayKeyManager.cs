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

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace XDay.DisplayKeyAPI
{
    public class DisplayKeyManager
    {
        public List<DisplayKeyGroup> Groups => m_Groups;
        public List<DisplayKey> AllKeys => m_AllKeys;
        public int KeyCount => m_AllKeys.Count;
        public string OutputFolder { get => m_OutputFolder; set => m_OutputFolder = value; }
        public static DisplayKeyManager Instance => m_Instance;

        public DisplayKeyManager()
        {
            if (m_Instance != null)
            {
                Debug.LogError("DisplayKeyManager只能有一个实例");
            }
            m_Instance = this;
        }

        public void OnDestroy()
        {
            m_Instance = null;
        }

        public bool IsValidKey(int id)
        {
            foreach (var key in m_AllKeys)
            {
                if (key.ID == id) {  return true; }
            }
            return false;
        }

        public int GetIndex(int id)
        {
            var idx = 0;
            foreach (var key in m_AllKeys)
            {
                if (key.ID == id)
                {
                    return idx;
                }
                ++idx;
            }
            return -1;
        }

        public DisplayKey GetKey(int id)
        {
            foreach (var key in m_AllKeys)
            {
                if(key.ID == id)
                {
                    return key;
                }
            }
            Log.Instance?.Error($"display key {id} not found!");
            return null;
        }

        public DisplayKey GetKeyByIndex(int index)
        {
            if (index >= 0 && index < m_AllKeys.Count)
            {
                return m_AllKeys[index];
            }
            return null;
        }

        public DisplayKeyGroup CreateGroup(string name)
        {
            var group = new DisplayKeyGroup(this, name);
            m_Groups.Add(group);
            return group;
        }

        public void RemoveGroup(DisplayKeyGroup group)
        {
            foreach (var key in group.Keys)
            {
                m_AllKeys.Remove(key);
            }

            m_Groups.Remove(group);
        }
        
#if UNITY_EDITOR
        public void Save(string dataPath)
        {
            var serializer = ISerializer.CreateFile(ISerializer.CreateBinary(), dataPath);

            serializer.WriteInt32(m_EditorVersion, "Version");

            serializer.WriteString(m_OutputFolder, "Output Folder");

            serializer.WriteList(m_Groups, "Display Key Groups", (group, index) => {
                serializer.WriteStructure($"Display Key Group {index}", () => {
                    group.Save(serializer);
                });
            });

            serializer.Uninit();
        }

        public void Load(string dataPath)
        {
            var deserializer = IDeserializer.CreateBinary(new FileStream(dataPath, FileMode.Open), null);

            var version = deserializer.ReadInt32("Version");

            if (version >= 2)
            {
                m_OutputFolder = deserializer.ReadString("Output Folder");
            }

            m_Groups = deserializer.ReadList("Display Key Groups", (index) => {
                var group = new DisplayKeyGroup(this);
                deserializer.ReadStructure($"Display Key Group {index}", () => {
                    group.Load(deserializer);
                });
                return group;
            });

            foreach (var group in m_Groups)
            {
                foreach(var key in group.Keys)
                {
                    AddKey(key);
                }
            }

            deserializer.Uninit();
        }

        public void Export(string dataPath)
        {
            var serializer = ISerializer.CreateFile(ISerializer.CreateBinary(), dataPath);

            serializer.WriteInt32(m_RuntimeVersion, "Version");

            serializer.WriteList(m_Groups, "Display Key Groups", (group, index) => {
                serializer.WriteStructure($"Display Key Group {index}", () => {
                    group.Export(serializer);
                });
            });

            serializer.Uninit();
        }
#endif

        public void Import(Stream stream)
        {
            var deserializer = IDeserializer.CreateBinary(stream, null);

            deserializer.ReadInt32("Version");

            m_Groups = deserializer.ReadList("Display Key Groups", (index) => {
                var group = new DisplayKeyGroup(this);
                deserializer.ReadStructure($"Display Key Group {index}", () => {
                    group.Import(deserializer);
                });
                return group;
            });

            foreach (var group in m_Groups)
            {
                foreach (var key in group.Keys)
                {
                    AddKey(key);
                }
            }

            deserializer.Uninit();
        }

        internal void AddKey(DisplayKey key)
        {
            m_AllKeys.Add(key);
        }

        internal void SetCustomDataTranslator(Func<string, string> translator)
        {
            m_CustomDataTranslator = translator;
        }

        internal Func<string, string> GetCustomDataTranslator()
        {
            return m_CustomDataTranslator;
        }

        private List<DisplayKeyGroup> m_Groups = new();
        private readonly List<DisplayKey> m_AllKeys = new();
        private string m_OutputFolder;
        private const int m_EditorVersion = 2;
        private const int m_RuntimeVersion = 1;
        private Func<string, string> m_CustomDataTranslator;
        private static DisplayKeyManager m_Instance;
    }

    public static class DisplayKeyManagerExtension
    {
        public static string GetIconPath(this int id)
        {
            if (DisplayKeyManager.Instance == null)
            {
                return "";
            }

            var key = DisplayKeyManager.Instance.GetKey(id);
            if (key == null)
            {
                return null;
            }
            return key.IconPath;
        }

        public static string GetPrefabPath(this int id)
        {
            if (DisplayKeyManager.Instance == null)
            {
                return "";
            }

            var key = DisplayKeyManager.Instance.GetKey(id);
            if (key == null)
            {
                return null;
            }
            return key.PrefabPath;
        }

        public static string GetAudioClipPath(this int id)
        {
            if (DisplayKeyManager.Instance == null)
            {
                return "";
            }

            var key = DisplayKeyManager.Instance.GetKey(id);
            if (key == null)
            {
                return null;
            }
            return key.AudioClipPath;
        }

        public static string GetCustomData(this int id)
        {
            if (DisplayKeyManager.Instance == null)
            {
                return "";
            }

            var key = DisplayKeyManager.Instance.GetKey(id);
            if (key == null)
            {
                return null;
            }
            return key.CustomData;
        }
    }
}
