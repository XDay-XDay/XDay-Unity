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
using System;
using System.Text.RegularExpressions;
using System.IO;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Editor
{
    [XDaySerializableClass("Editor Resource Descriptor")]
    internal class EditorResourceDescriptor : ResourceDescriptor, IEditorResourceDescriptor
    {
        public string PathPrefix => m_Prefix;
        public GameObject Prefab => m_Prefab;
        public Rect Bounds { get => m_Bounds; set => m_Bounds = value; }
        public override bool IsValid => m_Prefab != null;
        public override string TypeName => "EditorResourceDescriptor";

        public EditorResourceDescriptor()
        {
        }

        public EditorResourceDescriptor(int id, int index, string path)
            : base(id, index, path)
        {
            m_LOD0GUID = AssetDatabase.AssetPathToGUID(m_LOD0);
        }

        protected override void OnInit()
        {
            m_LOD0 = AssetDatabase.GUIDToAssetPath(m_LOD0GUID);
            if (string.IsNullOrEmpty(m_LOD0))
            {
                Debug.LogError($"guid {m_LOD0GUID} path is null");
            }
            m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_LOD0);
            if (m_Prefab == null)
            {
                Debug.LogError($"加载Prefab失败{m_LOD0}");
                m_Prefab = new GameObject("Placeholder: " + m_LOD0);
            }
            m_Bounds = m_Prefab.QueryRect();

            FoundLODs();

            CreateLODGroup();
        }

        private string GetPrefabPath(int lod)
        {
            if (lod < 0 || m_LODs == null)
            {
                return m_LOD0;
            }

            if (lod >= m_LODs.Count)
            {
                lod = m_LODs.Count - 1;
            }

            return $"{m_LOD0}{m_LODs[lod]}.prefab";
        }

        private void FoundLODs()
        {
            var lodPos = m_LOD0.IndexOf(WorldDefine.LOD_KEYWORD, StringComparison.OrdinalIgnoreCase);
            if (lodPos == -1)
            {
                return;
            }

            var lodText = m_LOD0.Substring(lodPos);
            lodText = Helper.GetPathName(lodText, false);
            var regexExp = new Regex(@$"{WorldDefine.LOD_KEYWORD}\d+", RegexOptions.IgnoreCase);
            var match = regexExp.Match(lodText);
            if (match.Length != lodText.Length)
            {
                return;
            }

            m_Prefix = m_LOD0.Substring(0, lodPos);

            var prefixLOD = m_LOD0.Substring(0, lodPos + WorldDefine.LOD_KEYWORD.Length);

            m_LODs = null;
            for (var lod = 0; lod < m_MaxLODCount; ++lod)
            {
                if (File.Exists($"{prefixLOD}{lod}.prefab"))
                {
                    m_LODs ??= new();
                    m_LODs.Add(lod);
                }
            }

            if (m_LODs == null)
            {
                return;
            }

            m_LOD0 = prefixLOD;

            for (var i = 0; i < m_LODs.Count - 1; ++i)
            {
                if (m_LODs[i + 1] - m_LODs[i] > 1)
                {
                    Debug.LogError($"{m_LOD0} lod is not continuous");
                }
            }

            m_LODPaths = new string[m_LODs.Count];
            for (var i = 0; i < m_LODs.Count; ++i)
            {
                m_LODPaths[i] = GetPrefabPath(i);
            }
        }

        private void CreateLODGroup()
        {
            var curCheckPath = GetPrefabPath(0);

            var group = 0;
            m_LODGroup = new int[m_MaxLODCount];
            m_LODGroup[0] = group;
            for (var lod = 1; lod < m_LODGroup.Length; ++lod)
            {
                var path = GetPrefabPath(lod);
                if (path != curCheckPath)
                {
                    curCheckPath = path;
                    ++group;
                }
                m_LODGroup[lod] = group;
            }
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "ResourceDescriptor.Version");

            base.EditorSerialize(serializer, label, converter);

            serializer.WriteString(m_LOD0GUID, "LOD0 GUID");
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("ResourceDescriptor.Version");

            base.EditorDeserialize(deserializer, label);

            m_LOD0GUID = deserializer.ReadString("LOD0 GUID");
        }

        public override void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "ResourceDescriptor.Version");

            base.GameSerialize(serializer, label, converter);

            serializer.WriteInt32Array(m_LODGroup, "LOD Group");
            serializer.WriteStringArray(m_LODPaths, "LOD Paths");
            serializer.WriteString(GetPath(0), "LOD 0");
            serializer.WriteInt32List(m_LODs, "Found LODs");
        }

        private string m_Prefix;
        private Rect m_Bounds;
        private GameObject m_Prefab;
        [XDaySerializableField(1, "LOD0 GUID")]
        private string m_LOD0GUID;
        private const int m_MaxLODCount = 8;
        private const int m_Version = 1;
        private const int m_RuntimeVersion = 1;
    }
}

//XDay