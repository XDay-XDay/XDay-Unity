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
using UnityEditor;
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.City.Editor
{
    class GridView
    {
        public GridView(string name, int horizontalGridCount, int verticalGridCount, float gridSize, Vector3 position, Quaternion rotation, Transform parent, Grid grid, CityEditor cityEditor)
        {
            m_HorizontalGridCount = horizontalGridCount;
            m_VerticalGridCount = verticalGridCount;
            m_GridSize = gridSize;

            CreateRoot(name, position, rotation, parent, grid, cityEditor);
            CreateLine();
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_LineMesh);
            Helper.DestroyUnityObject(RootGameObject);
            Helper.DestroyUnityObject(m_LineMaterial);
        }

        void CreateRoot(string name, Vector3 position, Quaternion rotation, Transform parent, Grid grid, CityEditor cityEditor)
        {
            RootGameObject = new GameObject(name);
            RootGameObject.AddComponent<NoKeyDeletion>();
            var behaviour = RootGameObject.AddOrGetComponent<GridBehaviour>();
            behaviour.Initialize(grid.ID, (e) => { WorldEditor.EventSystem.Broadcast(e); });
            Selection.activeGameObject = RootGameObject;

            RootGameObject.transform.SetParent(parent, worldPositionStays: true);
            RootGameObject.transform.SetPositionAndRotation(position, rotation);
        }

        void CreateLine()
        {
            m_Line = new GameObject("Line");
            m_Line.AddComponent<NoKeyDeletion>();
            m_Line.transform.SetParent(RootGameObject.transform, false);
            m_Line.transform.localPosition = new Vector3(0, 0.05f, 0);
            //Common.HideGameObjectInHierarchyWindow(m_Line);

            var renderer = m_Line.AddComponent<MeshRenderer>();
            m_LineMaterial = new Material(Shader.Find("XDay/Grid"));
            renderer.sharedMaterial = m_LineMaterial;
            m_LineMaterial.SetColor("_Color", CityEditorDefine.Blue);
            var filter = m_Line.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateLineMesh();
        }

        Mesh CreateLineMesh()
        {
            var hResolution = m_HorizontalGridCount + 1;
            var vResolution = m_VerticalGridCount + 1;
            var vertices = new Vector3[hResolution * 2 + vResolution * 2];
            var indices = new int[vertices.Length];

            var width = m_HorizontalGridCount * m_GridSize;
            var height = m_VerticalGridCount * m_GridSize;

            //horizontal line
            for (var i = 0; i < vResolution; ++i)
            {
                vertices[i * 2] = new Vector3(-width * 0.5f, 0, i * m_GridSize - height * 0.5f);
                vertices[i * 2 + 1] = new Vector3(width * 0.5f, 0, i * m_GridSize - height * 0.5f);
            }

            //vertical line
            var offset = vResolution * 2;
            for (var i = 0; i < hResolution; ++i)
            {
                vertices[offset + i * 2] = new Vector3(i * m_GridSize - width * 0.5f, 0, -height * 0.5f);
                vertices[offset + i * 2 + 1] = new Vector3(i * m_GridSize - width * 0.5f, 0, height * 0.5f);
            }

            for (var i = 0; i < indices.Length; ++i)
            {
                indices[i] = i;
            }

            m_LineMesh = new Mesh
            {
                vertices = vertices
            };
            m_LineMesh.SetIndices(indices, MeshTopology.Lines, 0);
            m_LineMesh.RecalculateBounds();
            m_LineMesh.UploadMeshData(markNoLongerReadable: true);

            return m_LineMesh;
        }

        public Vector3 Position {
            set
            {
                if (RootGameObject != null)
                {
                    RootGameObject.transform.position = value;
                }
            }
            get
            {
                if (RootGameObject == null)
                {
                    return Vector3.zero;
                }
                return RootGameObject.transform.position;
            }
        }
        public Quaternion Rotation {
            set
            {
                if (RootGameObject != null) {
                    RootGameObject.transform.rotation = value;
                }
            } 
            get
            {
                if (RootGameObject == null)
                {
                    return Quaternion.identity;
                }
                return RootGameObject.transform.rotation;
            } 
        }
        public GameObject RootGameObject { get; private set; }
        public string Name { get => RootGameObject.name; set => RootGameObject.name = value; }
        public bool IsLineActive { get => m_Line.activeSelf; set => m_Line.SetActive(value); }

        GameObject m_Line;
        Mesh m_LineMesh;
        Material m_LineMaterial;
        readonly int m_HorizontalGridCount;
        readonly int m_VerticalGridCount;
        readonly float m_GridSize;
    }
}

