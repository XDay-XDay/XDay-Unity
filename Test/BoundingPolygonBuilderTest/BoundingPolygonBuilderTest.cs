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

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using XDay.UtilityAPI.Editor;

public class BoundingPolygonBuilderTest : MonoBehaviour
{
    public Mesh Mesh;
    public GameObject Prefab;

    private void Start()
    {
        m_LastMesh = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Mesh != null && m_LastMesh != Mesh)
            {
                var points = GetMeshPoints();
                var builder = new BoundingPolygonBuilder();
                m_Shape = builder.Build(points).ToArray();
                m_LastMesh = Mesh;
            }
            else if (Prefab != null)
            {
                var builder = new BoundingPolygonBuilder();
                m_Shape = builder.Build(Prefab, 2).ToArray();
            } 
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLineStrip(m_Shape, true);
    }

    private List<Vector3> GetMeshPoints()
    {
        List<Vector3> vertices = new();
        Mesh.GetVertices(vertices);
        return vertices;
    }

    private Vector3[] m_Shape;
    private Mesh m_LastMesh;
}

#endif