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
using UnityEditor;
using System.Threading.Tasks;
using XDay.UtilityAPI.Math;

namespace XDay.UtilityAPI.Editor
{
    public partial class SmoothRegionCreator
    {
        public int NextID => ++m_NextID;

        public SmoothRegionCreator(Transform parent, float width, float height)
        {
            m_Root = new GameObject("Root");
            m_Root.transform.SetParent(parent, true);
            Helper.HideGameObject(m_Root);

            m_Width = width;
            m_Height = height;

            m_Random = new System.Random(System.DateTime.Now.Millisecond);
        }

        public void OnDestroy()
        {
            Clear();

            Helper.DestroyUnityObject(m_Root);
        }

        private void Clear()
        {
            m_NextID = 0;
            m_EvaluatedSegments.Clear();
            m_Regions.Clear();
        }

        public void Create(Input input, bool displayProgressBar)
        {
            m_Input = input;

            Clear();

            if (displayProgressBar)
            {
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Generating Region Data", $"Creating Regions...", 0.1f);
                if (cancel)
                {
                    return;
                }
            }
            CreateRegions(input.Regions);

            if (displayProgressBar)
            {
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Generating Region Data", $"Removing Region Vertices...", 0.2f);
                if (cancel)
                {
                    return;
                }
            }
            RemoveRegionsVertices(false);

            if (displayProgressBar)
            {
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Generating Region Data", $"Creating Region Control Points...", 0.3f);
                if (cancel)
                {
                    return;
                }
            }
            CreateRegionsControlPoints();
        }

        public void Generate(string folder, int lod,
            bool displayProgressBar,
            bool generateAssets,
            float layerWidth,
            float layerHeight,
            int horizontalTileCount,
            int verticalTileCount,
            Transform parent)
        {
            m_EvaluatedSegments.Clear();

            if (displayProgressBar)
            {
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Generating Region Data", $"Smoothing Region Vertices...", 0.5f);
                if (cancel)
                {
                    return;
                }
            }

            SmoothRegionsVertices(parent);

            if (generateAssets)
            {
                GenerateBorderAssets();
            }

            try
            {
                AssetDatabase.StartAssetEditing();

                EditorUtility.DisplayProgressBar("Generating Region Data", $"Triangulating Regions...", 0.7f);
                TriangulateRegions(folder, lod, displayProgressBar, generateAssets);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
        }

        private void GenerateBorderAssets()
        {
            foreach (var region in m_Regions)
            {
                var configID = m_Input.GetRegionConfigIDFunc(region.RegionID);
                var namePrefix = m_Input.GetRegionObjectNamePrefix("border", configID, 1);
                var gameObject = region.GetGameObject(Region.ObjectType.Border);
                //create material
                {
                    var renderer = gameObject.GetComponentInChildren<MeshRenderer>(true);
                    var material = Object.Instantiate(renderer.sharedMaterial);
                    var materialPath = $"{namePrefix}.mat";
                    AssetDatabase.CreateAsset(material, materialPath);
                    var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    renderer.sharedMaterial = newMaterial;
                }

                //create mesh
                {
                    var filter = gameObject.GetComponentInChildren<MeshFilter>(true);
                    var mesh = Object.Instantiate(filter.sharedMesh);
                    mesh.UploadMeshData(true);
                    var meshPath = $"{namePrefix}.asset";
                    AssetDatabase.CreateAsset(mesh, meshPath);
                    var newMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    filter.sharedMesh = newMesh;
                }

                //create prefab
                {
                    PrefabUtility.SaveAsPrefabAsset(gameObject, $"{namePrefix}.prefab");
                }
            }

            AssetDatabase.Refresh();
        }

        private void CreateRegions(List<RegionInput> regions)
        {
            Task<Region>[] tasks = new Task<Region>[regions.Count];
            //一个区域的所有格子必须是相连的，并且没有洞
            for (int i = 0; i < regions.Count; ++i)
            {
                int idx = i;
                var task = Task.Run(() =>
                {
                    var coord = regions[idx].Coordinates[0];
                    var region = CreateRegion(regions[idx].RegionID, coord.x, coord.y, regions[idx].Coordinates);
                    return region;
                });

                tasks[i] = task;
            }

            Task.WaitAll(tasks);

            for (int i = 0; i < tasks.Length; ++i)
            {
                m_Regions.Add(tasks[i].Result);
            }
        }

        //删除并且移动某些满足条件的顶点，让边缘线更平滑
        private List<Vector3> RemoveAndMoveVertices(Region t, List<Vector3> outline, int regionIndex)
        {
            List<Vector3> removedVertices = new();

            //创建一个包含更多信息的顶点列表，方便后续对这个列表进行删除操作
            List<VertexWrapper> wrappedVertices = new();
            for (int i = 0; i < outline.Count; ++i)
            {
                var v = new VertexWrapper
                {
                    Index = i,
                    Position = outline[i],
                    Removed = false
                };
                wrappedVertices.Add(v);
            }

            float distanceThreshold = m_Input.GridSize * m_Input.Settings.GridErrorThreshold;
            for (int i = 0; i < wrappedVertices.Count - 1; ++i)
            {
                if (!wrappedVertices[i].Removed)
                {
                    //删除与这个点i距离够近且满足一定条件的点
                    RemoveCloseEnoughVertices(t, wrappedVertices[i], i, wrappedVertices, distanceThreshold, regionIndex);
                }
            }

            for (int i = wrappedVertices.Count - 1; i >= 0; --i)
            {
                if (wrappedVertices[i].Moved)
                {
                    //将某些顶点移动,让边缘更圆滑
                    outline[i] += new Vector3(m_Input.GridSize * 0.5f, 0, m_Input.GridSize * 0.5f);
                }
            }

            //去掉已经删除的顶点
            for (int i = wrappedVertices.Count - 1; i >= 0; --i)
            {
                if (wrappedVertices[i].Removed)
                {
                    removedVertices.Add(wrappedVertices[i].Position);
                    outline.RemoveAt(i);
                }
            }

            return removedVertices;
        }

        //删除与点index够近且满足一些其他条件的点
        private void RemoveCloseEnoughVertices(Region t, VertexWrapper v, int index, List<VertexWrapper> vertices,
            float distanceThreshold, int regionIndex)
        {
            //遍历从index + 1开始的点
            int n = vertices.Count;
            for (int k = index + 1; k < vertices.Count; ++k)
            {
                if (vertices[k].Removed)
                {
                    //点k已经被删除，跳过
                    continue;
                }

                var d1 = vertices[k].Position - v.Position;
                var d2 = vertices[(k + 1) % n].Position - vertices[k].Position;
                //if (IsLeftTurnLH(d1, d2))
                //{
                //    //如果形成了left turn,则表示v, vertices[k], vertices[k+1]是内拐,vertices[k]是内部点,不删除，因为创建的边框会包围所有的outline点
                //    break;
                //}
                float dis = Vector3.Distance(v.Position, vertices[k].Position);
                bool satisfyRectangularCondition = true;
                if (dis <= distanceThreshold && satisfyRectangularCondition)
                {
                    //后面的区域不能删除前面区域的点，如果删除了会导致区域之间有接缝
                    if (!IsPreviousRegionVertex(vertices[k].Position, regionIndex))
                    {
                        bool isSharedEdgeEndPoint = IsSharedEdgeEndPoint(t, vertices[k].Position);
                        if (isSharedEdgeEndPoint)
                        {
                            break;
                        }
                        //删除k点
                        vertices[k].Removed = true;
                        //check if it is interior point
                        var coord0 = m_Input.FromPositionToCoordinate(v.Position);
                        var coord1 = m_Input.FromPositionToCoordinate(vertices[k].Position);
                        if (m_Input.GetRegionIDFunc(coord0.x, coord0.y) == m_Input.GetRegionIDFunc(coord1.x, coord1.y))
                        {
                            //v点和k点都是属于同一个区域，移动k点到格子中心
                            vertices[k].Moved = true;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        }

        //pos坐标的点是否是regionIndex之前的区域的点
        private bool IsPreviousRegionVertex(Vector3 pos, int regionIndex)
        {
            for (int i = regionIndex - 1; i >= 0; --i)
            {
                if (m_Regions[i].Outline.Contains(pos))
                {
                    return true;
                }
            }
            return false;
        }

        //是否是shared edge的最后一个点
        private bool IsSharedEdgeEndPoint(Region t, Vector3 pos)
        {
            var sharedEdges = t.SharedEdges;
            int n = sharedEdges.Count;
            for (int i = 0; i < n; ++i)
            {
                if (sharedEdges[i].IsEndPoint(pos))
                {
                    return true;
                }
            }
            return false;
        }

        private Region CreateRegion(int regionID, int x, int y, List<Vector2Int> coordinates)
        {
            //区域所有边缘格子的边的集合
            List<Edge> allEdges = new();
            //遍历找到一个区域中所有的边缘格子的边集合
            List<Vector2Int> stack = new();
            stack.Add(new Vector2Int(x, y));
            HashSet<Vector2Int> processed = new();
            Vector2Int[] offset = new Vector2Int[4]
            {
                new(0, 1),
                new(0, -1),
                new(1, 0),
                new(-1, 0),
            };
            while (stack.Count > 0)
            {
                var coord = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                processed.Add(coord);

                var edges = GetEdges(coord.x, coord.y, regionID);
                if (edges != null)
                {
                    allEdges.AddRange(edges);
                }

                for (int i = 0; i < 4; ++i)
                {
                    var neighbourCoord = coord + offset[i];
                    if (processed.Contains(neighbourCoord) == false && m_Input.GetRegionIDFunc(neighbourCoord.x, neighbourCoord.y) == regionID)
                    {
                        if (stack.Contains(neighbourCoord) == false)
                        {
                            stack.Add(neighbourCoord);
                        }
                    }
                }
            }

            //将这些格子的边合并成更长的边
            return Interconnect(allEdges, regionID, coordinates);
        }

        /*
         * Edge方向,是逆时针
         * 3 <- 2
         * |    |
         * 0 -> 1
         */
        private List<Edge> GetEdges(int x, int y, int regionID)
        {
            List<Edge> edges = null;
            Debug.Assert(regionID != 0);
            if (x >= 0 && x < m_Input.HorizontalGridCount && y >= 0 && y < m_Input.VerticalGridCount)
            {
                edges = new List<Edge>();
                int selfRegionID = m_Input.GetRegionIDFunc(x - 1, y);
                int neighbourRegionID = m_Input.GetRegionIDFunc(x + 1, y);
                int topRegionID = m_Input.GetRegionIDFunc(x, y + 1);
                int bottomRegionID = m_Input.GetRegionIDFunc(x, y - 1);
                if (selfRegionID != regionID)
                {
                    var leftEdge = new Edge(NextID, m_Input.FromCoordinateToPositionFunc(x, y + 1), m_Input.FromCoordinateToPositionFunc(x, y), regionID, selfRegionID);
                    edges.Add(leftEdge);
                }
                if (neighbourRegionID != regionID)
                {
                    var rightEdge = new Edge(NextID, m_Input.FromCoordinateToPositionFunc(x + 1, y), m_Input.FromCoordinateToPositionFunc(x + 1, y + 1), regionID, neighbourRegionID);
                    edges.Add(rightEdge);
                }
                if (topRegionID != regionID)
                {
                    var topEdge = new Edge(NextID, m_Input.FromCoordinateToPositionFunc(x + 1, y + 1), m_Input.FromCoordinateToPositionFunc(x, y + 1), regionID, topRegionID);
                    edges.Add(topEdge);
                }
                if (bottomRegionID != regionID)
                {
                    var bottomEdge = new Edge(NextID, m_Input.FromCoordinateToPositionFunc(x, y), m_Input.FromCoordinateToPositionFunc(x + 1, y), regionID, bottomRegionID);
                    edges.Add(bottomEdge);
                }
            }
            return edges;
        }

        //合并边
        private Region Interconnect(List<Edge> edges, int regionID, List<Vector2Int> regionCoordinates)
        {
            for (int i = 0; i < edges.Count; ++i)
            {
                if (edges[i].Removed == false)
                {
                    for (int j = edges.Count - 1; j >= 0; --j)
                    {
                        if (edges[j].Removed == false)
                        {
                            if (TryConnect(edges[i], edges[j]))
                            {
                                //i和j合并后，删除j
                                edges[j].Removed = true;
                            }
                        }
                    }
                }
            }

            //key is edge start position, value is edge
            Dictionary<Vector3, Edge> startPosToEdge = new();
            for (int i = edges.Count - 1; i >= 0; --i)
            {
                if (edges[i].Removed)
                {
                    edges.RemoveAt(i);
                }
                else
                {
                    startPosToEdge.Add(edges[i].Start, edges[i]);
                }
            }

            List<Vector3> outline = new()
            {
                edges[0].Start
            };
            var curEdge = edges[0];
            Vector3 end = curEdge.Start;
            //从相连的edge一直走，找到所有的outline顶点
            while (curEdge != null)
            {
                bool found = startPosToEdge.TryGetValue(curEdge.End, out Edge connectedEdge);
                Debug.Assert(found);
                curEdge = connectedEdge;
                outline.Add(connectedEdge.Start);
                if (connectedEdge.End == end)
                {
                    //又回到了起点，outline构建完毕
                    break;
                }
            }

            //根据region的edge创建region和相邻region之间共享的边信息，用于后续创建分段的edge mesh
            var sharedEdgesWithNeighbourRegion = CreateSharedEdgesWithNeighbourRegion(edges);
            var regionColor = m_Input.GetRegionColorFunc(regionID);
            var region = new Region(sharedEdgesWithNeighbourRegion, edges, outline, regionID, regionColor, regionCoordinates);
            return region;
        }

        //根据region的edge创建region和相邻region之间共享的边信息，用于后续创建分段的edge mesh
        private List<SharedEdgeWithNeighbourRegion> CreateSharedEdgesWithNeighbourRegion(List<Edge> edges)
        {
            List<SharedEdgeWithNeighbourRegion> sharedEdges = new();
            HashSet<int> processedEdges = new();
            Dictionary<Vector3, Edge> startPosToEdge = new();
            for (int i = edges.Count - 1; i >= 0; --i)
            {
                startPosToEdge.Add(edges[i].Start, edges[i]);
            }
            Dictionary<Vector3, Edge> endPosToEdge = new();
            for (int i = edges.Count - 1; i >= 0; --i)
            {
                endPosToEdge.Add(edges[i].End, edges[i]);
            }

            for (int e = 0; e < edges.Count; ++e)
            {
                if (processedEdges.Contains(edges[e].ID))
                {
                    continue;
                }
                List<Edge> stack = new()
                {
                    edges[e]
                };
                while (stack.Count > 0)
                {
                    Edge cur = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    processedEdges.Add(cur.ID);

                    //check edge which is connected to current edge's start position
                    endPosToEdge.TryGetValue(cur.Start, out Edge connectedToCurrentEdgeStartPoint);
                    if (!processedEdges.Contains(connectedToCurrentEdgeStartPoint.ID))
                    {
                        if (IsSharedEdge(cur, connectedToCurrentEdgeStartPoint))
                        {
                            stack.Add(connectedToCurrentEdgeStartPoint);
                            cur.ConnectedToStart = connectedToCurrentEdgeStartPoint;
                            connectedToCurrentEdgeStartPoint.ConnectedToEnd = cur;
                        }
                    }

                    //check edge which is connected to current edge's end position
                    startPosToEdge.TryGetValue(cur.End, out Edge connectedToCurrentEdgeEndPoint);
                    if (!processedEdges.Contains(connectedToCurrentEdgeEndPoint.ID))
                    {
                        if (IsSharedEdge(cur, connectedToCurrentEdgeEndPoint))
                        {
                            stack.Add(connectedToCurrentEdgeEndPoint);
                            cur.ConnectedToEnd = connectedToCurrentEdgeEndPoint;
                            connectedToCurrentEdgeEndPoint.ConnectedToStart = cur;
                        }
                    }
                }

                var connectedEdgeVertices = ConnectEdges(edges[e]);
                sharedEdges.Add(new SharedEdgeWithNeighbourRegion(connectedEdgeVertices, edges[e].SelfRegionID, edges[e].NeighbourRegionID));
            }

            return sharedEdges;
        }

        private bool IsSharedEdge(Edge a, Edge b)
        {
            return a.SelfRegionID == b.SelfRegionID &&
                a.NeighbourRegionID == b.NeighbourRegionID;
        }

        private bool IsLoop(Edge e)
        {
            Edge s = e;
            while (e != null)
            {
                e = e.ConnectedToStart;
                if (e == s)
                {
                    return true;
                }
            }
            return false;
        }

        //合并和e相连的所有边的顶点
        private List<Vector3> ConnectEdges(Edge e)
        {
            if (IsLoop(e))
            {
                //断开链表
                e.ConnectedToStart.ConnectedToEnd = null;
                e.ConnectedToStart = null;
            }

            Edge header = null;
            while (e != null)
            {
                if (e.ConnectedToStart == null)
                {
                    header = e;
                    break;
                }
                e = e.ConnectedToStart;
            }

            List<Vector3> edgeVertices = new();
            while (header != null)
            {
                edgeVertices.Add(header.Start);
                if (header.ConnectedToEnd == null)
                {
                    edgeVertices.Add(header.End);
                }
                header = header.ConnectedToEnd;
            }

            return edgeVertices;
        }

        //如果a和b可以合并，则合并成更长的边，并删除b
        private bool TryConnect(Edge a, Edge b)
        {
            if (a.End == b.End && a.Start == b.Start)
            {
                //ab是同一条边，跳过
                return false;
            }
            if (a.NeighbourRegionID != b.NeighbourRegionID)
            {
                //a，b边不是相邻的同一个区域，不能合并
                return false;
            }

            if (a.IsHorizontal != b.IsHorizontal)
            {
                //a，b不是同一个方向，不能合并
                return false;
            }
            bool connected = false;
            if (a.End == b.Start)
            {
                connected = true;
                a.End = b.End;
            }
            else if (a.Start == b.End)
            {
                a.Start = b.Start;
                connected = true;
            }
            else if (a.Start == b.Start)
            {
                a.Start = b.End;
                connected = true;
            }
            else if (a.End == b.End)
            {
                a.End = b.Start;
                connected = true;
            }
            return connected;
        }

        private float GetRandom(float min, float max)
        {
            float v = (float)m_Random.NextDouble();
            return min + v * (max - min);
        }

        private List<ControlPoint> CreateControlPoints(List<Vector3> outline, float ratio, float minTangentLength, float maxTangentLength, int regionID)
        {
            List<ControlPoint> controlPoints = new();
            int n = outline.Count;
            for (int i = 0; i < n; ++i)
            {
                var dir0 = outline[i] - outline[Helper.Mod(i - 1, n)];
                var dir1 = outline[i] - outline[Helper.Mod(i + 1, n)];
                var d = dir0.normalized + dir1.normalized;
                d.Normalize();
                //计算顶点的切线方向,因为outline是逆时针环绕，所以用右手坐标系算法
                var p = new Vector3(-d.z, 0, d.x);
                float randomAngle = 0;
                Quaternion q = Quaternion.Euler(0, randomAngle, 0);
                p = q * p;
                float r = ratio;
                float dis0 = Mathf.Clamp(dir0.magnitude * r, minTangentLength, maxTangentLength);
                float dis1 = Mathf.Clamp(dir1.magnitude * r, minTangentLength, maxTangentLength);

                bool isLeftTurn = Helper.IsLeftTurnLH(dir0, -dir1);
                if (!isLeftTurn)
                {
                    //切线方向取反
                    p = -p;
                }

                var tangent0 = outline[i] - p * dis0;
                var tangent1 = outline[i] + p * dis1;

                var controlPoint = new ControlPoint(outline[i], tangent0, tangent1);
                controlPoints.Add(controlPoint);
            }

            return controlPoints;
        }

        //生成曲线
        private List<Vector3> SmoothVertices(List<ControlPoint> controlPoints, float pointDeltaDistance, int maxPointCountInOneSegment)
        {
            List<Vector3> curvePoints = new();
            var result = Evaluate(controlPoints, pointDeltaDistance, maxPointCountInOneSegment);
            for (int i = 0; i < result.Count; ++i)
            {
                curvePoints.Add(result[i].Pos);
            }
            return curvePoints;
        }

        private List<EvaluatePoint> Evaluate(List<ControlPoint> controlPoints, float pointDeltaDistance, int maxPointCountInOneSegment)
        {
            List<EvaluatePoint> evaluatePoints = new();
            for (int i = 0; i < controlPoints.Count - 1; ++i)
            {
                //分段生成曲线
                var result = EvaluateSegment(controlPoints[i], controlPoints[i + 1], i + 1, pointDeltaDistance, maxPointCountInOneSegment);
                AddEvaluatePoints(evaluatePoints, result);
            }

            if (controlPoints.Count > 2)
            {
                //首尾相连
                var result = EvaluateSegment(controlPoints[controlPoints.Count - 1], controlPoints[0], 0, pointDeltaDistance, maxPointCountInOneSegment);
                AddEvaluatePoints(evaluatePoints, result);
                //remove last point which is same as first point
                evaluatePoints.RemoveAt(evaluatePoints.Count - 1);
            }

            return evaluatePoints;
        }

        private void AddEvaluatePoints(List<EvaluatePoint> allPoints, List<EvaluatePoint> pointsInOneSegment)
        {
            for (int i = 0; i < pointsInOneSegment.Count; ++i)
            {
                var pos = pointsInOneSegment[i].Pos;
                if (allPoints.Count == 0 || allPoints[^1].Pos != pos)
                {
                    allPoints.Add(new EvaluatePoint(pos));
                }
            }
        }

        private class SegmentEvaluateInfo
        {
            public SegmentEvaluateInfo(Vector3 start, Vector3 end, List<EvaluatePoint> points)
            {
                this.start = start;
                this.end = end;
                this.points = points;
            }

            public Vector3 start;
            public Vector3 end;
            public List<EvaluatePoint> points;
        }

        private List<EvaluatePoint> FindPointsReverse(Vector3 controlPointStart, Vector3 controlPointEnd)
        {
            lock (m_EvaluatedSegments)
            {
                for (int i = 0; i < m_EvaluatedSegments.Count; ++i)
                {
                    if (m_EvaluatedSegments[i].end == controlPointStart &&
                        m_EvaluatedSegments[i].start == controlPointEnd)
                    {
                        return Helper.GetReverseList(m_EvaluatedSegments[i].points);
                    }
                }
            }
            return null;
        }

        private List<EvaluatePoint> FindPoints(Vector3 controlPointStart, Vector3 controlPointEnd)
        {
            for (int i = 0; i < m_EvaluatedSegments.Count; ++i)
            {
                if (m_EvaluatedSegments[i].start == controlPointStart &&
                    m_EvaluatedSegments[i].end == controlPointEnd)
                {
                    return m_EvaluatedSegments[i].points;
                }
            }

            return FindPointsReverse(controlPointStart, controlPointEnd);
        }

        private List<EvaluatePoint> EvaluateSegment(ControlPoint start, ControlPoint end, int controlPointIndex, float pointDeltaDistance, int maxPointCountInOneSegment)
        {
            //先查找是否已经生成过了end到start之间的线段，注意这里顺序要取反，因为邻边的winding order是相反的
            List<EvaluatePoint> points = FindPointsReverse(start.Position, end.Position);
            if (points != null)
            {
                return points;
            }

            points = new List<EvaluatePoint>();

            float distance = Vector3.Distance(end.Position, start.Position);
            //计算需要插值多少个点
            int n = Mathf.Min(Mathf.CeilToInt(distance / pointDeltaDistance), maxPointCountInOneSegment);
            for (int i = 0; i < n; ++i)
            {
                float t = (float)i / (n - 1);
                var pos = SplineHelper.Bezier(start.Position, start.Tangent1, end.Tangent0, end.Position, t);
                points.Add(new EvaluatePoint(pos));
            }

            lock (m_EvaluatedSegments)
            {
                //保存该段evaluate的结果，共后续邻接区域的共享边使用
                m_EvaluatedSegments.Add(new SegmentEvaluateInfo(start.Position, end.Position, points));
            }
            return points;
        }

        private void RemoveRegionsVertices(bool checkError)
        {
            List<Task<List<Vector3>>> tasks = new();
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                int idx = i;
                var task = Task.Run(() =>
                {
                    var removedVertices = RemoveAndMoveVertices(m_Regions[idx], m_Regions[idx].Outline, idx);
                    if (checkError)
                    {
                        //删除self intersected points
                        var outline = new List<Vector3>();
                        outline.AddRange(m_Regions[idx].Outline);
                        PolygonRemoveSelfIntersection.Process(outline, out var removedSelfIntersectionVertices);
                        //removedVertices.AddRange(removedSelfIntersectionVertices);
                        if (removedSelfIntersectionVertices.Count > 0)
                        {
                            foreach (var pos in removedSelfIntersectionVertices)
                            {
                                Debug.LogError($"Found Self Intersection position at {pos}");
                            }
                        }
                    }
                    return removedVertices;
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            for (int i = 0; i < tasks.Count; ++i)
            {
                var removedVertices = tasks[i].Result;
                //删除其他region的顶点,还会删除shared edge的顶点
                for (int k = 0; k < m_Regions.Count; ++k)
                {
                    m_Regions[k].RemoveVertices(removedVertices);
                }
            }
        }

        //将outline的点转换成control point,为曲线生成做准备
        private void CreateRegionsControlPoints()
        {
            Task[] tasks = new Task[m_Regions.Count];
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                int idx = i;
                var task = Task.Run(() =>
                {
                    m_Regions[idx].ControlPoints = CreateControlPoints(m_Regions[idx].Outline, m_Input.Settings.SegmentLengthRatio, m_Input.Settings.MinTangentLength, m_Input.Settings.MaxTangentLength, m_Regions[idx].RegionID);

                    //将region的shared edge的顶点转换成control point的引用
                    m_Regions[idx].CreateSharedEdgeControlPoints();
                });

                tasks[i] = task;
            }

            Task.WaitAll(tasks);

#if false
            foreach (var region in m_Regions)
            {
                foreach (var cp in region.ControlPoints)
                {
                    var dp = new GameObject("Cp");
                    var c = dp.AddComponent<DrawControlPointInEditor>();
                    c.Set(cp.Position, cp.Tangent0, cp.Tangent1);
                }
            }
#endif
        }

        //根据region的control points生成曲线outline
        private void SmoothRegionsVertices(Transform parent)
        {
            Task[] tasks = new Task[m_Regions.Count];
            //smooth
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                int idx = i;
                var task = Task.Run(() =>
                {
                    m_Regions[idx].Outline = SmoothVertices(m_Regions[idx].ControlPoints, m_Input.Settings.PointDeltaDistance, m_Input.Settings.MaxPointCountInOneSegment);
                });

                tasks[i] = task;
            }

            Task.WaitAll(tasks);

            var totalVertexCount = 0;
            var totalIndexCount = 0;
            for (var i = 0; i < m_Regions.Count; ++i)
            {
                var borderGen = new RectangleBorderCreator();
                var param = new RectangleBorderCreator.CreateInfo()
                {
                    BorderMaterial = m_Input.Settings.EdgeMaterial,
                    MeshMaterial = m_Input.Settings.RegionMaterial,
                    Width = m_Input.Settings.LineWidth,
                    Name = $"LOD1 Border-{i}",
                    Parent = parent,
                    Center = m_Input.GetRegionCenterFunc(m_Regions[i].RegionID),
                };
                m_Regions[i].SetGameObject(Region.ObjectType.Border, borderGen.Generate(param, m_Regions[i].Outline, out var vertexCount, out var indexCount));
                totalVertexCount += vertexCount;
                totalIndexCount += indexCount;
            }

            Debug.LogError($"LOD1: vertex count: {totalVertexCount}, triangleCount: {totalIndexCount / 3}");

#if false
            foreach (var region in m_Regions)
            {
                var dp = new GameObject("region");
                var d = dp.AddComponent<DrawPolyLineInEditor>();
                d.SetVertices(region.Outline);
            }
#endif
        }

        public void HideLine()
        {
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                m_Regions[i].HideLine();
            }
        }

        public void HideMesh()
        {
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                m_Regions[i].HideMesh();
            }
        }

        public void ShowMesh()
        {
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                m_Regions[i].ShowMesh();
            }
        }

        public void ShowLine()
        {
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                m_Regions[i].ShowLine();
            }
        }

        private List<SegmentEvaluateInfo> m_EvaluatedSegments = new();
        private List<Region> m_Regions = new();
        private Input m_Input;
        private GameObject m_Root;
        private int m_NextID;
        private System.Random m_Random;
        private float m_Width;
        private float m_Height;
    }
}
