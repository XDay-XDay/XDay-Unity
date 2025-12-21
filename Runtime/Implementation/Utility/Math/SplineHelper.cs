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

namespace XDay.UtilityAPI.Math
{
    public static class SplineHelper
    {
        public static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float a = (1 - t) * (1 - t);
            float b = 2 * (1 - t) * t;
            float c = t * t;

            return a * p0 + b * p1 + c * p2;
        }

        public static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return new Vector3(
                Bezier(p0.x, p1.x, p2.x, p3.x, t),
                Bezier(p0.y, p1.y, p2.y, p3.y, t),
                Bezier(p0.z, p1.z, p2.z, p3.z, t));
        }

        public static float Bezier(float x0, float x1, float x2, float x3, float t)
        {
            return (((x3 - 3 * x2 + 3 * x1 - x0) * t + (3 * x2 - 6 * x1 + 3 * x0)) * t + (3 * x1 - 3 * x0)) * t + x0;
        }

        public static void Evaluate(List<SplineControlPoint> controlPoints, bool isLoop, List<SplineEvaluatePoint> result)
        {
            result.Clear();
            for (int i = 0; i < controlPoints.Count - 1; ++i)
            {
                EvaluateSegment(controlPoints[i], controlPoints[i + 1], i + 1, controlPoints, result);
            }

            if (isLoop && controlPoints.Count > 2)
            {
                EvaluateSegment(controlPoints[controlPoints.Count - 1], controlPoints[0], 0, controlPoints, result);
                //remove last point which is same as first point
                result.RemoveAt(result.Count - 1);
            }
        }

        private static void EvaluateSegment(SplineControlPoint start,
            SplineControlPoint end, 
            int controlPointIndex, 
            List<SplineControlPoint> controlPoints, 
            List<SplineEvaluatePoint> evaluatedPoints)
        {
            float lengthSoFar = 0;
            List<Vector3> pointsInSegment = new();
            for (int i = 0; i < end.PointCountInSegment; ++i)
            {
                float t = (float)(i + 1) / end.PointCountInSegment;
                var pos = Bezier(start.Position, start.Tangents[1], end.Tangents[0], end.Position, t);
                pointsInSegment.Add(pos);
                if (pointsInSegment.Count >= 2)
                {
                    lengthSoFar += (pos - pointsInSegment[pointsInSegment.Count - 2]).magnitude;
                }

                if (evaluatedPoints.Count == 0 || evaluatedPoints[evaluatedPoints.Count - 1].Position != pos)
                {
                    //去掉重复的点
                    bool isBreakPoint = (controlPointIndex != controlPoints.Count - 1 && i == end.PointCountInSegment - 1);
                    evaluatedPoints.Add(new SplineEvaluatePoint(pos, controlPointIndex, lengthSoFar, isBreakPoint));
                }
            }

            end.CalculatedCurveLength = lengthSoFar;
        }
    }

    public class SplineControlPoint
    {
        public float CalculatedCurveLength = 0;
        public int PointCountInSegment = 2;
        public Vector3[] Tangents = new Vector3[2];
        public Vector3 Position;
    }

    public class SplineEvaluatePoint
    {
        public SplineEvaluatePoint(Vector3 pos, int controlPointIndex, float lengthFromStartControlPoint, bool isBreakPoint)
        {
            Position = pos;
            ControlPointIndex = controlPointIndex;
            LengthFromStartControlPoint = lengthFromStartControlPoint;
            IsBreakPoint = isBreakPoint;
        }

        public Vector3 Position;
        public int ControlPointIndex = -1;
        public float LengthFromStartControlPoint = 0;
        public bool IsBreakPoint;
    }
}
