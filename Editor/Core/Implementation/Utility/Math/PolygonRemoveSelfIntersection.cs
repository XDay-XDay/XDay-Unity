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
using System.Linq;
using UnityEngine;

namespace XDay.UtilityAPI.Editor
{
    public static class PolygonRemoveSelfIntersection
    {
        public static List<Vector3> Process(List<Vector3> polygon, out List<Vector3> removedVertices)
        {
            SimpleStopwatch w = new();
            w.Begin();
            List<Vector3> part1 = new();
            List<Vector3> part2 = new();
            List<Vector3> originalOutline = new();
            originalOutline.AddRange(polygon);
            removedVertices = new List<Vector3>();
            List<TestPair> testedPairs = new();
            while (true)
            {
                bool foundIntersection = false;
                //find intersection edges
                int n = polygon.Count;
                for (int i = 0; i < n; ++i)
                {
                    for (int j = 0; j < n; ++j)
                    {
                        if (j != i)
                        {
                            Vector2 s1 = Helper.ToVector2(polygon[i]);
                            Vector2 e1 = Helper.ToVector2(polygon[(i + 1) % n]);
                            Vector2 s2 = Helper.ToVector2(polygon[j]);
                            Vector2 e2 = Helper.ToVector2(polygon[(j + 1) % n]);

                            if (ContainsTestedPair(s1, e1, s2, e2, testedPairs) == false)
                            {
                                testedPairs.Add(new TestPair(s1, e1, s2, e2));
                                if (Helper.SegmentSegmentIntersectionTest2D(s1, e1, s2, e2, out Vector2 p))
                                {
                                    foundIntersection = true;
                                    part1.Clear();
                                    for (int k = 0; k <= i; ++k)
                                    {
                                        part1.Add(polygon[k]);
                                    }
                                    part1.Add(Helper.ToVector3XZ(p));
                                    for (int k = j + 1; k < n; ++k)
                                    {
                                        part1.Add(polygon[k]);
                                    }

                                    part2.Clear();
                                    part2.Add(Helper.ToVector3XZ(p));
                                    for (int k = i + 1; k <= j; ++k)
                                    {
                                        part2.Add(polygon[k]);
                                    }
#if false
                                    float area1 = EditorUtils.CalculatePolygonArea(part1);
                                    float area2 = EditorUtils.CalculatePolygonArea(part2);
                                    if (area1 < area2)
                                    {
                                        outline = part2;
                                    }
                                    else {
                                        outline = part1;
                                    }
#else
                                    if (part1.Count < part2.Count)
                                    {
                                        polygon.Clear();
                                        polygon.AddRange(part2);
                                    }
                                    else
                                    {
                                        polygon.Clear();
                                        polygon.AddRange(part1);
                                    }
#endif
                                    break;
                                }
                            }
                        }
                    }
                    if (foundIntersection)
                    {
                        break;
                    }
                }
                if (!foundIntersection)
                {
                    break;
                }
            }

            w.Stop();
            UnityEngine.Debug.Log($"Remove Intersection cost {w.ElapsedSeconds} seconds");

            removedVertices = originalOutline.Except(polygon).ToList();

            return polygon;
        }

        private static bool ContainsTestedPair(Vector2 s1, Vector2 e1, Vector2 s2, Vector2 e2, List<TestPair> pairs)
        {
            for (int i = 0; i < pairs.Count; ++i)
            {
                if (pairs[i].s1 == s1 &&
                    pairs[i].e1 == e1 &&
                    pairs[i].s2 == s2 &&
                    pairs[i].e2 == e2)
                {
                    return true;
                }
            }
            return false;
        }

        private class TestPair
        {
            public TestPair(Vector2 s1, Vector2 e1, Vector2 s2, Vector2 e2)
            {
                this.s1 = s1;
                this.s2 = s2;
                this.e1 = e1;
                this.e2 = e2;
            }

            public Vector2 s1;
            public Vector2 e1;
            public Vector2 s2;
            public Vector2 e2;
        }
    }
}
