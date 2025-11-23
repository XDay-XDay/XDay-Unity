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
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;

namespace XDay.WorldAPI.Region.Editor
{
    /// <summary>
    /// 生成LOD0的方形边界
    /// </summary>
    internal class RegionRectangleMeshGen : IRegionSystemLODMeshGen
    {
        public string TypeName => "RegionRectangleMeshGen";

        public RegionRectangleMeshGen()
        {
        }

        public void Init(RegionSystemLayer layer)
        {
            m_Layer = layer;
        }

        public void OnDestroy()
        {
            Clear();
        }

        public void Generate(bool generateAssets)
        {
            Clear();

            var borderMaterial = EditorHelper.GetObjectFromGuid<Material>(m_BorderMaterialGUID);
            if (borderMaterial == null)
            {
                Debug.LogError("没有设置边界材质");
                return;
            }
            if (m_BorderWidth <= 0)
            {
                Debug.LogError("无效的边界宽度");
                return;
            }

            var totalVertexCount = 0;
            var totalIndexCount = 0;
            var regions = m_Layer.Regions;
            for (var i = 0; i < regions.Count; ++i)
            {
                var region = regions[i];
                EditorUtility.DisplayProgressBar("生成区域", $"生成第{i}/{regions.Count}个", (i + 1) * 1f / regions.Count);
                var coord = m_Layer.GetRegionCoordinates(region.ID);
                if (coord.Count > 0)
                {
                    var polygon = m_Layer.GetOutlinePolygon(region.ID, coord);
                    var center = m_Layer.GetRegionCenter(region.ID);
                    var borderGen = new RectangleBorderCreator();
                    var param = new RectangleBorderCreator.CreateInfo()
                    {
                        BorderMaterial = borderMaterial,
                        MeshMaterial = null,
                        Width = m_BorderWidth,
                        Name = "LOD0 Border",
                        Parent = m_Layer.Renderer.Root.transform,
                        Center = center,
                    };
                    var obj = borderGen.Generate(param, polygon, out var vertexCount, out var indexCount);
                    totalVertexCount += vertexCount;
                    totalIndexCount += indexCount;
                    obj.GetComponentInChildren<MeshRenderer>(true).sharedMaterial.color = region.Color;
                    m_BorderGameObjects.Add(region.ID, obj);
                }
            }

            Debug.LogError($"LOD0: vertex count: {totalVertexCount}, triangle count: {totalIndexCount / 3}");

            if (generateAssets)
            {
                GenerateAssets();
            }

            EditorUtility.ClearProgressBar();
        }

        private void GenerateAssets()
        {
            foreach (var kv in m_BorderGameObjects)
            {
                var obj = kv.Value;
                var region = m_Layer.GetRegion(kv.Key);
                var namePrefix = m_Layer.GetRegionObjectNamePrefix("border", region.ConfigID, 0);

                //create material
                {
                    var renderer = obj.GetComponentInChildren<MeshRenderer>(true);
                    var material = Object.Instantiate(renderer.sharedMaterial);
                    var materialPath = $"{namePrefix}.mat";
                    AssetDatabase.CreateAsset(material, materialPath);
                    var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    renderer.sharedMaterial = newMaterial;
                }

                //create mesh
                {
                    var filter = obj.GetComponentInChildren<MeshFilter>(true);
                    var mesh = Object.Instantiate(filter.sharedMesh);
                    mesh.UploadMeshData(true);
                    var meshPath = $"{namePrefix}.asset";
                    AssetDatabase.CreateAsset(mesh, meshPath);
                    var newMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    filter.sharedMesh = newMesh;
                }

                //create prefab
                {
                    PrefabUtility.SaveAsPrefabAsset(obj, $"{namePrefix}.prefab");
                }
            }

            AssetDatabase.Refresh();
        }

        private void Clear()
        {
            foreach (var kv in m_BorderGameObjects)
            {
                Helper.DestroyUnityObject(kv.Value);
            }
            m_BorderGameObjects.Clear();
        }

        public void InspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();
            m_ShowInInspector = EditorGUILayout.Foldout(m_ShowInInspector, "LOD0参数");

            EditorGUILayout.Space();

            if (GUILayout.Button("生成", GUILayout.MaxWidth(40)))
            {
                Generate(true);
            }

            if (GUILayout.Button("预览", GUILayout.MaxWidth(40)))
            {
                Generate(false);
            }

            EditorGUILayout.EndHorizontal();
            if (m_ShowInInspector)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("显示边界"))
                {
                    ShowBorders(true);
                }

                if (GUILayout.Button("隐藏边界"))
                {
                    ShowBorders(false);
                }
                EditorGUILayout.EndHorizontal();

                m_BorderMaterialGUID = EditorHelper.ObjectFieldGUID<Material>("边界材质", m_BorderMaterialGUID);
                m_BorderWidth = EditorGUILayout.FloatField("宽度", m_BorderWidth);
                EditorGUI.indentLevel--;
            }
        }

        private void ShowBorders(bool visible)
        {
            foreach (var kv in m_BorderGameObjects)
            {
                kv.Value.SetActive(visible);
            }
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "RegionRectangleMeshGen.Version");

            serializer.WriteString(m_BorderMaterialGUID, "BorderMaterialGUID");
            serializer.WriteSingle(m_BorderWidth, "BorderWidth");
            serializer.WriteBoolean(m_ShowInInspector, "ShowInInspector");
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("RegionRectangleMeshGen.Version");

            m_BorderMaterialGUID = deserializer.ReadString("BorderMaterialGUID");
            m_BorderWidth = deserializer.ReadSingle("BorderWidth");
            m_ShowInInspector = deserializer.ReadBoolean("ShowInInspector");
        }

        [SerializeField]
        private string m_BorderMaterialGUID;
        [SerializeField]
        private float m_BorderWidth;
        [SerializeField]
        private bool m_ShowInInspector = true;
        private RegionSystemLayer m_Layer;
        private Dictionary<int, GameObject> m_BorderGameObjects = new();
        private const int m_Version = 1;
    }
}
