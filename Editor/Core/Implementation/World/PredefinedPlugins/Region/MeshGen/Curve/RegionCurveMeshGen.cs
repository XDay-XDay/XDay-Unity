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
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;
using static XDay.WorldAPI.Region.Editor.RegionSystem;

namespace XDay.WorldAPI.Region.Editor
{
    /// <summary>
    /// 生成LOD1的弧形边界和内部区域
    /// </summary>
    internal class RegionCurveMeshGen : IRegionSystemLODMeshGen
    {
        public string TypeName => "RegionCurveMeshGen";

        public RegionCurveMeshGen()
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

            var regionMaterial = EditorHelper.GetObjectFromGuid<Material>(m_RegionMaterialGUID);
            if (regionMaterial == null)
            {
                Debug.LogError("没有设置区域材质");
                return;
            }

            if (m_BorderWidth <= 0)
            {
                Debug.LogError("无效的边界宽度");
                return;
            }

            CreateParam();

            CreateAndGenerateOutlineAssets(true, m_Layer.Width, m_Layer.Height, 3, 3);
        }

        private void Clear()
        {
            m_SmoothRegionCreator?.OnDestroy();
            m_SmoothRegionCreator = null;
        }

        public void InspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();
            m_ShowInInspector = EditorGUILayout.Foldout(m_ShowInInspector, "LOD1参数");

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
                    m_SmoothRegionCreator.ShowLine();
                }

                if (GUILayout.Button("隐藏边界"))
                {
                    m_SmoothRegionCreator.HideLine();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("显示区域"))
                {
                    m_SmoothRegionCreator.ShowMesh();
                }

                if (GUILayout.Button("隐藏区域"))
                {
                    m_SmoothRegionCreator.HideMesh();
                }
                EditorGUILayout.EndHorizontal();

                m_BorderMaterialGUID = EditorHelper.ObjectFieldGUID<Material>("边界材质", m_BorderMaterialGUID);
                m_RegionMaterialGUID = EditorHelper.ObjectFieldGUID<Material>("区域材质", m_RegionMaterialGUID);
                m_BorderWidth = EditorGUILayout.FloatField("宽度", m_BorderWidth);
                m_PointDeltaDistance = EditorGUILayout.FloatField("点距", m_PointDeltaDistance);
                m_MaxPointCountInOneSegment = EditorGUILayout.IntField("分段最大点数", m_MaxPointCountInOneSegment);
                m_GridErrorThreshold = EditorGUILayout.FloatField("格子数误差", m_GridErrorThreshold);
                //m_SegmentLengthRatio = EditorGUILayout.FloatField("Segment Length Ratio", m_SegmentLengthRatio);
                //m_MinTangentLength = EditorGUILayout.FloatField("Min Tangent Length", m_MinTangentLength);
                //m_MaxTangentLength = EditorGUILayout.FloatField("Max Tangent Length", m_MaxTangentLength);
                EditorGUI.indentLevel--;
            }
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "RegionRectangleMeshGen.Version");

            serializer.WriteString(m_BorderMaterialGUID, "BorderMaterialGUID");
            serializer.WriteSingle(m_BorderWidth, "BorderWidth");
            serializer.WriteBoolean(m_ShowInInspector, "ShowInInspector");

            serializer.WriteSingle(m_VertexDisplayRadius, "VertexDisplayRadius");
            serializer.WriteSingle(m_SegmentLengthRatioRandomRange, "SegmentLengthRatioRandomRange");
            serializer.WriteSingle(m_TangentRotationRandomRange, "TangentRotationRandomRange");

            serializer.WriteSingle(m_SegmentLengthRatio, "SegmentLengthRatio");
            serializer.WriteSingle(m_MinTangentLength, "MinTangentLength");
            serializer.WriteSingle(m_MaxTangentLength, "MaxTangentLength");
            serializer.WriteSingle(m_PointDeltaDistance, "PointDeltaDistance");
            serializer.WriteInt32(m_MaxPointCountInOneSegment, "MaxPointCountInOneSegment");
            serializer.WriteBoolean(m_MoreRectangular, "MoreRectangular");
            serializer.WriteSingle(m_BorderWidth, "BorderWidth");
            serializer.WriteSingle(m_TextureAspectRatio, "TextureAspectRatio");
            serializer.WriteSingle(m_GridErrorThreshold, "GridErrorThreshold");
            serializer.WriteString(m_BorderMaterialGUID, "BorderMaterial");
            serializer.WriteString(m_RegionMaterialGUID, "RegionMaterial");
            serializer.WriteBoolean(m_UseVertexColorForRegionMesh, "UseVertexColorForRegionMesh");
            serializer.WriteBoolean(m_CombineMesh, "CombineMesh");
            serializer.WriteBoolean(m_MergeEdge, "MergeEdge");
            serializer.WriteBoolean(m_ShareEdge, "ShareEdge");
            serializer.WriteSingle(m_EdgeHeight, "EdgeHeight");
            serializer.WriteBoolean(m_CombineAllEdgesOfOneRegion, "CombineAllEdgesOfOneRegion");
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("RegionRectangleMeshGen.Version");

            m_BorderMaterialGUID = deserializer.ReadString("BorderMaterialGUID");
            m_BorderWidth = deserializer.ReadSingle("BorderWidth");
            m_ShowInInspector = deserializer.ReadBoolean("ShowInInspector");

            m_VertexDisplayRadius = deserializer.ReadSingle("VertexDisplayRadius");
            m_SegmentLengthRatioRandomRange = deserializer.ReadSingle("SegmentLengthRatioRandomRange");
            m_TangentRotationRandomRange = deserializer.ReadSingle("TangentRotationRandomRange");
            m_SegmentLengthRatio = deserializer.ReadSingle("SegmentLengthRatio");
            m_MinTangentLength = deserializer.ReadSingle("MinTangentLength");
            m_MaxTangentLength = deserializer.ReadSingle("MaxTangentLength");
            m_PointDeltaDistance = deserializer.ReadSingle("PointDeltaDistance");
            m_MaxPointCountInOneSegment = deserializer.ReadInt32("MaxPointCountInOneSegment");
            m_MoreRectangular = deserializer.ReadBoolean("MoreRectangular");
            m_BorderWidth = deserializer.ReadSingle("LineWidth");
            m_TextureAspectRatio = deserializer.ReadSingle("TextureAspectRatio");
            m_GridErrorThreshold = deserializer.ReadSingle("GridErrorThreshold");
            m_BorderMaterialGUID = deserializer.ReadString("BorderMaterial");
            m_RegionMaterialGUID = deserializer.ReadString("RegionMaterial");
            m_UseVertexColorForRegionMesh = deserializer.ReadBoolean("UseVertexColorForRegionMesh");
            m_CombineMesh = deserializer.ReadBoolean("CombineMesh");
            m_MergeEdge = deserializer.ReadBoolean("MergeEdge");
            m_ShareEdge = deserializer.ReadBoolean("ShareEdge");
            m_EdgeHeight = deserializer.ReadSingle("EdgeHeight");
            m_CombineAllEdgesOfOneRegion = deserializer.ReadBoolean("CombineAllEdgesOfOneRegion");
        }

        public void CreateAndGenerateOutlineAssets(bool generateAssets, float layerWidth, float layerHeight,
                    int horizontalTileCount, int verticalTileCount)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Generating Region Data", $"Generating...", 0);
                CreateOutline(true);
                GenerateOutlineAssets(true, generateAssets, layerWidth, layerHeight, horizontalTileCount, verticalTileCount, m_Layer.Renderer.Root.transform);
                EditorUtility.ClearProgressBar();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
                EditorUtility.ClearProgressBar();
            }
        }

        public void CreateOutline(bool clearProgressBar)
        {
            m_SmoothRegionCreator?.OnDestroy();
            m_SmoothRegionCreator = new SmoothRegionCreator(m_Layer.Renderer.Root.transform, m_Layer.Width, m_Layer.Height);
            List<SmoothRegionCreator.RegionInput> regionsInput = new();
            List<Task<SmoothRegionCreator.RegionInput>> tasks = new();

            var regions = m_Layer.Regions;
            for (int i = 0; i < regions.Count; ++i)
            {
                int idx = i;
                var task = Task.Run(() =>
                {
                    var coordinates = m_Layer.GetRegionCoordinates(regions[idx].ID);
                    if (coordinates.Count > 0)
                    {
                        SmoothRegionCreator.RegionInput regionInput = new(regions[idx].ID, coordinates);
                        return regionInput;
                    }
                    return null;
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            for (int i = 0; i < tasks.Count; ++i)
            {
                var regionInput = tasks[i].Result;
                if (regionInput != null)
                {
                    regionsInput.Add(regionInput);
                }
            }

            SmoothRegionCreator.CreateParam settings = new(
                m_SmoothRegionMeshGenerationParam.PointDeltaDistance, 
                m_SmoothRegionMeshGenerationParam.SegmentLengthRatio, 
                m_SmoothRegionMeshGenerationParam.MinTangentLength, 
                m_SmoothRegionMeshGenerationParam.MaxTangentLength, 
                m_SmoothRegionMeshGenerationParam.MaxPointCountInOneSegment, 
                m_SmoothRegionMeshGenerationParam.LineWidth, 
                m_SmoothRegionMeshGenerationParam.GridErrorThreshold, 
                m_SmoothRegionMeshGenerationParam.EdgeMaterial,
                m_SmoothRegionMeshGenerationParam.RegionMaterial);

            SmoothRegionCreator.Input input = new(
                regionsInput, 
                m_Layer.GridWidth, 
                m_Layer.HorizontalGridCount, 
                m_Layer.VerticalGridCount, 
                m_Layer.GetRegionID, 
                m_Layer.GetRegionConfigID, 
                m_Layer.GetRegionObjectNamePrefix,
                m_Layer.CoordinateToPosition, 
                m_Layer.PositionToCoordinate, 
                m_Layer.GetRegionColor, 
                m_Layer.GetRegionCenter, 
                settings);
            m_SmoothRegionCreator.Create(input, true);

            if (clearProgressBar)
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public void GenerateOutlineAssets(bool displayProgressBar, bool generateAssets, 
            float layerWidth, float layerHeight, int horizontalTileCount, int verticalTileCount, Transform parent)
        {
            m_SmoothRegionCreator?.Generate(m_Layer.GetPrefabFolder(1), 1, displayProgressBar, generateAssets, 
                layerWidth, layerHeight, horizontalTileCount, verticalTileCount, parent);
        }

        private void CreateParam()
        {
            m_SmoothRegionMeshGenerationParam = new SmoothRegionMeshGenerationParam(0,
                segmentLengthRatio: m_SegmentLengthRatio,
                minTangentLength: m_Layer.GridWidth * m_MinTangentLength,
                maxTangentLength: m_Layer.GridWidth * m_MaxTangentLength,
                pointDeltaDistance: m_Layer.GridWidth * m_PointDeltaDistance,
                maxPointCountInOneSegment: m_MaxPointCountInOneSegment,
                lineWidth: m_BorderWidth,
                gridErrorThreshold: m_GridErrorThreshold,
                edgeMaterial: EditorHelper.GetObjectFromGuid<Material>(m_BorderMaterialGUID),
                regionMaterial: EditorHelper.GetObjectFromGuid<Material>(m_RegionMaterialGUID));
        }

        [SerializeField]
        private string m_BorderMaterialGUID;
        [SerializeField]
        private string m_RegionMaterialGUID;
        [SerializeField]
        private float m_BorderWidth;
        [SerializeField]
        private bool m_ShowInInspector = true;
        [SerializeField]
        private float m_SegmentLengthRatioRandomRange = 0.1f;
        [SerializeField]
        private float m_VertexDisplayRadius = 0.5f;
        [SerializeField]
        private float m_TangentRotationRandomRange = 20f;
        [SerializeField]
        private int m_MaxPointCountInOneSegment = 4;
        [SerializeField]
        private float m_SegmentLengthRatio = 0.3f;
        [SerializeField]
        private float m_MinTangentLength = 0.1f;
        [SerializeField]
        private float m_MaxTangentLength = 0.6f;
        [SerializeField]
        private float m_PointDeltaDistance = 0.1f;
        [SerializeField]
        private float m_TextureAspectRatio = 2f;
        [SerializeField]
        private float m_GridErrorThreshold = 10f;
        [SerializeField]
        private bool m_MoreRectangular = false;
        [SerializeField]
        private bool m_UseVertexColorForRegionMesh = false;
        [SerializeField]
        private bool m_CombineMesh = false;
        [SerializeField]
        private bool m_MergeEdge = false;
        [SerializeField]
        private bool m_ShareEdge = true;
        [SerializeField]
        private bool m_CombineAllEdgesOfOneRegion = false;
        [SerializeField]
        private float m_EdgeHeight = 1f;

        private SmoothRegionMeshGenerationParam m_SmoothRegionMeshGenerationParam;
        private SmoothRegionCreator m_SmoothRegionCreator;
        private RegionSystemLayer m_Layer;
        private const int m_Version = 1;
    }
}
