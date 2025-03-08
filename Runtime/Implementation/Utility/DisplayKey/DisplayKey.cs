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
using UnityEngine;

namespace XDay.UtilityAPI
{
    public class DisplayKeyParam
    {
        public INamedAspect Aspect;
        public Type UnityObjectType;

        public DisplayKeyParam Clone()
        {
            DisplayKeyParam p = new DisplayKeyParam()
            {
                Aspect = Aspect.Clone(),
                UnityObjectType = UnityObjectType,
            };
            if (UnityObjectType != null)
            {
                p.Aspect.Value.SetObject(Aspect.Value.GetObject());
            }
            return p;
        }

#if UNITY_EDITOR
        public void Save(ISerializer serializer)
        {
            serializer.WriteInt32(m_EditorVersion, "Version");

            Aspect.Serialize(serializer);

            string objectGUID = null;
            if (UnityObjectType != null)
            {
                var obj = Aspect.Value.GetObject() as UnityEngine.Object;
                objectGUID = Helper.GetObjectGUID(obj);
            }

            serializer.WriteString(objectGUID, "Object GUID");
            var typename = UnityObjectType == null ? "" : UnityObjectType.Name;
            serializer.WriteString(typename, "Type Name");
        }

        public void Load(IDeserializer deserializer)
        {
            deserializer.ReadInt32("Version");

            Aspect = INamedAspect.Create();
            Aspect.Deserialize(deserializer);
            var objectGUID = deserializer.ReadString("Object GUID");
            var typename = deserializer.ReadString("Type Name");
            UnityObjectType = Helper.SearchTypeByName(typename);
            if (UnityObjectType != null)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(objectGUID);
                var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                Aspect.Value.SetObject(obj);
            }
        }

        public void Export(ISerializer serializer)
        {
            serializer.WriteInt32(m_RuntimeVersion, "Version");

            Aspect.Serialize(serializer);
        }
#endif

        public void Import(IDeserializer deserializer)
        {
            deserializer.ReadInt32("Version");

            Aspect = INamedAspect.Create();
            Aspect.Deserialize(deserializer);
        }

        private const int m_EditorVersion = 1;
        private const int m_RuntimeVersion = 1;
    }

    public class DisplayKey
    {
        public int ID { get;set; }
        public string Name { get; set; } = "";
        //keep guid in editor, path in runtime
        public string IconPath { get; set; } = "";
        public string PrefabPath { get; set; } = "";
        public string AudioClipPath { get; set; } = "";
        public bool Foldout { get; set; } = false;
        public int ParameterCount => m_Parameters == null ? 0 : m_Parameters.Count;

        public void AddString(string name, string value)
        {
            var aspect = INamedAspect.Create(IAspect.FromString(value), name);
            AddParameter(aspect, null);
        }

        public void AddSingle(string name, float value)
        {
            var aspect = INamedAspect.Create(IAspect.FromSingle(value), name);
            AddParameter(aspect, null);
        }

        public void AddInt32(string name, int value)
        {
            var aspect = INamedAspect.Create(IAspect.FromInt32(value), name);
            AddParameter(aspect, null);
        }

        public void AddBoolean(string name, bool value)
        {
            var aspect = INamedAspect.Create(IAspect.FromBoolean(value), name);
            AddParameter(aspect, null);
        }

        public void AddVector2(string name, Vector2 value)
        {
            var aspect = INamedAspect.Create(IAspect.FromVector2(value), name);
            AddParameter(aspect, null);
        }
        
        public void AddVector3(string name, Vector3 value)
        {
            var aspect = INamedAspect.Create(IAspect.FromVector3(value), name);
            AddParameter(aspect, null);
        }
        
        public void AddVector4(string name, Vector4 value)
        {
            var aspect = INamedAspect.Create(IAspect.FromVector4(value), name);
            AddParameter(aspect, null);
        }

        public void AddColor(string name, Color value)
        {
            var aspect = INamedAspect.Create(IAspect.FromColor(value), name);
            AddParameter(aspect, null);
        }

        public void AddObject(string name, UnityEngine.Object obj, Type unityObjectType)
        {
            var aspect = INamedAspect.Create(IAspect.FromObject(obj), name);
            AddParameter(aspect, unityObjectType);
        }

        public void RemoveParameter(int index)
        {
            if (m_Parameters != null)
            {
                if (index >= 0 && index < m_Parameters.Count)
                {
                    m_Parameters.RemoveAt(index);
                }
            }
        }

        public DisplayKeyParam GetParameter(int index)
        {
            if (m_Parameters != null)
            {
                if (index >= 0 && index < m_Parameters.Count)
                {
                    return m_Parameters[index];
                }
            }
            return null;
        }

        public DisplayKey Clone()
        {
            var newKey = new DisplayKey() {
                Name = Name,
                ID = ID + 1,
                Foldout = false,
                IconPath = IconPath,
                PrefabPath = PrefabPath,
                AudioClipPath = AudioClipPath,
                m_Parameters = CloneParameters(),
            };
            return newKey;
        }

#if UNITY_EDITOR
        public void Save(ISerializer serializer)
        {
            serializer.WriteInt32(m_EditorVersion, "Version");

            serializer.WriteInt32(ID, "ID");
            serializer.WriteString(Name, "Name");
            serializer.WriteString(IconPath, "IconPath");
            serializer.WriteString(PrefabPath, "PrefabPath");
            serializer.WriteString(AudioClipPath, "AudioClipPath");
            serializer.WriteBoolean(Foldout, "Foldout");

            serializer.WriteList(m_Parameters, "Parameters", (param, index) => {
                serializer.WriteStructure($"Parameter {index}", () => {
                    param.Save(serializer);
                });
            });
        }

        public void Load(IDeserializer deserializer)
        {
            var version = deserializer.ReadInt32("Version");

            ID = deserializer.ReadInt32("ID");
            Name = deserializer.ReadString("Name");
            IconPath = deserializer.ReadString("IconPath");
            PrefabPath = deserializer.ReadString("PrefabPath");
            if (version >= 2)
            {
                AudioClipPath = deserializer.ReadString("AudioClipPath");
            }
            Foldout = deserializer.ReadBoolean("Foldout");

            m_Parameters = deserializer.ReadList("Parameters", (index) => {
                var param = new DisplayKeyParam();
                deserializer.ReadStructure($"Parameter {index}", () => {
                    param.Load(deserializer);
                });
                return param;
            });
        }

        public void Export(ISerializer serializer)
        {
            serializer.WriteInt32(m_RuntimeVersion, "Version");

            serializer.WriteInt32(ID, "ID");
            serializer.WriteString(Name, "Name");
            serializer.WriteString(UnityEditor.AssetDatabase.GUIDToAssetPath(IconPath), "IconPath");
            serializer.WriteString(UnityEditor.AssetDatabase.GUIDToAssetPath(PrefabPath), "PrefabPath");
            serializer.WriteString(UnityEditor.AssetDatabase.GUIDToAssetPath(AudioClipPath), "AudioClipPath");

            serializer.WriteList(m_Parameters, "Parameters", (param, index) => {
                serializer.WriteStructure($"Parameter {index}", () => {
                    param.Export(serializer);
                });
            });
        }
#endif

        public void Import(IDeserializer deserializer)
        {
            deserializer.ReadInt32("Version");

            ID = deserializer.ReadInt32("ID");
            Name = deserializer.ReadString("Name");
            IconPath = deserializer.ReadString("IconPath");
            PrefabPath = deserializer.ReadString("PrefabPath");
            AudioClipPath = deserializer.ReadString("AudioClipPath");

            m_Parameters = deserializer.ReadList("Parameters", (index) => {
                var param = new DisplayKeyParam();
                deserializer.ReadStructure($"Parameter {index}", () => {
                    param.Import(deserializer);
                });
                return param;
            });
        }

        private void AddParameter(INamedAspect aspect, Type unityObjectType)
        {
            m_Parameters ??= new();
            m_Parameters.Add(new DisplayKeyParam() { Aspect = aspect, UnityObjectType = unityObjectType });
        }

        private List<DisplayKeyParam> CloneParameters()
        {
            if (m_Parameters == null)
            {
                return null;
            }
            var parameters = new List<DisplayKeyParam>(m_Parameters.Count);
            for (var i = 0; i < m_Parameters.Count; i++)
            {
                parameters.Add(m_Parameters[i].Clone());
            }
            return parameters;
        }

        private List<DisplayKeyParam> m_Parameters;
        private const int m_EditorVersion = 2;
        private const int m_RuntimeVersion = 1;
    }
}