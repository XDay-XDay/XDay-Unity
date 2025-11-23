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
    public partial class SmoothRegionCreator
    {
        private class Edge
        {
            public bool IsHorizontal => Mathf.Approximately(Start.z, End.z);

            public Edge(int id, Vector3 start, Vector3 end, int selfRegionID, int neighbourRegionID)
            {
                ID = id;
                Start = start;
                End = end;
                SelfRegionID = selfRegionID;
                NeighbourRegionID = neighbourRegionID;
            }

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

        //将outline根据相邻的区域分段
        private class SharedEdgeWithNeighbourRegion
        {
            public SharedEdgeWithNeighbourRegion(List<Vector3> controlPoints, int selfRegionID, int neighbourRegionID)
            {
                m_ControlPointPositions = controlPoints;
                SelfRegionID = selfRegionID;
                NeighbourRegionID = neighbourRegionID;
            }

            public void RemoveControlPoint(Vector3 pos)
            {
                for (int i = 0; i < m_ControlPointPositions.Count; ++i)
                {
                    if (m_ControlPointPositions[i] == pos)
                    {
                        m_ControlPointPositions.RemoveAt(i);
                        break;
                    }
                }
            }

            public void ConvertToControlPoints(List<ControlPoint> controlPointsReference)
            {
                ControlPoints = new List<ControlPoint>();
                for (int i = 0; i < m_ControlPointPositions.Count; ++i)
                {
                    var controlPoint = GetControlPoint(controlPointsReference, m_ControlPointPositions[i]);
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
                return m_ControlPointPositions[^1] == pos;
            }

            public int SelfRegionID;
            public int NeighbourRegionID;
            public List<ControlPoint> ControlPoints;
            //曲线点
            public List<Vector3> EvaluatedVertices;
            public string PrefabPath;
            public string MeshPath;
            public int FixedEdgePointCount = 0;
            private readonly List<Vector3> m_ControlPointPositions;
        }

        private class Region
        {
            public enum ObjectType
            {
                Region,
                Border,
                Count,
            }

            public List<Edge> Edges => m_Edges;
            public List<Vector3> Outline { get => m_Outline; set => m_Outline = value; }
            public List<ControlPoint> ControlPoints { get => m_ControlPoints; set => m_ControlPoints = value; }
            public List<SharedEdgeWithNeighbourRegion> SharedEdges => m_OutlineSharedEdges;
            public int RegionID => m_RegionID;
            public Color Color => m_Color;
            public List<Vector2Int> Coordinates => m_RegionCoordinates;

            public Region(List<SharedEdgeWithNeighbourRegion> sharedEdges, List<Edge> edges, List<Vector3> outline, 
                int regionID, Color color, List<Vector2Int> regionCoordinates)
            {
                Debug.Assert(sharedEdges.Count > 0);
                m_OutlineSharedEdges = sharedEdges;
                m_RegionID = regionID;
                m_Edges = edges;
                m_Outline = outline;
                m_Color = color;
                m_RegionCoordinates.AddRange(regionCoordinates);
            }

            public void CreateSharedEdgeControlPoints()
            {
                //将shared edge的坐标转换成control point的引用
                for (int i = 0; i < m_OutlineSharedEdges.Count; ++i)
                {
                    m_OutlineSharedEdges[i].ConvertToControlPoints(ControlPoints);
                }
            }

            public void HideLine()
            {
                if (m_Objects[(int)ObjectType.Border] != null)
                {
                    m_Objects[(int)ObjectType.Border].SetActive(false);
                }
            }

            public void ShowLine()
            {
                if (m_Objects[(int)ObjectType.Border] != null)
                {
                    m_Objects[(int)ObjectType.Border].SetActive(true);
                }
            }

            public void HideMesh()
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

            public SharedEdgeWithNeighbourRegion GetSharedEdge(int regionID, int neighbourRegionID)
            {
                foreach (var edge in m_OutlineSharedEdges)
                {
                    if (edge.SelfRegionID == regionID && edge.NeighbourRegionID == neighbourRegionID)
                    {
                        return edge;
                    }
                }
                return null;
            }

            public Vector3[] MeshVertices;
            public int[] MeshIndices;

            private List<Edge> m_Edges;
            private List<SharedEdgeWithNeighbourRegion> m_OutlineSharedEdges = new();
            private List<Vector3> m_Outline;
            private List<ControlPoint> m_ControlPoints = new();
            private List<Vector2Int> m_RegionCoordinates = new();
            private int m_RegionID;
            private Color m_Color;
            private GameObject[] m_Objects = new GameObject[(int)ObjectType.Count];
        }

        private class ControlPoint
        {
            public Vector3 Position => m_Pos;
            public Vector3 Tangent0 => m_Tangent0;
            public Vector3 Tangent1 => m_Tangent1;

            public ControlPoint(Vector3 pos, Vector3 tangent0, Vector3 tangent1)
            {
                m_Pos = pos;
                m_Tangent0 = tangent0;
                m_Tangent1 = tangent1;
            }

            //为了多线程执行，保存这些数据
            private Vector3 m_Pos;
            private Vector3 m_Tangent0;
            private Vector3 m_Tangent1;
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
            public int RegionID => m_RegionID;
            public List<Vector2Int> Coordinates => m_Coordinates;

            public RegionInput(int regionID, List<Vector2Int> coordinates)
            {
                Debug.Assert(coordinates.Count > 0);
                m_RegionID = regionID;
                m_Coordinates = coordinates;
            }

            private List<Vector2Int> m_Coordinates;
            private int m_RegionID;
        }

        public class CreateParam
        {
            public float PointDeltaDistance => m_PointDeltaDistance;
            public float SegmentLengthRatio => m_SegmentLengthRatio;
            public float MinTangentLength => m_MinTangentLength;
            public float MaxTangentLength => m_MaxTangentLength;
            public int MaxPointCountInOneSegment => m_MaxPointCountInOneSegment;
            public float LineWidth => m_LineWidth;
            public float GridErrorThreshold => m_GridErrorThreshold;
            public Material EdgeMaterial => m_EdgeMaterial;
            public Material RegionMaterial => m_RegionMaterial;

            public CreateParam(float pointDeltaDistance, float segmentLengthRatio, float minTangentLength, float maxTangentLength, 
                int maxPointCountInOneSegment, float lineWidth, float gridErrorThreshold, Material edgeMaterial, Material regionMaterial)
            {
                m_PointDeltaDistance = pointDeltaDistance;
                m_SegmentLengthRatio = segmentLengthRatio;
                m_MinTangentLength = minTangentLength;
                m_MaxTangentLength = maxTangentLength;
                m_MaxPointCountInOneSegment = maxPointCountInOneSegment;
                m_LineWidth = lineWidth;
                m_RegionMaterial = regionMaterial;
                m_EdgeMaterial = edgeMaterial;
                m_GridErrorThreshold = gridErrorThreshold;
            }

            //生成曲线点时两点之间的最小距离
            private readonly float m_PointDeltaDistance;
            //tangent的长度占线段长度的比例
            private readonly float m_SegmentLengthRatio;
            private readonly float m_MinTangentLength;
            private float m_MaxTangentLength;
            //一段中最多插值多少个曲线点
            private readonly int m_MaxPointCountInOneSegment;
            //线段宽度
            private readonly float m_LineWidth;
            //删除顶点时的距离误差格子数
            private readonly float m_GridErrorThreshold;
            private readonly Material m_EdgeMaterial;
            private readonly Material m_RegionMaterial;
        }

        //生成需要的所有输入参数
        public class Input
        {
            public float GridSize => m_GridSize;
            public int HorizontalGridCount => m_HorizontalGridCount;
            public int VerticalGridCount => m_VerticalGridCount;
            public List<RegionInput> Regions => m_Regions;
            public System.Func<int, int, int> GetRegionIDFunc => m_GetRegionIDFunc;
            public System.Func<int, int> GetRegionConfigIDFunc => m_GetRegionConfigIDFunc;
            public System.Func<string, int, int, string> GetRegionObjectNamePrefix => m_GetRegionObjectNamePrefix;
            public System.Func<int, int, Vector3> FromCoordinateToPositionFunc => m_FromCoordinateToPositionFunc;
            public System.Func<Vector3, Vector2Int> FromPositionToCoordinate => m_FromPositionToCoordinateFunc;
            public System.Func<int, Color> GetRegionColorFunc => m_GetRegionColorFunc;
            public System.Func<int, Vector3> GetRegionCenterFunc => m_GetRegionCenterFunc;
            public CreateParam Settings => m_Settings;

            public Input(List<RegionInput> regions, float gridSize, int horizontalGridCount, int verticalGridCount,
                System.Func<int, int, int> GetRegionIDFunc,
                System.Func<int, int> GetRegionConfigIDFunc,
                System.Func<string, int, int, string> GetRegionObjectNamePrefix,
                System.Func<int, int, Vector3> FromCoordinateToPositionFunc,
                System.Func<Vector3, Vector2Int> FromPositionToCoordinateFunc,
                System.Func<int, Color> GetRegionColorFunc,
                System.Func<int, Vector3> getRegionCenterFunc,
                CreateParam settings)
            {
                m_GridSize = gridSize;
                m_HorizontalGridCount = horizontalGridCount;
                m_VerticalGridCount = verticalGridCount;
                m_Regions = regions;
                m_GetRegionIDFunc = GetRegionIDFunc;
                m_GetRegionConfigIDFunc = GetRegionConfigIDFunc;
                m_GetRegionObjectNamePrefix = GetRegionObjectNamePrefix;
                m_FromCoordinateToPositionFunc = FromCoordinateToPositionFunc;
                m_FromPositionToCoordinateFunc = FromPositionToCoordinateFunc;
                m_GetRegionColorFunc = GetRegionColorFunc;
                m_GetRegionCenterFunc = getRegionCenterFunc;
                m_Settings = settings;
            }

            private readonly float m_GridSize;
            private readonly int m_HorizontalGridCount;
            private readonly int m_VerticalGridCount;
            private readonly List<RegionInput> m_Regions;
            private readonly System.Func<int, int, int> m_GetRegionIDFunc;
            private readonly System.Func<int, int> m_GetRegionConfigIDFunc;
            private readonly System.Func<string, int, int, string> m_GetRegionObjectNamePrefix;
            private readonly System.Func<int, int, Vector3> m_FromCoordinateToPositionFunc;
            private readonly System.Func<int, Color> m_GetRegionColorFunc;
            private readonly System.Func<Vector3, Vector2Int> m_FromPositionToCoordinateFunc;
            private readonly System.Func<int, Vector3> m_GetRegionCenterFunc;
            private readonly CreateParam m_Settings;
        }
    }
}

