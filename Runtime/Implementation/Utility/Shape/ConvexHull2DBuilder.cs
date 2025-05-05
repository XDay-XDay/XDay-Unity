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
using System.Linq;

namespace XDay.UtilityAPI.Shape
{
    public static class ConvexHull2DBuilder
    {
        public static List<Vector3> BuildConvexHull(bool inWorldSpace, GameObject gameObject)
        {
            return BuildConvexHull(inWorldSpace, new List<GameObject>() { gameObject });
        }

        public static List<Vector3> BuildConvexHull(bool inWorldSpace, List<GameObject> gameObjects)
        {
            var collectedVertices = new List<Vector3>();
            foreach (var gameObject in gameObjects)
            {
                CollectVertices(inWorldSpace, gameObject, collectedVertices);
            }

            return BuildConvexHull(collectedVertices);
        }

        public static List<Vector3> BuildConvexHull(List<Vector3> points)
        {
            if (points.Count() <= 3)
            {
                return points;
            }

            points.Sort((p0, p1) => !Mathf.Approximately(p0.x, p1.x) ? p0.x.CompareTo(p1.x) : p0.z.CompareTo(p1.z));

            var pointCount = points.Count();
            var hullPoints = new Vector3[2 * pointCount];

            static float CrossProduct(Vector3 p, Vector3 va, Vector3 vb) { return (va.x - p.x) * (vb.z - p.z) - (va.z - p.z) * (vb.x - p.x); }

            var cursor = 0;
            
            for (var i = 0; i < pointCount; ++i)
            {
                while (cursor >= 2 && CrossProduct(hullPoints[cursor - 2], hullPoints[cursor - 1], points[i]) <= 0)
                {
                    --cursor;
                }
                hullPoints[cursor] = points[i];
                ++cursor;
            }

            var next = cursor + 1;
            for (var i = pointCount - 2; i >= 0; --i)
            {
                while (cursor >= next && CrossProduct(hullPoints[cursor - 2], hullPoints[cursor - 1], points[i]) <= 0)
                {
                    --cursor;
                }
                hullPoints[cursor] = points[i];
                ++cursor;
            }

            return hullPoints.Take(cursor - 1).ToList();
        }

        static void CollectVertices(bool inWorldSpace, GameObject gameObject, List<Vector3> collectedVertices)
        {
            foreach (var spriteRenderer in gameObject.GetComponentsInChildren<SpriteRenderer>(includeInactive: true))
            {
                collectedVertices.AddRange(BoundsToVertices(inWorldSpace ? spriteRenderer.bounds : spriteRenderer.sprite.bounds));
            }

            foreach (var meshFilter in gameObject.GetComponentsInChildren<MeshFilter>(includeInactive:true))
            {
                collectedVertices.AddRange(ToWorldSpace(inWorldSpace, meshFilter));
            }
        }

        static List<Vector3> ToWorldSpace(bool inWorldSpace, MeshFilter meshFilter)
        {
            var worldSpaceVertices = new List<Vector3>();
            if (meshFilter.sharedMesh != null)
            {
                m_TempContainer.Clear();
                meshFilter.sharedMesh.GetVertices(m_TempContainer);

                if (!inWorldSpace)
                {
                    worldSpaceVertices.AddRange(m_TempContainer);
                }
                else
                {
                    foreach (var localPosition in m_TempContainer)
                    {
                        var worldPosition = meshFilter.transform.TransformPoint(localPosition);
                        worldPosition.y = 0;
                        worldSpaceVertices.Add(worldPosition);
                    }
                }
            }

            return worldSpaceVertices;
        }

        static List<Vector3> BoundsToVertices(Bounds bounds)
        {
            var boundsMin = bounds.min;
            var boundsMax = bounds.max;
            return new List<Vector3> {
                new Vector3(boundsMax.x, 0, boundsMin.z),
                new Vector3(boundsMax.x, 0, boundsMax.z),
                new Vector3(boundsMin.x, 0, boundsMax.z),
                new Vector3(boundsMin.x, 0, boundsMin.z),        
            };
        }

        static readonly List<Vector3> m_TempContainer = new();
    }
}