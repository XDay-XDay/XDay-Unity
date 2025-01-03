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
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    internal partial class ResourceGroup
    {
        public class Resource : ISerializable
        {
            public string Name => m_Name;
            public IResourceData Data => m_Data;
            public string Path => AssetDatabase.GUIDToAssetPath(m_GUID);
            public GameObject Prefab => m_Prefab;
            public Texture2D Icon
            {
                get
                {
                    CreateIcon();
                    return m_Icon;
                }
                set => m_Icon = value;
            }
            public string TypeName => "ResourceGroup.Resource";

            public Resource()
            {
            }

            public Resource(string path, IResourceData data)
            {                
                m_Name = Helper.GetPathName(path, false);
                m_Data = data;
                m_GUID = AssetDatabase.AssetPathToGUID(path);
            }

            public void Init()
            {
                m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
                m_Name = Helper.GetPathName(Path, false);
            }

            public void Uninit()
            {
                Helper.DestroyUnityObject(m_Icon);
                m_Icon = null;
            }

            public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
            {
                serializer.WriteInt32(m_Version, "Resource.Version");
                serializer.WriteString(m_GUID, "GUID");
                serializer.WriteSerializable(m_Data, "Data", converter, false);
            }

            public void EditorDeserialize(IDeserializer deserializer, string label)
            {
                deserializer.ReadInt32("Resource.Version");
                m_GUID = deserializer.ReadString("GUID");
                m_Data = deserializer.ReadSerializable<IResourceData>("Data", false);
            }

            private void CreateIcon()
            {
                if (m_Icon == null)
                {
                    var preview = AssetPreview.GetAssetPreview(m_Prefab);
                    if (preview != null)
                    {
                        m_Icon = new Texture2D(preview.width, preview.height, preview.format, false);
                        m_Icon.SetPixels32(preview.GetPixels32());
                        m_Icon.Apply();
                    }
                }
            }

            private string m_Name;
            private string m_GUID;
            private IResourceData m_Data;
            private Texture2D m_Icon;
            private GameObject m_Prefab;
            private const int m_Version = 1;
        }
    }
}

//XDay