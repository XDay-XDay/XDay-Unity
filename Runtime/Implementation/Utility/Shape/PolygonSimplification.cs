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
using UnityEngine;

namespace XDay.UtilityAPI.Shape
{
    /// <summary>
    /// 简化Polygon顶点个数
    /// </summary>
    public class PolygonSimplification
    {
        public static List<Vector3> Process(List<Vector3> polygon, Parameter parameter = null)
        {
            parameter ??= new Parameter();

            var bounds = polygon.GetBounds2D();
            var size = Mathf.Max(bounds.width, bounds.height);

            var filteredPoints = FilterNearbyVertices(new List<Vector3>(polygon), 
                mergeDistance:size * parameter.NormalizedMergeDistanceThreshold, 
                parameter.LineMergeAngleThreshold,
                filterDistance: size * parameter.NormalizedRemoveDistanceThreshold);

            var simplifiedPolygon = FilterColinearVertices(filteredPoints, parameter.LineColinearAngleThreshold);

            return simplifiedPolygon;
        }

        static List<Vector3> FilterNearbyVertices(List<Vector3> polygon, float mergeDistance, float mergeLineAngle, float filterDistance)
        {
            bool filterVertex;
            do
            {
                filterVertex = false;
                var count = polygon.Count;
                for (var curIndex = 0; curIndex < count; ++curIndex)
                {
                    var nextIndex = Helper.Loop(curIndex + 1, count);
                    var nextEdgeDir = polygon[nextIndex] - polygon[curIndex];
                    nextEdgeDir.Normalize();

                    var lastIndex = Helper.Loop(curIndex - 1, count);
                    var lastEdgeDir = polygon[curIndex] - polygon[lastIndex];
                    var lastEdgeLength = lastEdgeDir.magnitude;
                    lastEdgeDir.Normalize();
                    
                    var angleBetweenEdges = Vector3.Angle(lastEdgeDir, nextEdgeDir);

                    if ((angleBetweenEdges <= mergeLineAngle && lastEdgeLength <= mergeDistance) ||
                        lastEdgeLength <= filterDistance)
                    {
                        polygon.RemoveAt(curIndex);

                        filterVertex = true;

                        break;
                    }
                }
            } while (filterVertex);

            return polygon;
        }
        
        static List<Vector3> FilterColinearVertices(List<Vector3> polygon, float lineColinearAngleThreshold)
        {
            bool filterVertex;
            do
            {
                filterVertex = false;
                var count = polygon.Count;
                for (var curIndex = 0; curIndex < count; ++curIndex)
                {
                    var lastIndex = Helper.Loop(curIndex - 1, count);
                    var lastEdgeDir = polygon[curIndex] - polygon[lastIndex];
                    lastEdgeDir.Normalize();

                    var nextIndex = Helper.Loop(curIndex + 1, count);
                    var nextEdgeDir = polygon[nextIndex] - polygon[curIndex];
                    nextEdgeDir.Normalize();

                    var angleBetweenEdges = Vector3.Angle(lastEdgeDir, nextEdgeDir);
                    if (angleBetweenEdges <= lineColinearAngleThreshold)
                    {
                        polygon.RemoveAt(curIndex);
                        filterVertex = true;
                        break;
                    }
                }
            }
            while (filterVertex);

            return polygon;
        }

        public class Parameter
        {
            /// <summary>
            /// 两个Edge角度小于该值代表共线
            /// </summary>
            public float LineColinearAngleThreshold = 18.0f;

            /// <summary>
            /// 两个相邻顶点距离小于该值代表可删除
            /// </summary>
            public float NormalizedRemoveDistanceThreshold = 0.06f;

            /// <summary>
            /// 以下两个参数组队使用
            /// </summary>
            public float LineMergeAngleThreshold = 22.0f;
            public float NormalizedMergeDistanceThreshold = 0.22f;
        }
    }
}
