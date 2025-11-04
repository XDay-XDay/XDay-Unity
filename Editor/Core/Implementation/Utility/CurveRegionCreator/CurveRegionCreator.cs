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
    public enum TangentMoveType
    {
        Free,
        Rotate,
        RotateAndScale,
    }

    public partial class CurveRegionCreator
    {
        public int NextID { get { return ++m_NextID; } }
        public List<EdgeAssetInfo> EdgeAssetsInfo { get { return m_EdgeAssetsInfo; } set { m_EdgeAssetsInfo = value; } }
        public List<Block> Blocks { get { return m_Blocks; } set { m_Blocks = value; } }
        public int LOD1MaskTextureWidth { get { return m_LOD1MaskTextureWidth; } set { m_LOD1MaskTextureWidth = value; } }
        public int LOD1MaskTextureHeight { get { return m_LOD1MaskTextureHeight; } set { m_LOD1MaskTextureHeight = value; } }
        public Color32[] LOD1MaskTextureData { get { return m_LOD1MaskTextureData; } set { m_LOD1MaskTextureData = value; } }

        public CurveRegionCreator(Transform parent, float width, float height)
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
            m_EdgeAssetsInfo.Clear();
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                m_Territories[i].OnDestroy();
            }
            m_Territories.Clear();
        }

        //创建线条
        public void Create(Input input, bool displayProgressBar)
        {
            m_Input = input;

            Clear();

            if (displayProgressBar)
            {
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Generating Region Data", $"Creating Territories...", 0.1f);
                if (cancel)
                {
                    return;
                }
            }
            CreateTerritories(input.Regions);

            if (displayProgressBar)
            {
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Generating Region Data", $"Removing Territory Vertices...", 0.2f);
                if (cancel)
                {
                    return;
                }
            }
            RemoveTerritoriesVerticesMultithread(false);

            if (displayProgressBar)
            {
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Generating Region Data", $"Creating Territory Control Points...", 0.3f);
                if (cancel)
                {
                    return;
                }
            }
            CreateTerritoriesControlPoints();
        }

        //生成mesh
        public void Generate(string folder, 
            int lod, 
            bool displayProgressBar, 
            bool generateAssets, 
            float layerWidth, 
            float layerHeight, 
            int horizontalTileCount, 
            int verticalTileCount, 
            int subLayerIndex, 
            List<TerritoryCentroidInfo> outCentroidInfo)
        {
            m_EvaluatedSegments.Clear();
            m_EdgeAssetsInfo.Clear();
            //mMeshies.Clear();
            //mPrefabs.Clear();
            //mMaterialPaths.Clear();

            if (displayProgressBar)
            {
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Generating Region Data", $"Smoothing Territory Vertices...", 0.5f);
                if (cancel)
                {
                    return;
                }
            }

            SmoothTerritoriesVertices();

            try
            {
                AssetDatabase.StartAssetEditing();

                EditorUtility.DisplayProgressBar("Generating Region Data", $"Triangulating Territories...", 0.7f);
                TriangulateTerritories(folder, lod, displayProgressBar, generateAssets, true);

                //合并被两个区域共享的两个edge为一个
                if (m_Input.Settings.MergeEdge)
                {
                    EditorUtility.DisplayProgressBar("Generating Region Data", $"Combining Territory Shared Edges...", 0.8f);
                    CombineTerritorySharedEdges(generateAssets, lod, false);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();

            //将同一个区域的所有edge合并为一条,包括mesh和数据,注意就算lod1的combine mesh也需要合并边,不然lod0和lod1边数不对会逻辑出错
            if (m_Input.Settings.CombineEdgesOfOneRegion && generateAssets)
            {
                CombineEdgesOfTerritories();
            }

            if (m_Input.Settings.CombineMesh && generateAssets)
            {
                EditorUtility.DisplayProgressBar("CombineTerritoryMesh", $"Please wait...", 0.7f);
                CombineTerritoryMesh(layerWidth, layerHeight, horizontalTileCount, verticalTileCount, generateAssets, folder, lod);
            }

            if (generateAssets && m_Input.Settings.GenerateUnityAssets == false)
            {
                Debug.Assert(false, "todo");
                string path = $"{folder}/region_lod{lod}.bytes";
                //SaveCustomAsset(path);
            }

            if (outCentroidInfo != null)
            {
                //if (pointGenerator != null)
                //{
                //    GenerateCustomPoints(bindingID, subLayerIndex, pointGenerator, outCentroidInfo);
                //}
                //else
                //{
                    GenerateTerritoryCentroid(outCentroidInfo);
                //}
            }
        }

        private void CreateTerritories(List<RegionInput> regions)
        {
            Task<Territory>[] tasks = new Task<Territory>[regions.Count];
            //一个区域的所有格子必须是相连的，并且没有洞
            for (int i = 0; i < regions.Count; ++i)
            {
                int idx = i;
                var task = Task.Run(() =>
                {
                    var coord = regions[idx].Coordinates[0];
                    var territory = CreateTerritory(regions[idx].RegionID, coord.x, coord.y, regions[idx].Coordinates);
                    return territory;
                });

                tasks[i] = task;
            }

            Task.WaitAll(tasks);

            for (int i = 0; i < tasks.Length; ++i)
            {
                m_Territories.Add(tasks[i].Result);
            }
        }

        //删除并且移动某些满足条件的顶点，让边缘线更平滑
        private List<Vector3> RemoveAndMoveVertices(Territory t, List<Vector3> outline, int territoryIndex)
        {
            List<Vector3> removedVertices = new();

            //创建一个包含更多信息的顶点列表，方便后续对这个列表进行删除操作
            List<VertexWrapper> wrappedVertices = new();
            for (int i = 0; i < outline.Count; ++i)
            {
                var v = new VertexWrapper();
                v.Index = i;
                v.Position = outline[i];
                v.Removed = false;
                wrappedVertices.Add(v);
            }

            float distanceThreshold = m_Input.GridSize * m_Input.Settings.GridErrorThreshold;
            for (int i = 0; i < wrappedVertices.Count - 1; ++i)
            {
                if (!wrappedVertices[i].Removed)
                {
                    //删除与这个点i距离够近且满足一定条件的点
                    RemoveCloseEnoughVertices(t, wrappedVertices[i], i, wrappedVertices, distanceThreshold, territoryIndex);
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
        private void RemoveCloseEnoughVertices(Territory t, VertexWrapper v, int index, List<VertexWrapper> vertices, float distanceThreshold, int territoryIndex)
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
                if (IsLeftTurnLH(d1, d2))
                {
                    //如果形成了left turn,则表示v, vertices[k], vertices[k+1]是内拐,vertices[k]是内部点,不删除，因为创建的边框会包围所有的outline点
                    break;
                }
                float dis = Vector3.Distance(v.Position, vertices[k].Position);
                bool satisfyRectangularCondition = true;
                if (m_Input.Settings.MoreRectangular)
                {
                    //k+1和k点的距离也小于distanceThreshold才删除,这样生成的边缘更多方形
                    if (k + 1 < vertices.Count)
                    {
                        float dis2 = Vector3.Distance(vertices[k + 1].Position, vertices[k].Position);
                        satisfyRectangularCondition = dis2 <= distanceThreshold;
                    }
                }

                if (dis <= distanceThreshold && satisfyRectangularCondition)
                {
                    //后面的区域不能删除前面区域的点，如果删除了会导致区域之间有接缝
                    if (!IsPreviousTerritoryVertex(vertices[k].Position, territoryIndex))
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

        //pos坐标的点是否是territoryIndex之前的区域的点
        private bool IsPreviousTerritoryVertex(Vector3 pos, int territoryIndex)
        {
            for (int i = territoryIndex - 1; i >= 0; --i)
            {
                if (m_Territories[i].Outline.Contains(pos))
                {
                    return true;
                }
            }
            return false;
        }

        //是否是shared edge的最后一个点
        private bool IsSharedEdgeEndPoint(Territory t, Vector3 pos)
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

        private Territory CreateTerritory(int regionID, int x, int y, List<Vector2Int> coordinates)
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
            return Interconnect(allEdges, regionID, m_Input.Settings.VertexDisplayRadius, coordinates);
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
        private Territory Interconnect(List<Edge> edges, int regionID, float vertexDisplayRadius, List<Vector2Int> regionCoordinates)
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

            List<Vector3> outline = new();
            outline.Add(edges[0].Start);
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

            if (m_Input.Settings.ShareEdge == false)
            {
                foreach (var edge in edges)
                {
                    edge.NeighbourRegionID = 0;
                }
            }

            //根据territory的edge创建territory和相邻territory之间共享的边信息，用于后续创建分段的edge mesh
            var sharedEdgesWithNeighbourTerritory = CreateSharedEdgesWithNeighbourTerritory(edges);
            var regionColor = m_Input.GetRegionColorFunc(regionID);
            var territory = new Territory(sharedEdgesWithNeighbourTerritory, edges, outline, regionID, regionColor, regionCoordinates);
            return territory;
        }

        //根据territory的edge创建territory和相邻territory之间共享的边信息，用于后续创建分段的edge mesh
        private List<SharedEdgeWithNeighbourTerritroy> CreateSharedEdgesWithNeighbourTerritory(List<Edge> edges)
        {
            List<SharedEdgeWithNeighbourTerritroy> sharedEdges = new();
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
                List<Edge> stack = new();

                stack.Add(edges[e]);
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
                //EditorUtils.CreateDrawLineStrip("rectangle shared edge vertices", connectedEdgeVertices, mInput.settings.vertexDisplayRadius);
                sharedEdges.Add(new SharedEdgeWithNeighbourTerritroy(connectedEdgeVertices, edges[e].SelfRegionID, edges[e].NeighbourRegionID));
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
                var dir0 = outline[i] - outline[Mod(i - 1, n)];
                var dir1 = outline[i] - outline[Mod(i + 1, n)];
                var d = dir0.normalized + dir1.normalized;
                d.Normalize();
                //计算顶点的切线方向,因为outline是逆时针环绕，所以用右手坐标系算法
                var p = new Vector3(-d.z, 0, d.x);
                //计算一个随机的切线旋转角度
				//去掉random的原因是lod0和lod1随机值不同,todo,根据种子来计算两个lod相同的随机值
                float randomAngle = 0;//GetRandom(-mInput.settings.tangentRandomRotationRange, mInput.settings.tangentRandomRotationRange);
                Quaternion q = Quaternion.Euler(0, randomAngle, 0);
                p = q * p;
                //ratio加一个随机值
				//去掉random的原因是lod0和lod1随机值不同,todo,根据种子来计算两个lod相同的随机值
                float r = ratio;//GetRandom(0, mInput.settings.segmentLengthRatioRandomRange);
                float dis0 = Mathf.Clamp(dir0.magnitude * r, minTangentLength, maxTangentLength);
                float dis1 = Mathf.Clamp(dir1.magnitude * r, minTangentLength, maxTangentLength);

                bool isLeftTurn = IsLeftTurnLH(dir0, -dir1);
                if (!isLeftTurn)
                {
                    //切线方向取反
                    p = -p;
                }

                var tangent0 = outline[i] - p * dis0;
                var tangent1 = outline[i] + p * dis1;

                var controlPoint = new ControlPoint(outline[i], tangent0, tangent1, $"{regionID}_{i}");
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
                if (allPoints.Count == 0 || allPoints[allPoints.Count - 1].Pos != pos)
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

        //删除符合某些条件的顶点
        private void RemoveTerritoriesVerticesSingleThread(bool checkError)
        {
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                var removedVertices = RemoveAndMoveVertices(m_Territories[i], m_Territories[i].Outline, i);
                if (checkError)
                {
                    //删除self intersected points
                    var outline = new List<Vector3>();
                    outline.AddRange(m_Territories[i].Outline);
                    PolygonRemoveSelfIntersection.Process(outline, out var removedSelfIntersectionVertices);
                    if (removedSelfIntersectionVertices.Count > 0)
                    {
                        foreach (var pos in removedSelfIntersectionVertices)
                        {
                            Debug.LogError($"Found Self Intersection position at {pos}");
                        }
                    }
                    //removedVertices.AddRange(removedSelfIntersectionVertices);
                }
                //删除其他territory的顶点,还会删除shared edge的顶点
                for (int k = 0; k < m_Territories.Count; ++k)
                {
                    m_Territories[k].RemoveVertices(removedVertices);
#if false
                    var objddd = new GameObject($"After Remove Vertices of preview region {m_Regions[k].outline.Count}");
                    var dp11 = objddd.AddComponent<DrawPolygon>();
                    dp11.radius = mInput.settings.vertexDisplayRadius;
                    dp11.SetVertices(m_Regions[k].outline);
#endif

                }

#if false
                var obj1 = new GameObject($"After Remove Vertices {m_Regions[i].regionID} {m_Regions[i].outline.Count}");
                var dp1 = obj1.AddComponent<DrawPolygon>();
                dp1.radius = mInput.settings.vertexDisplayRadius;
                dp1.SetVertices(m_Regions[i].outline);
#endif
            }
        }

        private void RemoveTerritoriesVerticesMultithread(bool checkError)
        {
            List<Task<List<Vector3>>> tasks = new();
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                int idx = i;
                var task = Task.Run(() => {
                    var removedVertices = RemoveAndMoveVertices(m_Territories[idx], m_Territories[idx].Outline, idx);
                    if (checkError)
                    {
                        //删除self intersected points
                        var outline = new List<Vector3>();
                        outline.AddRange(m_Territories[idx].Outline);
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
                //删除其他territory的顶点,还会删除shared edge的顶点
                for (int k = 0; k < m_Territories.Count; ++k)
                {
                    m_Territories[k].RemoveVertices(removedVertices);
                }
            }
        }

        //将outline的点转换成control point,为曲线生成做准备
        private void CreateTerritoriesControlPoints()
        {
            Task[] tasks = new Task[m_Territories.Count];
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                int idx = i;
                var task = Task.Run(() =>
                {
                    m_Territories[idx].ControlPoints = CreateControlPoints(m_Territories[idx].Outline, m_Input.Settings.SegmentLengthRatio, m_Input.Settings.MinTangentLength, m_Input.Settings.MaxTangentLength, m_Territories[idx].RegionID);

                    //将territory的shared edge的顶点转换成control point的引用
                    m_Territories[idx].CreateSharedEdgeControlPoints();
                });

                tasks[i] = task;
            }

            Task.WaitAll(tasks);

            //create control point game objects in main thread
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                var controlPoints = m_Territories[i].ControlPoints;
                foreach (var cp in controlPoints)
                {
                    cp.CreateGameObjects(m_Input.Settings.VertexDisplayRadius, m_Root.transform);
                }
            }
        }

        //根据territory的control points生成曲线outline
        private void SmoothTerritoriesVertices()
        {
            Task[] tasks = new Task[m_Territories.Count];
            //smooth
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                int idx = i;
                var task = Task.Run(()=> {
                    m_Territories[idx].Outline = SmoothVertices(m_Territories[idx].ControlPoints, m_Input.Settings.PointDeltaDistance, m_Input.Settings.MaxPointCountInOneSegment);
                });

                tasks[i] = task;
                
#if false
                var obj3 = new GameObject($"Curve Outline {m_Regions[i].regionID} {m_Regions[i].outline.Count}");
                var dp3 = obj3.AddComponent<DrawPolygon>();
                dp3.radius = mInput.settings.vertexDisplayRadius;
                dp3.SetVertices(m_Regions[i].outline);
                obj3.transform.SetParent(mRoot.transform, true);
                m_Regions[i].SetGameObject(Territory.ObjectType.CurveOutline, obj3);
#endif
            }

            Task.WaitAll(tasks);
        }

        public void HideLineAndMesh()
        {
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                m_Territories[i].HideLineAndMesh();
            }
        }

        public void HideLine()
        {
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                m_Territories[i].HideLine();   
            }
        }

        public void HideMesh()
        {
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                m_Territories[i].HideMesh();
            }
        }

        public void HideRegionMesh()
        {
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                m_Territories[i].HideRegionMesh();
            }
        }

        public void ShowMesh()
        {
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                m_Territories[i].ShowMesh();
            }
        }

        private List<SegmentEvaluateInfo> m_EvaluatedSegments = new();
        private List<Territory> m_Territories = new();
        private List<EdgeAssetInfo> m_EdgeAssetsInfo = new();
        private List<Block> m_Blocks = new();
        private Input m_Input;
        private GameObject m_Root;
        private int m_NextID;
        private int m_LOD1MaskTextureWidth;
        private int m_LOD1MaskTextureHeight;
        private Color32[] m_LOD1MaskTextureData;
        private System.Random m_Random;
        private float m_Width;
        private float m_Height;
    }
}
