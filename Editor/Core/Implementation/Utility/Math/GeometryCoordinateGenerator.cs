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

namespace XDay.UtilityAPI.Editor
{
    public static class GeometryCoordinateGenerator
    {
        public static List<Vector3> GenerateInCircle(int count, float radius, Vector3 center, float borderSize, float space, int tryCount = 150)
        {
            var coordinates = new List<Vector3>();
            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < tryCount; ++j)
                {
                    Vector3 coordinate;
                    if (borderSize > 0)
                    {
                        var rot = Mathf.Deg2Rad * Random.Range(0f, 360f);
                        coordinate = center + new Vector3(Mathf.Sin(rot), 0, Mathf.Cos(rot)) * Random.Range(radius - borderSize * 0.5f, radius + borderSize * 0.5f);
                    }
                    else
                    {
                        coordinate = center + (Random.insideUnitCircle * radius).ToVector3XZ();
                    }

                    if (!Helper.CheckOverlap(coordinates, coordinate, space))
                    {
                        coordinates.Add(coordinate);
                        break;
                    }
                }
            }
            return coordinates;
        }

        public static List<Vector3> GenerateInLine(int count, Vector3 start, Vector3 end, float borderSize, float space, bool equalSpace, int tryCount = 150)
        {
            if (equalSpace)
            {
                var dir = end - start;
                var length = dir.magnitude;
                dir /= length;
                count = Mathf.Min(count, Mathf.FloorToInt(length / space));
                var coordinates = new List<Vector3>();
                for (var i = 0; i < count; i++)
                {
                    coordinates.Add(start + space * i * dir);
                }
                return coordinates;
            }
            else
            {
                var coordinates = new List<Vector3>();
                for (var i = 0; i < count; i++)
                {
                    for (var j = 0; j < tryCount; ++j)
                    {
                        var range = Random.Range(0.0f, 1.0f);
                        var dir = end - start;
                        var point = start + range * dir;
                        dir.Normalize();
                        point += Random.Range(-borderSize * 0.5f, borderSize * 0.5f) * new Vector3(-dir.z, 0, dir.x);

                        if (!Helper.CheckOverlap(coordinates, point, space))
                        {
                            coordinates.Add(point);
                            break;
                        }
                    }
                }
                return coordinates;
            }
        }

        public static List<Vector3> GenerateInRectangle(int count, Vector3 center, float rectWidth, float rectHeight, float borderSize, float space, int tryCount = 150)
        {
            var minX = center.x - rectWidth * 0.5f;
            var minZ = center.z - rectHeight * 0.5f;
            var maxX = center.x + rectWidth * 0.5f;
            var maxZ = center.z + rectHeight * 0.5f;
            var coordinates = new List<Vector3>();
            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < tryCount; ++j)
                {
                    Vector3 coordinate;
                    if (borderSize > 0)
                    {
                        coordinate = Vector3.zero;
                        var range = Random.Range(0, (rectWidth + rectHeight) * 2);
                        var xOffset = (rectWidth - borderSize) * 0.5f;
                        var yOffset = (rectHeight - borderSize) * 0.5f;
                        if (range <= rectWidth)
                        {
                            coordinate.x = -xOffset - Random.Range(0, borderSize);
                            coordinate.z = Random.Range(-rectHeight * 0.5f, rectHeight * 0.5f);
                        }
                        else if (range <= rectWidth * 2f)
                        {
                            coordinate.x = xOffset + Random.Range(0, borderSize);
                            coordinate.z = Random.Range(-rectHeight * 0.5f, rectHeight * 0.5f);
                        }
                        else if (range <= (rectWidth * 2f + rectHeight))
                        {
                            coordinate.x = Random.Range(-rectWidth * 0.5f, rectWidth * 0.5f);
                            coordinate.z = yOffset + Random.Range(0, borderSize);
                        }
                        else
                        {
                            coordinate.x = Random.Range(-rectWidth * 0.5f, rectWidth * 0.5f);
                            coordinate.z = -yOffset - Random.Range(0, borderSize);
                        }

                        coordinate += center;
                    }
                    else
                    {
                        coordinate = new Vector3(Random.Range(minX, maxX), center.y, Random.Range(minZ, maxZ));
                    }

                    if (!Helper.CheckOverlap(coordinates, coordinate, space))
                    {
                        coordinates.Add(coordinate);
                        break;
                    }
                }
            }
            return coordinates;
        }

        public static List<Vector3> GenerateInPolygon(int count, List<Vector3> polygon, float borderSize, float space, int tryCount = 150)
        {
            var coordinates = new List<Vector3>();
            if (borderSize <= 0)
            {
                if (polygon.Count > 0)
                {
                    if (PolygonTriangulator.Triangulate(polygon, out var triangleVertices, out var triangleIndices))
                    {
                        var triangleCount = triangleIndices.Length / 3;
                        var coordinatesInEachTriangle = new List<Vector3>[triangleCount];
                        for (var i = 0; i <triangleCount; ++i)
                        {
                            coordinatesInEachTriangle[i] = new();
                        }
                        List<float> triangleAreas = new(triangleCount);

                        var triangle = new Vector3[3];
                        float area = 0;
                        for (var i = 0; i < triangleCount; i++)
                        {
                            SetTriangle(triangle, i, triangleVertices, triangleIndices);

                            var triangleArea = Helper.GetPolygonArea(triangle);
                            area += triangleArea;

                            triangleAreas.Add(triangleArea);
                        }

                        List<float> triangleAreasRatio = new(triangleCount);
                        float ratioTotalSoFar = 0;
                        for (var i = 0; i < triangleCount; i++)
                        {
                            ratioTotalSoFar += triangleAreas[i] / area;
                            triangleAreasRatio.Add(ratioTotalSoFar);
                        }

                        for (var i = 0; i < count; i++)
                        {
                            var seed = Random.Range(0f, 1f);
                            for (var ti = 0; ti < triangleCount; ti++)
                            {
                                if (seed <= triangleAreasRatio[ti])
                                {
                                    SetTriangle(triangle, ti, triangleVertices, triangleIndices);

                                    GenerateOneCoordinateInTriangle(triangle, ti, coordinatesInEachTriangle, coordinates, space, tryCount);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var chances = GenerateBorderChance(polygon);

                for (var i = 0; i < count; i++)
                {
                    for (var j = 0; j < tryCount; ++j)
                    {
                        var borderIndex = -1;
                        var seed = Random.Range(0f, 1f);
                        for (var border = 0; border < chances.Length; border++)
                        {
                            if (seed <= chances[border])
                            {
                                borderIndex = border;
                                break;
                            }
                        }
                        Debug.Assert(borderIndex >= 0);

                        var dir = polygon[(borderIndex + 1) % polygon.Count] - polygon[borderIndex];
                        var point = polygon[borderIndex] + Random.Range(0f, 1f) * dir;
                        dir.Normalize();
                        point += Random.Range(-borderSize * 0.5f, borderSize * 0.5f) * new Vector3(dir.z, 0, -dir.x);

                        if (!Helper.CheckOverlap(coordinates, point, space))
                        {
                            coordinates.Add(point);
                            break;
                        }
                    }
                }
            }
            return coordinates;
        }

        private static float[] GenerateBorderChance(List<Vector3> polygon)
        {
            var chances = new float[polygon.Count];
            var perimeter = 0f;
            for (var i = 0; i < polygon.Count; i++)
            {
                var dir = polygon[(i + 1) % polygon.Count] - polygon[i];
                perimeter += dir.magnitude;
                chances[i] = perimeter;
            }

            for (var i = 0; i < chances.Length; i++)
            {
                chances[i] /= perimeter;
            }

            return chances;
        }

        private static void SetTriangle(Vector3[] triangle, int index, Vector3[] triangleVertices, int[] triangleIndices)
        {
            for (var i = 0; i < 3; ++i)
            {
                triangle[i] = triangleVertices[triangleIndices[index * 3 + i]];
            }
        }

        private static void GenerateOneCoordinateInTriangle(Vector3[] vertices, int triangleIndex, List<Vector3>[] coordinatesInEachTriangle, List<Vector3> coordinates, float space, int tryCount)
        {
            for (var i = 0; i < tryCount; ++i)
            {
                var x = Random.Range(0f, 1f);
                var sx = Mathf.Sqrt(x);
                var y = Random.Range(0f, 1f);
                var coordinate = (1 - sx) * vertices[0] + sx * (1 - y) * vertices[1] + sx * y * vertices[2];
                foreach (var existPoint in coordinatesInEachTriangle[triangleIndex])
                {
                    if (coordinate == existPoint)
                    {
                        break;
                    }
                }

                if (!Helper.CheckOverlap(coordinates, coordinate, space))
                {
                    coordinates.Add(coordinate);
                    coordinatesInEachTriangle[triangleIndex].Add(coordinate);
                    break;
                }
            }
        }

    }
}


//XDay