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
using ClipperLib;

namespace XDay.UtilityAPI.Math
{
    public enum PolygonType
    {
        Hole,
        NoneHole,
        All,
    }

    public static class PolygonHelper
    {
        /// <summary>
        /// polygonA顶点中增加和polygonB的交点,并返回新的polygonA顶点
        /// </summary>
        /// <param name="polygonA"></param>
        /// <param name="polygonB"></param>
        /// <returns></returns>
        public static List<Vector3> AddIntersections(List<Vector3> polygonA, List<Vector3> polygonB)
        {
            List<Vector3> ret = new();
            int an = polygonA.Count;
            int bn = polygonB.Count;
            for (int i = 0; i < an; ++i)
            {
                var startA = polygonA[i];
                var endA = polygonA[(i + 1) % an];
                ret.Add(startA);
                for (int j = 0; j < bn; ++j)
                {
                    var startB = polygonB[j];
                    var endB = polygonB[(j + 1) % bn];
                    bool intersected = Helper.SegmentSegmentIntersectionTest(Helper.ToVector2(startA), Helper.ToVector2(endA), Helper.ToVector2(startB), Helper.ToVector2(endB), out Vector2 ip);
                    if (intersected)
                    {
                        ret.Add(Helper.ToVector3XZ(ip));
                    }
                }
            }
            ret = SortEdgeVertex(ret);
            return ret;
        }

        /// <summary>
        /// 扩大或缩小多边形
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="polygonVertices"></param>
        /// <returns></returns>
        public static List<List<Vector3>> ExpandPolygon(List<Vector3> polygonVertices, float radius, bool round = true)
        {
            if (radius == 0)
            {
                List<Vector3> copy = new();
                copy.AddRange(polygonVertices);
                return new List<List<Vector3>>() { copy };
            }

            List<IntPoint> path = new();
            for (int i = 0; i < polygonVertices.Count; ++i)
            {
                path.Add(new IntPoint(Helper.UpScale(polygonVertices[i].x), Helper.UpScale(polygonVertices[i].z)));
            }

            var upscaledRadius = Helper.UpScale(radius);
            ClipperOffset offsetPolygon = new()
            {
                ArcTolerance = Mathf.Abs(upscaledRadius) * 0.01f,
            };
            offsetPolygon.AddPath(path, round ? JoinType.jtRound : JoinType.jtSquare, EndType.etClosedPolygon);

            PolyTree result = new();
            offsetPolygon.Execute(ref result, upscaledRadius);
            var noneHoles = GetPolygonsFromPolyTree(result, PolygonType.NoneHole);
            var holes = GetPolygonsFromPolyTree(result, PolygonType.Hole);
            List<List<Vector3>> ret = new();
            for (int i = 0; i < noneHoles.Count; ++i)
            {
                ret.Add(noneHoles[i]);
            }

            for (var i = 0; i < ret.Count; ++i)
            {
                Helper.RemoveDuplicatedFast(ret[i]);
            }
#if false
            //temp code
            for (int i = 0; i < noneHoles.Count; ++i)
            {
                var obj = new GameObject($"extended none holes {i}");
                var dp = obj.AddComponent<DrawPolygon>();
                dp.SetVertices(noneHoles[i]);
            }

            for (int i = 0; i < holes.Count; ++i)
            {
                var obj = new GameObject($"extended holes {i}");
                var dp = obj.AddComponent<DrawPolygon>();
                dp.SetVertices(holes[i]);
            }
#endif
            return ret;
        }

        public static List<Vector3> GetPolygonLineIntersections(List<Vector3> pa, Vector3 start, Vector3 end)
        {
            Clipper clipper = new();
            // a polygon is closed
            clipper.AddPath(ConvertVector3ListToIntPointList(pa), PolyType.ptClip, true);
            // a line is open
            clipper.AddPath(ConvertVector3ListToIntPointList(new List<Vector3>() { start, end }), PolyType.ptSubject, false); 
            PolyTree soln = new(); // the solution is a tree
            clipper.Execute(ClipperLib.ClipType.ctIntersection, soln);

            var intersectedRegions = GetPolygonsFromPolyTree(soln, PolygonType.NoneHole);
            var holes = GetPolygonsFromPolyTree(soln, PolygonType.Hole);

            if (intersectedRegions.Count > 0)
            {
                return intersectedRegions[0];
            }

            return null;
        }

        public static List<Vector3> GetPolygonIntersections(List<Vector3> pa, List<Vector3> pb)
        {
            List<IntPoint> pathA = new();
            for (int i = 0; i < pa.Count; ++i)
            {
                pathA.Add(new IntPoint(Helper.UpScale(pa[i].x), Helper.UpScale(pa[i].z)));
            }

            List<IntPoint> pathB = new();
            for (int i = 0; i < pb.Count; ++i)
            {
                pathB.Add(new IntPoint(Helper.UpScale(pb[i].x), Helper.UpScale(pb[i].z)));
            }

            Clipper clipper = new();
            clipper.AddPath(pathB, PolyType.ptSubject, true);
            clipper.AddPath(pathA, PolyType.ptClip, true);
            List<List<IntPoint>> intersections = new();
            bool succeeded = clipper.Execute(ClipperLib.ClipType.ctIntersection, intersections, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            Debug.Assert(succeeded);

            List<Vector3> ret = new();
            if (intersections.Count == 0)
            {
                return ret;
            }
            if (intersections.Count != 1)
            {
                Debug.Log("intersections: " + intersections.Count);
            }

            for (int i = 0; i < intersections.Count; ++i)
            {
                var list = intersections[i];
                for (int j = 0; j < list.Count; ++j)
                {
                    ret.Add(new Vector3((float)Helper.DownScale(intersections[i][j].X), 0, (float)Helper.DownScale(intersections[i][j].Y)));
                }
            }

            return ret;
        }

        /// <summary>
        /// 计算一个矩形区域与一组多边形的交集
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minZ"></param>
        /// <param name="maxX"></param>
        /// <param name="maxZ"></param>
        /// <param name="polygons"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        public static List<List<Vector3>> GetPolygonIntersections(float minX, float minZ, float maxX, float maxZ, List<List<Vector3>> polygons, out List<List<Vector3>> holes)
        {
            var pathA = CreateBoundsPath(minX, minZ, maxX, maxZ);
            var pathsB = ConvertVector3ListListToIntPointListList(polygons);

            Clipper clipper = new();
            clipper.AddPath(pathA, PolyType.ptSubject, true);
            clipper.AddPaths(pathsB, PolyType.ptClip, true);
            PolyTree result = new();
            bool succeeded = clipper.Execute(ClipperLib.ClipType.ctIntersection, result, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            Debug.Assert(succeeded);

            var intersectedRegions = GetPolygonsFromPolyTree(result, PolygonType.NoneHole);
            holes = GetPolygonsFromPolyTree(result, PolygonType.Hole);
            return intersectedRegions;
        }

        /// <summary>
        /// 合并所有的多边形
        /// </summary>
        /// <param name="polygons"></param>
        /// <param name="extentSize"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        public static List<List<Vector3>> GetPolygonsUnion(List<List<Vector3>> polygons, float extentSize, out List<List<Vector3>> holes)
        {
            holes = new List<List<Vector3>>();
            List<List<Vector3>> combinedResult = new();
            if (polygons.Count > 0)
            {
                Clipper clipper = new();

                List<List<Vector3>> noneHoles = new();

                List<List<IntPoint>> paths = new();
                for (int i = 0; i < polygons.Count; ++i)
                {
                    List<IntPoint> p = ConvertPolygon(polygons[i]);
                    if (p.Count > 0)
                    {
                        if (extentSize > 0)
                        {
                            //扩展障碍物多边形
                            ClipperOffset offsetPolygon = new();
                            offsetPolygon.AddPath(p, JoinType.jtSquare, EndType.etClosedLine);
                            List<List<IntPoint>> extended = new();
                            offsetPolygon.Execute(ref extended, Helper.UpScale(extentSize));
                            p = extended[0];
                        }
                        paths.Add(p);
                    }
                }
                clipper.AddPaths(paths, PolyType.ptSubject, true);

                //这里算出的solution包括了noneHoles和holes
                //List<List<IntPoint>> solution = new List<List<IntPoint>>();
                //clipper.Execute(ClipperLib.ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
                //var ret = solution;

                PolyTree combinedTree = new();
                bool succeeded = clipper.Execute(ClipType.ctUnion, combinedTree, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
                combinedResult = GetPolygonsFromPolyTree(combinedTree, PolygonType.NoneHole);
                holes.AddRange(GetPolygonsFromPolyTree(combinedTree, PolygonType.Hole));
                noneHoles.AddRange(combinedResult);
                return noneHoles;
#if false
                //debug code
                for (int i = 0; i < combinedResult.Count; ++i)
                {
                    var obj = new GameObject("none hole " + i);
                    var dp = obj.AddComponent<DrawPolygon>();
                    dp.SetVertices(combinedResult[i]);
                }
                for (int i = 0; i < holes.Count; ++i)
                {
                    var obj = new GameObject("hole " + i);
                    var dp = obj.AddComponent<DrawPolygon>();
                    dp.SetVertices(holes[i]);
                }
#endif
            }
            return combinedResult;
        }

        /// <summary>
        /// return a - b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="noneHoles"></param>
        /// <param name="holes"></param>
        public static void GetPolygonDifference(List<Vector3> a, List<Vector3> b, out List<List<Vector3>> noneHoles, out List<List<Vector3>> holes)
        {
            Clipper clipper = new();
            clipper.AddPath(ConvertPolygon(a), PolyType.ptSubject, true);
            clipper.AddPath(ConvertPolygon(b), PolyType.ptClip, true);
            PolyTree result = new();
            clipper.Execute(ClipperLib.ClipType.ctDifference, result);
            noneHoles = GetPolygonsFromPolyTree(result, PolygonType.NoneHole);
            holes = GetPolygonsFromPolyTree(result, PolygonType.Hole);
        }

        public static void GetPolygonDifference(List<Vector3> a, List<List<Vector3>> b, out List<List<Vector3>> noneHoles, out List<List<Vector3>> holes, PolyFillType subjType, PolyFillType clipType)
        {
            Clipper clipper = new();
            clipper.AddPath(ConvertPolygon(a), PolyType.ptSubject, true);
            clipper.AddPaths(ConvertVector3ListListToIntPointListList(b), PolyType.ptClip, true);
            PolyTree result = new();
            clipper.Execute(ClipType.ctDifference, result, subjType, clipType);
            noneHoles = GetPolygonsFromPolyTree(result, PolygonType.NoneHole);
            holes = GetPolygonsFromPolyTree(result, PolygonType.Hole);
        }

        /// <summary>
        /// return a - b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="noneHoles"></param>
        /// <param name="holes"></param>
        public static void GetPolygonDifference(List<List<Vector3>> a, List<List<Vector3>> b, out List<List<Vector3>> noneHoles, out List<List<Vector3>> holes)
        {
            Clipper clipper = new();
            clipper.AddPaths(ConvertVector3ListListToIntPointListList(a), PolyType.ptSubject, true);
            clipper.AddPaths(ConvertVector3ListListToIntPointListList(b), PolyType.ptClip, true);
            PolyTree result = new();
            clipper.Execute(ClipperLib.ClipType.ctDifference, result);
            noneHoles = GetPolygonsFromPolyTree(result, PolygonType.NoneHole);
            holes = GetPolygonsFromPolyTree(result, PolygonType.Hole);
        }

        public static void GetPolygonDifference(float minX, float minZ, float maxX, float maxZ, List<List<Vector3>> obstacles, out List<List<Vector3>> noneHoles, out List<List<Vector3>> holes, PolyFillType subjType = PolyFillType.pftNonZero, PolyFillType clipType = PolyFillType.pftNonZero)
        {
            Clipper clipper = new();
            var boundsPath = CreateBoundsPath(minX, minZ, maxX, maxZ);
            clipper.AddPath(boundsPath, PolyType.ptSubject, true);
            clipper.AddPaths(ConvertVector3ListListToIntPointListList(obstacles), PolyType.ptClip, true);
            PolyTree result = new();
            clipper.Execute(ClipperLib.ClipType.ctDifference, result, subjType, clipType);
            noneHoles = GetPolygonsFromPolyTree(result, PolygonType.NoneHole);
            holes = GetPolygonsFromPolyTree(result, PolygonType.Hole);
        }

        /// <summary>
        /// 计算一个矩形区域与一组三角形的差集
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minZ"></param>
        /// <param name="maxX"></param>
        /// <param name="maxZ"></param>
        /// <param name="triangleIndices"></param>
        /// <param name="triangleVertices"></param>
        /// <param name="noneHoles"></param>
        /// <param name="holes"></param>
        public static void GetPolygonsDifference(float minX, float minZ, float maxX, float maxZ, List<Vector3Int> triangleIndices, Vector3[] triangleVertices, out List<List<Vector3>> noneHoles, out List<List<Vector3>> holes)
        {
            Clipper clipper = new();
            var boundsPath = CreateBoundsPath(minX, minZ, maxX, maxZ);
            clipper.AddPath(boundsPath, PolyType.ptSubject, true);

            List<List<IntPoint>> obstaclePaths = new();
            for (int i = 0; i < triangleIndices.Count; ++i)
            {
                var obstaclePath = CreateTrianglePath(triangleIndices[i], triangleVertices);
                obstaclePaths.Add(obstaclePath);
            }

            clipper.AddPaths(obstaclePaths, PolyType.ptClip, true);
            //减去tile中的三角形,得到剩下的空间
            PolyTree differencePolyTree = new();
            bool succeeded = clipper.Execute(ClipperLib.ClipType.ctDifference, differencePolyTree, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            Debug.Assert(succeeded);

            noneHoles = GetPolygonsFromPolyTree(differencePolyTree, PolygonType.NoneHole);
            holes = GetPolygonsFromPolyTree(differencePolyTree, PolygonType.Hole);
        }

        /// <summary>
        /// 计算一组多边形与一组三角形的差集
        /// </summary>
        /// <param name="polygons"></param>
        /// <param name="triangleIndices"></param>
        /// <param name="triangleVertices"></param>
        /// <param name="noneHoles"></param>
        /// <param name="holes"></param>
        public static void GetPolygonsDifference(List<List<Vector3>> polygons, List<Vector3Int> triangleIndices, Vector3[] triangleVertices, out List<List<Vector3>> noneHoles, out List<List<Vector3>> holes)
        {
            noneHoles = new List<List<Vector3>>();
            holes = new List<List<Vector3>>();

            if (polygons.Count == 0)
            {
                return;
            }

            Clipper clipper = new();
            clipper.AddPaths(ConvertVector3ListListToIntPointListList(polygons), PolyType.ptSubject, true);

            List<List<IntPoint>> obstaclePaths = new();
            for (int i = 0; i < triangleIndices.Count; ++i)
            {
                var obstaclePath = CreateTrianglePath(triangleIndices[i], triangleVertices);
                obstaclePaths.Add(obstaclePath);
            }

            clipper.AddPaths(obstaclePaths, PolyType.ptClip, true);
            //减去tile中的三角形,得到剩下的空间
            PolyTree differencePolyTree = new();
            bool succeeded = clipper.Execute(ClipperLib.ClipType.ctDifference, differencePolyTree, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            if (!succeeded)
            {
                Debug.Assert(false, "GetPolygonsDifference failed!");
            }

            noneHoles = GetPolygonsFromPolyTree(differencePolyTree, PolygonType.NoneHole);
            holes = GetPolygonsFromPolyTree(differencePolyTree, PolygonType.Hole);
        }

        /// <summary>
        /// return polygon - obstacles
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="obstacles"></param>
        /// <param name="noneHoles"></param>
        /// <param name="holes"></param>
        /// <param name="subjType"></param>
        /// <param name="clipType"></param>
        public static void GetPolygonsDifference(List<Vector3> polygon, List<List<Vector3>> obstacles, out List<List<Vector3>> noneHoles, out List<List<Vector3>> holes, PolyFillType subjType = PolyFillType.pftNonZero, PolyFillType clipType = PolyFillType.pftNonZero)
        {
            noneHoles = new List<List<Vector3>>();
            holes = new List<List<Vector3>>();
            if (obstacles.Count == 0)
            {
                noneHoles.Add(polygon);
            }
            else
            {
                Clipper clipper = new();
                var polygonPath = ConvertPolygon(polygon);
                var obstaclePaths = ConvertVector3ListListToIntPointListList(obstacles);
                clipper.AddPath(polygonPath, PolyType.ptSubject, true);
                clipper.AddPaths(obstaclePaths, PolyType.ptClip, true);
                //减去tile中的三角形,得到剩下的空间
                PolyTree differencePolyTree = new();
                bool succeeded = clipper.Execute(ClipperLib.ClipType.ctDifference, differencePolyTree, subjType, clipType);
                Debug.Assert(succeeded);
                noneHoles = PolygonHelper.GetPolygonsFromPolyTree(differencePolyTree, PolygonType.NoneHole);
                holes = PolygonHelper.GetPolygonsFromPolyTree(differencePolyTree, PolygonType.Hole);
            }
        }

        /// <summary>
        /// return polygon - obstacles
        /// </summary>
        /// <param name="polygons"></param>
        /// <param name="obstacles"></param>
        /// <param name="noneHoles"></param>
        /// <param name="holes"></param>
        public static void GetPolygonsDifference(List<List<Vector3>> polygons, List<List<Vector3>> obstacles, out List<List<Vector3>> noneHoles, out List<List<Vector3>> holes)
        {
            noneHoles = new List<List<Vector3>>();
            holes = new List<List<Vector3>>();
            if (obstacles.Count == 0)
            {
                noneHoles.AddRange(polygons);
            }
            else
            {
                Clipper clipper = new();
                var polygonPaths = ConvertVector3ListListToIntPointListList(polygons);
                var obstaclePaths = ConvertVector3ListListToIntPointListList(obstacles);
                clipper.AddPaths(polygonPaths, PolyType.ptSubject, true);
                clipper.AddPaths(obstaclePaths, PolyType.ptClip, true);
                //减去tile中的三角形,得到剩下的空间
                PolyTree differencePolyTree = new();
                bool succeeded = clipper.Execute(ClipperLib.ClipType.ctDifference, differencePolyTree, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
                Debug.Assert(succeeded);
                noneHoles = GetPolygonsFromPolyTree(differencePolyTree, PolygonType.NoneHole);
                holes = GetPolygonsFromPolyTree(differencePolyTree, PolygonType.Hole);
            }
        }

        private static List<Vector3> ConvertIntPointToVector3List(List<IntPoint> list)
        {
            List<Vector3> ret = new(list.Count);
            for (int i = 0; i < list.Count; ++i)
            {
                ret.Add(new Vector3((float)Helper.DownScale(list[i].X), 0, (float)Helper.DownScale(list[i].Y)));
            }
            return ret;
        }

        private static List<IntPoint> ConvertVector3ListToIntPointList(List<Vector3> list)
        {
            List<IntPoint> ret = new();
            for (int j = 0; j < list.Count; ++j)
            {
                ret.Add(new IntPoint(Helper.UpScale(list[j].x), Helper.UpScale(list[j].z)));
            }
            return ret;
        }

        private static List<List<IntPoint>> ConvertVector3ListListToIntPointListList(List<List<Vector3>> list)
        {
            List<List<IntPoint>> ret = new(list.Count);
            for (int i = 0; i < list.Count; ++i)
            {
                int n = list[i].Count;
                var innerList = new List<IntPoint>(n);
                for (int j = 0; j < n; ++j)
                {
                    innerList.Add(new IntPoint(Helper.UpScale(list[i][j].x), Helper.UpScale(list[i][j].z)));
                }
                ret.Add(innerList);
            }
            return ret;
        }

        private static List<IntPoint> ConvertPolygon(List<Vector3> polygon)
        {
            List<IntPoint> p = new();
            for (int i = 0; i < polygon.Count; ++i)
            {
                p.Add(new IntPoint(Helper.UpScale(polygon[i].x), Helper.UpScale(polygon[i].z)));
            }
            return p;
        }

        private static List<IntPoint> CreateBoundsPath(float minX, float minZ, float maxX, float maxZ)
        {
            var list = new List<IntPoint>()
            {
                new(Helper.UpScale(minX), Helper.UpScale(minZ)),
                new(Helper.UpScale(maxX), Helper.UpScale(minZ)),
                new(Helper.UpScale(maxX), Helper.UpScale(maxZ)),
                new(Helper.UpScale(minX), Helper.UpScale(maxZ)),
            };
            return list;
        }

        //see http://www.angusj.com/delphi/clipper/documentation/Docs/Units/ClipperLib/Classes/PolyTree/_Body.htm
        private static List<List<Vector3>> GetPolygonsFromPolyTree(PolyTree tree, PolygonType type)
        {
            List<List<Vector3>> polygons = new();

            PolyNode polynode = tree.GetFirst();
            while (polynode != null)
            {
                if ((type == PolygonType.Hole || type == PolygonType.All) && polynode.IsHole)
                {
                    polygons.Add(ConvertIntPointToVector3List(polynode.Contour));
                }
                else if ((type == PolygonType.All || type == PolygonType.NoneHole) && polynode.IsHole == false)
                {
                    polygons.Add(ConvertIntPointToVector3List(polynode.Contour));
                }
                polynode = polynode.GetNext();
            }

            return polygons;
        }

        private static List<IntPoint> CreateTrianglePath(Vector3Int triangle, Vector3[] triangleVertices)
        {
            var list = new List<IntPoint>()
            {
                new(Helper.UpScale(triangleVertices[triangle.x].x), Helper.UpScale(triangleVertices[triangle.x].z)),
                new(Helper.UpScale(triangleVertices[triangle.y].x), Helper.UpScale(triangleVertices[triangle.y].z)),
                new(Helper.UpScale(triangleVertices[triangle.z].x), Helper.UpScale(triangleVertices[triangle.z].z)),
            };
            return list;
        }

        private static bool IsOnEdge(Vector3 vert, Vector3 start, Vector3 dir)
        {
            float angle = Vector3.Angle((vert - start).normalized, dir);
            return Mathf.Approximately(angle, 0);
        }

        private static List<Vector3> SortEdgeVertex(List<Vector3> vertices)
        {
            List<Vector3> loop = new();
            loop.AddRange(vertices);
            loop.Add(vertices[0]);

            List<Vertex> onEdgeVertices = new();
            for (int i = 0; i < loop.Count;)
            {
                onEdgeVertices.Clear();
                Vector3 dir = Vector3.zero;
                var start = loop[i];
                bool broke = false;
                for (int j = i + 1; j < loop.Count; ++j)
                {
                    var vert = loop[j];
                    if (dir == Vector3.zero || IsOnEdge(vert, start, dir))
                    {
                        onEdgeVertices.Add(new Vertex(j, vert, Vector3.Distance(vert, start)));
                    }
                    else
                    {
                        broke = true;
                        i = j - 1;
                        break;
                    }
                    if (dir == Vector3.zero)
                    {
                        dir = vert - start;
                        dir.Normalize();
                    }
                }

                onEdgeVertices.Sort(new SortVertex());
                int minIndex = GetMinIndex(onEdgeVertices);
                for (int k = 0; k < onEdgeVertices.Count - 1; ++k)
                {
                    loop[minIndex] = onEdgeVertices[k].position;
                    ++minIndex;
                }
                if (!broke)
                {
                    break;
                }
            }

            loop.RemoveAt(loop.Count - 1);
            return loop;
        }

        private static int GetMinIndex(List<Vertex> onEdgeVertices)
        {
            int minIndex = int.MaxValue;
            for (int i = 0; i < onEdgeVertices.Count; ++i)
            {
                if (onEdgeVertices[i].index < minIndex)
                {
                    minIndex = onEdgeVertices[i].index;
                }
            }
            return minIndex;
        }

        private class Vertex
        {
            public Vertex(int index, Vector3 pos, float distance)
            {
                this.index = index;
                position = pos;
                distanceToStart = distance;
            }
            public int index;
            public Vector3 position;
            public float distanceToStart;
        }

        private class SortVertex : IComparer<Vertex>
        {
            public int Compare(Vertex x, Vertex y)
            {
                if (x.distanceToStart < y.distanceToStart)
                {
                    return -1;
                }
                else if (x.distanceToStart > y.distanceToStart)
                {
                    return 1;
                }
                return 0;
            }
        }
    }
}
