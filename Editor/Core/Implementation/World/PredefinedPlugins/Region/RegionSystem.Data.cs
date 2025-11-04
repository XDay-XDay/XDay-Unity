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

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystem
    {
        public class CurveRegionMeshGenerationLODParam : ISerializable
        {
            public string TypeName => "CurveRegionMeshGenerationLODParam";

            public CurveRegionMeshGenerationLODParam()
            {
            }

            public CurveRegionMeshGenerationLODParam(int lod,
                float segmentLengthRatio,
                float minTangentLength,
                float maxTangentLength,
                float pointDeltaDistance,
                int maxPointCountInOneSegment,
                bool moreRectangular,
                float lineWidth,
                float textureAspectRatio,
                float gridErrorThreshold,
                Material edgeMaterial, Material regionMaterial,
                bool useVertexColorForRegionMesh,
                bool combineMesh,
                bool mergeEdge, float edgeHeight, bool shareEdge, bool combineAllEdgesOfOneRegion)
            {
                SegmentLengthRatio = segmentLengthRatio;
                MinTangentLength = minTangentLength;
                MaxTangentLength = maxTangentLength;
                PointDeltaDistance = pointDeltaDistance;
                MaxPointCountInOneSegment = maxPointCountInOneSegment;
                MoreRectangular = moreRectangular;
                LineWidth = lineWidth;
                TextureAspectRatio = textureAspectRatio;
                if (edgeMaterial == null)
                {
                    //wzw temp
                    edgeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Packages/XDay/Editor/Core/Resource/Material/GridMaterial.mat");
                }
                EdgeMaterial = edgeMaterial;
                if (regionMaterial == null)
                {
                    if (lod == 0)
                    {
                        //wzw temp
                        regionMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Packages/XDay/Editor/Core/Resource/Material/GridMaterial.mat");
                    }
                    else
                    {
                        //wzw temp
                        regionMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Packages/XDay/Editor/Core/Resource/Material/GridMaterial.mat");
                    }
                }
                RegionMaterial = regionMaterial;
                UseVertexColorForRegionMesh = useVertexColorForRegionMesh;
                GridErrorThreshold = gridErrorThreshold;
                CombineMesh = combineMesh;
                MergeEdge = mergeEdge;
                EdgeHeight = edgeHeight;
                ShareEdge = shareEdge;
                CombineAllEdgesOfOneRegion = combineAllEdgesOfOneRegion;
            }

            public void Init()
            {
                EdgeMaterial = EditorHelper.GetObjectFromGuid<Material>(m_EdgeMaterialGUID);
                RegionMaterial = EditorHelper.GetObjectFromGuid<Material>(m_RegionMaterialGUID);
            }

            public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
            {
                serializer.WriteSingle(SegmentLengthRatio, "SegmentLengthRatio");
                serializer.WriteSingle(MinTangentLength, "MinTangentLength");
                serializer.WriteSingle(MaxTangentLength, "MaxTangentLength");
                serializer.WriteSingle(PointDeltaDistance, "PointDeltaDistance");
                serializer.WriteInt32(MaxPointCountInOneSegment, "MaxPointCountInOneSegment");
                serializer.WriteBoolean(MoreRectangular, "MoreRectangular");
                serializer.WriteSingle(LineWidth, "LineWidth");
                serializer.WriteSingle(TextureAspectRatio, "TextureAspectRatio");
                serializer.WriteSingle(GridErrorThreshold, "GridErrorThreshold");
                serializer.WriteString(EditorHelper.GetObjectGUID(EdgeMaterial), "EdgeMaterial");
                serializer.WriteString(EditorHelper.GetObjectGUID(RegionMaterial), "RegionMaterial");
                serializer.WriteBoolean(UseVertexColorForRegionMesh, "UseVertexColorForRegionMesh");
                serializer.WriteBoolean(CombineMesh, "CombineMesh");
                serializer.WriteBoolean(MergeEdge, "MergeEdge");
                serializer.WriteBoolean(ShareEdge, "ShareEdge");
                serializer.WriteSingle(EdgeHeight, "EdgeHeight");
                serializer.WriteBoolean(CombineAllEdgesOfOneRegion, "CombineAllEdgesOfOneRegion");
            }

            public void EditorDeserialize(IDeserializer deserializer, string label)
            {
                SegmentLengthRatio = deserializer.ReadSingle("SegmentLengthRatio");
                MinTangentLength = deserializer.ReadSingle("MinTangentLength");
                MaxTangentLength = deserializer.ReadSingle("MaxTangentLength");
                PointDeltaDistance = deserializer.ReadSingle("PointDeltaDistance");
                MaxPointCountInOneSegment = deserializer.ReadInt32("MaxPointCountInOneSegment");
                MoreRectangular = deserializer.ReadBoolean("MoreRectangular");
                LineWidth = deserializer.ReadSingle("LineWidth");
                TextureAspectRatio = deserializer.ReadSingle("TextureAspectRatio");
                GridErrorThreshold = deserializer.ReadSingle("GridErrorThreshold");
                m_EdgeMaterialGUID = deserializer.ReadString("EdgeMaterial");
                m_RegionMaterialGUID = deserializer.ReadString("RegionMaterial");
                UseVertexColorForRegionMesh = deserializer.ReadBoolean("UseVertexColorForRegionMesh");
                CombineMesh = deserializer.ReadBoolean("CombineMesh");
                MergeEdge = deserializer.ReadBoolean("MergeEdge");
                ShareEdge = deserializer.ReadBoolean("ShareEdge");
                EdgeHeight = deserializer.ReadSingle("EdgeHeight");
                CombineAllEdgesOfOneRegion = deserializer.ReadBoolean("CombineAllEdgesOfOneRegion");
            }

            public float SegmentLengthRatio = 0.3f;
            public float MinTangentLength = 10.0f;
            public float MaxTangentLength = 30.0f;
            public float PointDeltaDistance = 10.0f;
            public int MaxPointCountInOneSegment = 10;
            public bool MoreRectangular = false;
            public float LineWidth = 30;
            public float TextureAspectRatio = 2.0f;
            public float GridErrorThreshold;
            public Material EdgeMaterial;
            public Material RegionMaterial;
            public bool UseVertexColorForRegionMesh;
            public bool CombineMesh;
            public bool MergeEdge;
            public bool ShareEdge;
            public float EdgeHeight;
            public bool CombineAllEdgesOfOneRegion;

            private string m_EdgeMaterialGUID;
            private string m_RegionMaterialGUID;
        }

        public class TerritorySharedEdgeInfo
        {
            public int TerritoryID;
            public int NeighbourTerritoryID;
            public string PrefabPath;
            public Material Material;
        }

        public class CurveRegionMeshGenerationParam : ISerializable
        {
            public string TypeName => "CurveRegionMeshGenerationParam";

            public CurveRegionMeshGenerationParam()
            {
            }

            public CurveRegionMeshGenerationParam(float vertexDisplayRadius, float segmentLengthRatioRandomRange, float tangentRotationRandomRange, List<CurveRegionMeshGenerationLODParam> lodParams)
            {
                VertexDisplayRadius = vertexDisplayRadius;
                SegmentLengthRatioRandomRange = segmentLengthRatioRandomRange;
                TangentRotationRandomRange = tangentRotationRandomRange;
                LODParams = lodParams;
            }

            public void Init()
            {
                foreach (var param in LODParams)
                {
                    param.Init();
                }
            }

            public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
            {
                serializer.WriteSingle(VertexDisplayRadius, "VertexDisplayRadius");
                serializer.WriteSingle(SegmentLengthRatioRandomRange, "SegmentLengthRatioRandomRange");
                serializer.WriteSingle(TangentRotationRandomRange, "TangentRotationRandomRange");

                serializer.WriteList(LODParams, "LODParams", (param, index) =>
                {
                    serializer.WriteSerializable(param, $"LODParam {index}", converter, false);
                });
            }

            public void EditorDeserialize(IDeserializer deserializer, string label)
            {
                VertexDisplayRadius = deserializer.ReadSingle("VertexDisplayRadius");
                SegmentLengthRatioRandomRange = deserializer.ReadSingle("SegmentLengthRatioRandomRange");
                TangentRotationRandomRange = deserializer.ReadSingle("TangentRotationRandomRange");

                LODParams = deserializer.ReadList("LODParams", (index) =>
                {
                    return deserializer.ReadSerializable<CurveRegionMeshGenerationLODParam>($"LODParam {index}", false);
                });
            }

            public float VertexDisplayRadius = 2.5f;
            public float SegmentLengthRatioRandomRange = 0.1f;
            public float TangentRotationRandomRange = 20.0f;
            public List<CurveRegionMeshGenerationLODParam> LODParams = new();
        }

        public class EditorRegionMeshGenerationParam : ISerializable
        {
            public string TypeName => "EditorRegionMeshGenerationParam";

            public EditorRegionMeshGenerationParam()
            {
            }

            public EditorRegionMeshGenerationParam(int cornerSegment, float borderSizeRatio,
                float uvScale, bool curveCorner, string territoryMaterialGuid)
            {
                CornerSegment = cornerSegment;
                BorderSizeRatio = borderSizeRatio;
                UVScale = uvScale;
                CurveCorner = curveCorner;

                RegionMeshMaterial = EditorHelper.GetObjectFromGuid<Material>(territoryMaterialGuid);

                if (RegionMeshMaterial == null)
                {
                    //wzw temp
                    RegionMeshMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Packages/XDay/Editor/Core/Resource/Material/GridMaterial.mat");
                }
            }

            public void Init()
            {
                RegionMeshMaterial = EditorHelper.GetObjectFromGuid<Material>(m_RegionMeshMaterialGUID);
            }

            public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
            {
                serializer.WriteInt32(m_Version, "Version");

                serializer.WriteInt32(CornerSegment, "CornerSegment");
                serializer.WriteSingle(BorderSizeRatio, "BorderSizeRatio");
                serializer.WriteSingle(UVScale, "UVScale");
                serializer.WriteBoolean(CurveCorner, "CurveCorner");
                serializer.WriteString(EditorHelper.GetObjectGUID(RegionMeshMaterial), "RegionMeshMaterial");
            }

            public void EditorDeserialize(IDeserializer deserializer, string label)
            {
                deserializer.ReadInt32("Version");

                CornerSegment = deserializer.ReadInt32("CornerSegment");
                BorderSizeRatio = deserializer.ReadSingle("BorderSizeRatio");
                UVScale = deserializer.ReadSingle("UVScale");
                CurveCorner = deserializer.ReadBoolean("CurveCorner");
                m_RegionMeshMaterialGUID = deserializer.ReadString("RegionMeshMaterial");
            }

            public int CornerSegment;
            public float BorderSizeRatio;
            public float UVScale;
            public bool CurveCorner;
            public Material RegionMeshMaterial;
            private string m_RegionMeshMaterialGUID;
            private const int m_Version = 1;
        }
    }
}
