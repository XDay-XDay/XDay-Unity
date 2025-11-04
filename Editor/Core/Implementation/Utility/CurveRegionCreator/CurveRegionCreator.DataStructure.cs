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

using UnityEngine;
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor
{
    public partial class CurveRegionCreator
    {
        private class Edge
        {
            public Edge(int id, Vector3 start, Vector3 end, int selfRegionID, int neighbourRegionID)
            {
                ID = id;
                Start = start;
                End = end;
                SelfRegionID = selfRegionID;
                NeighbourRegionID = neighbourRegionID;
            }

            public bool IsHorizontal { get { return Mathf.Approximately(Start.z, End.z); } }

            public Vector3 Start;
            public Vector3 End;
            public bool Removed = false;
            public int SelfRegionID = 0;
            public int NeighbourRegionID = 0;
            public int ID;
            //与该edge的起点相连的edge
            public Edge ConnectedToStart;
            //与该edge的终点相连的edge
            public Edge ConnectedToEnd;
        }

        private class CombinedEdge
        {
            public int SelfRegionID;
            public Mesh Mesh;
        }

        //将outline根据相邻的区域分段
        private class SharedEdgeWithNeighbourTerritroy
        {
            public SharedEdgeWithNeighbourTerritroy(List<Vector3> controlPoints, int selfRegionID, int neighbourRegionID)
            {
                ControlPointPositions = controlPoints;
                SelfRegionID = selfRegionID;
                NeighbourRegionID = neighbourRegionID;
            }

            public void OnDestroy()
            {
                Helper.DestroyUnityObject(GameObject);
            }

            public void RemoveControlPoint(Vector3 pos)
            {
                for (int i = 0; i < ControlPointPositions.Count; ++i)
                {
                    if (ControlPointPositions[i] == pos)
                    {
                        ControlPointPositions.RemoveAt(i);
                        break;
                    }
                }
            }

            public void ConvertToControlPoints(List<ControlPoint> controlPointsReference)
            {
                ControlPoints = new List<ControlPoint>();
                for (int i = 0; i < ControlPointPositions.Count; ++i)
                {
                    var controlPoint = GetControlPoint(controlPointsReference, ControlPointPositions[i]);
                    Debug.Assert(controlPoint != null);
                    ControlPoints.Add(controlPoint);
                }
            }

            private ControlPoint GetControlPoint(List<ControlPoint> controlPointsReference, Vector3 pos)
            {
                for (int i = 0; i < controlPointsReference.Count; ++i)
                {
                    if (controlPointsReference[i].Position == pos)
                    {
                        return controlPointsReference[i];
                    }
                }
                return null;
            }

            public bool IsEndPoint(Vector3 pos)
            {
                return ControlPointPositions[ControlPointPositions.Count - 1] == pos;
            }

            public int SelfRegionID;
            public int NeighbourRegionID;
            //control point
            private List<Vector3> ControlPointPositions;
            public List<ControlPoint> ControlPoints;
            //曲线点
            public List<Vector3> EvaluatedVertices;
            public GameObject GameObject;
            public string PrefabPath;
            public string MeshPath;
            public int FixedEdgePointCount = 0;
        }

        public class EdgeAssetInfo
        {
            public EdgeAssetInfo(int territoryID, int neighbourTerritoyID, string prefabPath, Material material)
            {
                TerritoryID = territoryID;
                NeighbourTerritoyID = neighbourTerritoyID;
                PrefabPath = prefabPath;
                Material = material;
            }

            public int TerritoryID;
            public int NeighbourTerritoyID;
            public string PrefabPath;
            public Material Material;
        }

        private class Territory
        {
            public enum ObjectType
            {
                Region,
                InnerOutline,
                CurveOutline,

                Count,
            }


            public List<Edge> Edges { get { return m_Edges; } }
            public List<Vector3> Outline { get { return m_Outline; } set { m_Outline = value; } }
            public List<Vector3> InnerOutline { get { return m_InnerOutline; } set { m_InnerOutline = value; } }
            public List<ControlPoint> ControlPoints { get { return m_ControlPoints; } set { m_ControlPoints = value; } }
            public List<SharedEdgeWithNeighbourTerritroy> SharedEdges
            {
                get
                {
                    if (m_CombinedEdge != null)
                    {
                        return null;
                    }
                    return m_OutlineSharedEdges;
                }
            }
            public CombinedEdge CombinedEdge { get { return m_CombinedEdge; } }
            public int RegionID { get { return m_RegionID; } }
            public Color Color { get { return m_Color; } }
            public string PrefabPath { get { return m_PrefabPath; } set { m_PrefabPath = value; } }
            public List<Vector2Int> Coordinates { get { return m_RegionCoordinates; } }

            public Territory(List<SharedEdgeWithNeighbourTerritroy> sharedEdges, List<Edge> edges, List<Vector3> outline, int regionID, Color color, List<Vector2Int> regionCoordinates)
            {
                Debug.Assert(sharedEdges.Count > 0);
                m_OutlineSharedEdges = sharedEdges;
                m_RegionID = regionID;
                m_Edges = edges;
                m_Outline = outline;
                m_Color = color;
                m_RegionCoordinates.AddRange(regionCoordinates);
            }

            public void OnDestroy()
            {
                for (int i = 0; i < m_ControlPoints.Count; ++i)
                {
                    m_ControlPoints[i].OnDestroy();
                }

                foreach (var edge in m_OutlineSharedEdges)
                {
                    edge.OnDestroy();
                }

                if (m_CombinedEdge != null)
                {
                    Helper.DestroyUnityObject(m_CombinedEdge.Mesh);
                }
                m_CombinedEdge = null;

                HideLineAndMesh();
            }

            public void CreateSharedEdgeControlPoints()
            {
                //将shared edge的坐标转换成control point的引用
                for (int i = 0; i < m_OutlineSharedEdges.Count; ++i)
                {
                    m_OutlineSharedEdges[i].ConvertToControlPoints(ControlPoints);
                }
            }

            public void HideLineAndMesh()
            {
                HideLine();
                HideMesh();
            }

            public void HideLine()
            {
                Helper.DestroyUnityObject(m_Objects[(int)ObjectType.InnerOutline]);
                m_Objects[(int)ObjectType.InnerOutline] = null;
                Helper.DestroyUnityObject(m_Objects[(int)ObjectType.CurveOutline]);
                m_Objects[(int)ObjectType.CurveOutline] = null;
            }

            public void HideMesh()
            {
                if (m_Objects[(int)ObjectType.Region] != null)
                {
                    m_Objects[(int)ObjectType.Region].SetActive(false);
                }
                for (int i = 0; i < m_OutlineSharedEdges.Count; ++i)
                {
                    if (m_OutlineSharedEdges[i].GameObject != null)
                    {
                        m_OutlineSharedEdges[i].GameObject.SetActive(false);
                    }
                }
            }

            public void HideRegionMesh()
            {
                if (m_Objects[(int)ObjectType.Region] != null)
                {
                    m_Objects[(int)ObjectType.Region].SetActive(false);
                }
            }

            public void ShowMesh()
            {
                if (m_Objects[(int)ObjectType.Region])
                {
                    m_Objects[(int)ObjectType.Region].SetActive(true);
                }
                for (int i = 0; i < m_OutlineSharedEdges.Count; ++i)
                {
                    if (m_OutlineSharedEdges[i].GameObject != null)
                    {
                        m_OutlineSharedEdges[i].GameObject.SetActive(true);
                    }
                }
            }

            public void RemoveVertices(List<Vector3> vertices)
            {
                for (int i = 0; i < vertices.Count; ++i)
                {
                    m_Outline.Remove(vertices[i]);

                    for (int k = 0; k < m_OutlineSharedEdges.Count; ++k)
                    {
                        m_OutlineSharedEdges[k].RemoveControlPoint(vertices[i]);
                    }
                }
            }

            public void HideTangent()
            {
                for (int i = 0; i < m_ControlPoints.Count; ++i)
                {
                    m_ControlPoints[i].ShowTangent(false);
                }
            }

            public void ShowTangent(int index)
            {
                if (index >= 0 && index < m_ControlPoints.Count)
                {
                    m_ControlPoints[index].ShowTangent(true);
                }
            }

            public void SetGameObject(ObjectType type, GameObject obj)
            {
                int idx = (int)type;
                if (m_Objects[idx] != null)
                {
                    Helper.DestroyUnityObject(m_Objects[idx]);
                }
                m_Objects[idx] = obj;
            }

            public GameObject GetGameObject(ObjectType type)
            {
                int idx = (int)type;
                return m_Objects[idx];
            }

            public SharedEdgeWithNeighbourTerritroy GetSharedEdge(int territoryID, int neighbourTerritoryID)
            {
                foreach (var edge in m_OutlineSharedEdges)
                {
                    if (edge.SelfRegionID == territoryID && edge.NeighbourRegionID == neighbourTerritoryID)
                    {
                        return edge;
                    }
                }
                return null;
            }

            public void SetCombinedEdge(CombinedEdge edge)
            {
                m_CombinedEdge = edge;
            }

            public Vector3[] RegionMeshVertices;
            public int[] RegionMeshIndices;

            private List<Edge> m_Edges;
            private List<SharedEdgeWithNeighbourTerritroy> m_OutlineSharedEdges = new List<SharedEdgeWithNeighbourTerritroy>();
            private CombinedEdge m_CombinedEdge;
            private List<Vector3> m_Outline;
            private List<Vector3> m_InnerOutline;
            private List<ControlPoint> m_ControlPoints = new List<ControlPoint>();
            private List<Vector2Int> m_RegionCoordinates = new List<Vector2Int>();
            private int m_RegionID;
            private Color m_Color;
            private GameObject[] m_Objects = new GameObject[(int)ObjectType.Count];
            private string m_PrefabPath;
        }

        private class ControlPoint
        {
            public Vector3 Position { get { return m_Pos; } }
            public Vector3 Tangent0 { get { return m_Tangent0; } }
            public Vector3 Tangent1 { get { return m_Tangent1; } }

            public ControlPoint(Vector3 pos, Vector3 tangent0, Vector3 tangent1, string name)
            {
                m_Pos = pos;
                m_Tangent0 = tangent0;
                m_Tangent1 = tangent1;
                m_Name = name;
            }

            //这一步在主线程执行
            public void CreateGameObjects(float vertexDisplayRadius, Transform root)
            {
                m_Point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                m_Point.transform.position = m_Pos;
                m_Point.transform.localScale = Vector3.one * vertexDisplayRadius * 2;
                m_Point.name = m_Name;
                m_Point.transform.SetParent(root, true);
                m_Point.SetActive(false);

                m_Tangents[0] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                m_Tangents[0].transform.position = m_Tangent0;
                m_Tangents[0].transform.localScale = Vector3.one * vertexDisplayRadius * 2;
                m_Tangents[0].SetActive(false);
                m_Tangents[0].transform.SetParent(root, true);

                m_Tangents[1] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                m_Tangents[1].transform.position = m_Tangent1;
                m_Tangents[1].transform.localScale = Vector3.one * vertexDisplayRadius * 2;
                m_Tangents[1].SetActive(false);
                m_Tangents[1].transform.SetParent(root, true);

                Helper.HideGameObject(m_Point);
                Helper.HideGameObject(m_Tangents[0]);
                Helper.HideGameObject(m_Tangents[1]);
            }

            public void OnDestroy()
            {
                Helper.DestroyUnityObject(m_Point);
                for (int i = 0; i < 2; ++i)
                {
                    Helper.DestroyUnityObject(m_Tangents[i]);
                }
            }

            public void ShowTangent(bool show)
            {
                for (int i = 0; i < m_Tangents.Length; ++i)
                {
                    m_Tangents[i].SetActive(show);
                }
            }

            public void MoveControlPoint(Vector3 delta)
            {
                m_Point.transform.position = m_Point.transform.position + delta;
            }

            public void MoveTangent(int tangentIndex, Vector3 delta, TangentMoveType type)
            {
                if (type == TangentMoveType.Free)
                {
                    m_Tangents[tangentIndex].transform.position = m_Tangents[tangentIndex].transform.position + delta;
                }
                else if (type == TangentMoveType.Rotate)
                {
                    int other = (tangentIndex + 1) % 2;
                    var oldDir = m_Tangents[tangentIndex].transform.position - Position;
                    float len = oldDir.magnitude;
                    m_Tangents[tangentIndex].transform.position = m_Tangents[tangentIndex].transform.position + delta;
                    var newDir = (m_Tangents[tangentIndex].transform.position - Position).normalized;
                    oldDir.Normalize();
                    m_Tangents[tangentIndex].transform.position = newDir * len + Position;
                    float angle = Helper.GetAngleBetween(oldDir, newDir);

                    float rightLen = (m_Tangents[other].transform.position - Position).magnitude;
                    var oldRightDir = (m_Tangents[other].transform.position - Position).normalized;
                    var newRightDir = Helper.RotateY(oldRightDir, angle);
                    m_Tangents[other].transform.position = newRightDir * rightLen + Position;
                }
                else if (type == TangentMoveType.RotateAndScale)
                {
                    int other = (tangentIndex + 1) % 2;
                    var oldDir = m_Tangents[tangentIndex].transform.position - Position;
                    float len = oldDir.magnitude;
                    m_Tangents[tangentIndex].transform.position = m_Tangents[tangentIndex].transform.position + delta;
                    float newLen = (m_Tangents[tangentIndex].transform.position - Position).magnitude;
                    float deltaLen = newLen - len;
                    var newDir = (m_Tangents[tangentIndex].transform.position - Position).normalized;
                    oldDir.Normalize();
                    m_Tangents[tangentIndex].transform.position = newDir * newLen + Position;
                    float angle = Helper.GetAngleBetween(oldDir, newDir);

                    float rightLen = (m_Tangents[other].transform.position - Position).magnitude + deltaLen;
                    var oldRightDir = (m_Tangents[other].transform.position - Position).normalized;
                    var newRightDir = Helper.RotateY(oldRightDir, angle);
                    m_Tangents[other].transform.position = newRightDir * rightLen + Position;
                }
            }

            private GameObject m_Point;
            //0 is left, 1 is right
            private GameObject[] m_Tangents = new GameObject[2];
            //为了多线程执行，保存这些数据
            private Vector3 m_Pos;
            private Vector3 m_Tangent0;
            private Vector3 m_Tangent1;
            private string m_Name;
        }

        private class EvaluatePoint
        {
            public EvaluatePoint(Vector3 pos)
            {
                Pos = pos;
            }

            public Vector3 Pos;
        }

        private class VertexWrapper
        {
            public bool Removed = false;
            public int Index;
            public Vector3 Position;
            public bool Moved = false;
        }

        public class RegionInput
        {
            public int RegionID { get { return m_RegionID; } }
            public List<Vector2Int> Coordinates { get { return m_Coordinates; } }

            public RegionInput(int regionID, List<Vector2Int> coordinates)
            {
                Debug.Assert(coordinates.Count > 0);
                m_RegionID = regionID;
                m_Coordinates = coordinates;
            }

            private List<Vector2Int> m_Coordinates;
            private int m_RegionID;
        }

        public class SettingInput
        {
            public float PointDeltaDistance { get { return m_PointDeltaDistance; } }
            public float SegmentLengthRatio { get { return m_SegmentLengthRatio; } }
            public float MinTangentLength { get { return m_MinTangentLength; } }
            public float MaxTangentLength { get { return m_MaxTangentLength; } }
            public int MaxPointCountInOneSegment { get { return m_MaxPointCountInOneSegment; } }
            public bool MoreRectangular { get { return m_MoreRectangular; } }
            public float LineWidth { get { return m_LineWidth; } }
            public float VertexDisplayRadius { get { return m_VertexDisplayRadius; } }
            public float TextureAspectRatio { get { return m_TextureAspectRatio; } }
            public float SegmentLengthRatioRandomRange { get { return m_SegmentLengthRatioRandomRange; } }
            public float TangentRandomRotationRange { get { return m_TangentRandomRotationRange; } }
            public float GridErrorThreshold { get { return m_GridErrorThreshold; } }
            public bool UseVertexColorForRegionMesh { get { return m_UseVertexColorForRegionMesh; } }
            public Material EdgeMaterial { get { return m_EdgeMaterial; } }
            public Material RegionMaterial { get { return m_RegionMaterial; } }
            public bool CombineMesh { get { return m_CombineMesh; } }
            public bool MergeEdge { get { return m_MergeEdge; } }
            public float EdgeHeight { get { return m_EdgeHeight; } }
            public bool ShareEdge { get { return m_ShareEdge; } }
            public bool CombineEdgesOfOneRegion { get { return m_CombineEdgesOfOneRegion; } }
            public bool GenerateUnityAssets { get { return m_GenerateUnityAssets; } }

            public SettingInput(float pointDeltaDistance, float segmentLengthRatio, float minTangentLength, float maxTangentLength, int maxPointCountInOneSegment, bool moreRectangular, float lineWidth, float vertexDisplayRadius, float textureAspectRatio, float segmentLengthRatioRandomRange, float tangentRandomRotationRange, float gridErrorThreshold, Material edgeMaterial, Material regionMaterial, bool useVertexColorForRegionMesh, bool combineMesh, bool mergeEdge, float edgeHeight, bool shareEdge, bool combineAllEdgesOfOneRegion, bool generateUnityAssets)
            {
                m_PointDeltaDistance = pointDeltaDistance;
                m_SegmentLengthRatio = segmentLengthRatio;
                m_MinTangentLength = minTangentLength;
                m_MaxTangentLength = maxTangentLength;
                m_MaxPointCountInOneSegment = maxPointCountInOneSegment;
                m_MoreRectangular = moreRectangular;
                m_LineWidth = lineWidth;
                m_VertexDisplayRadius = vertexDisplayRadius;
                m_TextureAspectRatio = textureAspectRatio;
                m_SegmentLengthRatioRandomRange = segmentLengthRatioRandomRange;
                m_TangentRandomRotationRange = tangentRandomRotationRange;
                m_UseVertexColorForRegionMesh = useVertexColorForRegionMesh;
                m_RegionMaterial = regionMaterial;
                m_EdgeMaterial = edgeMaterial;
                m_GridErrorThreshold = gridErrorThreshold;
                m_CombineMesh = combineMesh;
                m_MergeEdge = mergeEdge;
                m_EdgeHeight = edgeHeight;
                m_ShareEdge = shareEdge;
                m_CombineEdgesOfOneRegion = combineAllEdgesOfOneRegion;
                m_GenerateUnityAssets = generateUnityAssets;
            }

            //生成曲线点时两点之间的最小距离
            private float m_PointDeltaDistance;
            //tangent的长度占线段长度的比例
            private float m_SegmentLengthRatio;
            //最小tangent长度
            private float m_MinTangentLength;
            //最大tangent长度
            private float m_MaxTangentLength;
            //一段中最多插值多少个曲线点
            private int m_MaxPointCountInOneSegment;
            //是否生成更方正的边缘线
            private bool m_MoreRectangular;
            //线段宽度
            private float m_LineWidth;
            //曲线顶点显示半径
            private float m_VertexDisplayRadius;
            //图片的宽高比,用于生成无拉伸的uv
            private float m_TextureAspectRatio;
            private float m_SegmentLengthRatioRandomRange;
            private float m_TangentRandomRotationRange;
            //删除顶点时的距离误差格子数
            private float m_GridErrorThreshold;
            private Material m_EdgeMaterial;
            private Material m_RegionMaterial;
            private bool m_UseVertexColorForRegionMesh;
            //是否将mesh按组合并,用于生成lod1
            private bool m_CombineMesh;
            //是否将两个区域共享的边合并为一条,合并后所有区域edge宽度相同
            private bool m_MergeEdge;
            //是否根据相邻区域不同而分裂一个区域的边
            private bool m_ShareEdge;
            //是否合并一个区域所有的边为一条边,可以减少edge的总量
            private bool m_CombineEdgesOfOneRegion = true;
            //edge顶点的高度
            private float m_EdgeHeight;
            //是否生成unity内置格式的资源
            private bool m_GenerateUnityAssets;
        }

        //生成需要的所有输入参数
        public class Input
        {
            public float GridSize { get { return m_GridSize; } }
            public int HorizontalGridCount { get { return m_HorizontalGridCount; } }
            public int VerticalGridCount { get { return m_VerticalGridCount; } }
            public List<RegionInput> Regions { get { return m_Regions; } }
            public System.Func<int, int, int> GetRegionIDFunc { get { return m_GetRegionIDFunc; } }
            public System.Func<int, int, Vector3> FromCoordinateToPositionFunc { get { return m_FromCoordinateToPositionFunc; } }
            public System.Func<Vector3, Vector2Int> FromPositionToCoordinate { get { return m_FromPositionToCoordinateFunc; } }
            public System.Func<int, Color> GetRegionColorFunc { get { return m_GetRegionColorFunc; } }
            public System.Func<int, Vector3> GetTerritoryCenterFunc { get { return m_GetTerritoryCenterFunc; } }
            public SettingInput Settings { get { return m_Settings; } }

            public Input(List<RegionInput> regions, float gridSize, int horizontalGridCount, int verticalGridCount,
                System.Func<int, int, int> GetRegionIDFunc,
                System.Func<int, int, Vector3> FromCoordinateToPositionFunc,
                System.Func<Vector3, Vector2Int> FromPositionToCoordinateFunc,
                System.Func<int, Color> GetRegionColorFunc,
                System.Func<int, Vector3> getTerritoryCenterFunc,
                SettingInput settings)
            {
                m_GridSize = gridSize;
                m_HorizontalGridCount = horizontalGridCount;
                m_VerticalGridCount = verticalGridCount;
                m_Regions = regions;
                m_GetRegionIDFunc = GetRegionIDFunc;
                m_FromCoordinateToPositionFunc = FromCoordinateToPositionFunc;
                m_FromPositionToCoordinateFunc = FromPositionToCoordinateFunc;
                m_GetRegionColorFunc = GetRegionColorFunc;
                m_GetTerritoryCenterFunc = getTerritoryCenterFunc;
                m_Settings = settings;
            }

            private float m_GridSize;
            private int m_HorizontalGridCount;
            private int m_VerticalGridCount;
            private List<RegionInput> m_Regions;
            private System.Func<int, int, int> m_GetRegionIDFunc;
            private System.Func<int, int, Vector3> m_FromCoordinateToPositionFunc;
            private System.Func<int, Color> m_GetRegionColorFunc;
            private System.Func<Vector3, Vector2Int> m_FromPositionToCoordinateFunc;
            private System.Func<int, Vector3> m_GetTerritoryCenterFunc;
            private SettingInput m_Settings;
        }
    }
}

