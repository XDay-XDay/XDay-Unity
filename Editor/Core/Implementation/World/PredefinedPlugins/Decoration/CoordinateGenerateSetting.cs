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

namespace XDay.WorldAPI.Decoration.Editor
{
    internal enum GeometryType
    {
        Polygon,
        Line,
        Rectangle,
        Circle,
    }

    internal class CoordinateGenerateSetting
    {
        public int Count { set; get; } = 10;
        public float Space { get; set; } = 1;
        public float BorderSize { set; get; } = 0;
        public float CircleRadius { set; get; } = 10;
        public float RectWidth { set; get; } = 5;
        public float RectHeight { set; get; } = 5;
        public bool Random { set; get; } = false;
        public bool LineEquidistant { get; set; } = false;
    }

    internal class CoordinateGenerateOperation
    {
        public CoordinateGenerateOperation(Vector3 center, float circlrRadius)
        {
            Center = center;
            CircleRadius = circlrRadius;
            Shape = GeometryType.Circle;
        }

        public CoordinateGenerateOperation(Vector3 center, float rectWidth, float rectHeight)
        {
            Center = center;
            RectWidth = rectWidth;
            RectHeight = rectHeight;
            Shape = GeometryType.Rectangle;
        }

        public CoordinateGenerateOperation(Vector3 lineStart, Vector3 lineEnd)
        {
            LineStart = lineStart;
            LineEnd = lineEnd;
            Shape = GeometryType.Line;
        }

        public CoordinateGenerateOperation(List<Vector3> polygon)
        {
            Polygon = new(polygon);
            Shape = GeometryType.Polygon;
        }

        public Vector3 Center { get; }
        public float CircleRadius { get; }
        public float RectWidth { get; }
        public float RectHeight { get; }
        public Vector3 LineStart { get; }
        public Vector3 LineEnd { get; }
        public List<Vector3> Polygon { get; }
        public GeometryType Shape { get; }
    }
}


//XDay