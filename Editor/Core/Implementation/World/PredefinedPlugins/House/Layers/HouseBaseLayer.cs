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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.House.Editor
{
    internal abstract class HouseBaseLayer : ISerializable
    {
        public int ID => m_ID;
        public GameObject RootGameObject { get; private set; }
        public string Name { get => RootGameObject.name; set => RootGameObject.name = value; }
        public int HorizontalGridCount => m_HorizontalGridCount;
        public int VerticalGridCount => m_VerticalGridCount;
        public bool IsActive
        {
            set => RootGameObject.SetActive(value);
            get
            {
                if (RootGameObject == null)
                {
                    return false;
                }
                return RootGameObject.activeInHierarchy;
            }
        }
        public bool ShowInInspector { get; set; } = true;
        public abstract string TypeName { get; }

        public HouseBaseLayer()
        {
        }

        public HouseBaseLayer(int id, string name, int horizontalGridCount, int verticalGridCount, float gridSize)
        {
            m_ID = id;
            m_Name = name;
            m_HorizontalGridCount = horizontalGridCount;
            m_VerticalGridCount = verticalGridCount;
            m_GridSize = gridSize;
        }

        public void Initialize(Transform parent, Vector3 localPosition)
        {
            CreateRoot(m_Name, parent, localPosition);

            OnInitialize();

            UpdateColors();
        }

        protected virtual void OnInitialize() { }

        public void OnDestroy()
        {
            m_GridMesh.OnDestroy();
            Helper.DestroyUnityObject(m_Texture);
            Helper.DestroyUnityObject(RootGameObject);
            Helper.DestroyUnityObject(m_GridMaterial);
        }

        public void SetHeight(float height, Vector3 offset)
        {
            RootGameObject.transform.localPosition = new Vector3(0, height, 0) + offset;
        }

        public abstract void Rotate();

        public void SetGridColors(Color32[] colors)
        {
            Debug.Assert(colors != null && colors.Length == m_Texture.width * m_Texture.height);

            m_Texture.SetPixels32(colors);
            m_Texture.Apply();
        }

        public void UpdateColors()
        {
            var colors = new Color32[m_HorizontalGridCount * m_VerticalGridCount];
            for (var y = 0; y < m_VerticalGridCount; ++y)
            {
                for (var x = 0; x < m_HorizontalGridCount; ++x)
                {
                    var idx = y * m_HorizontalGridCount + x;
                    colors[idx] = GetColor(x, y);
                }
            }
            SetGridColors(colors);
        }

        void CreateRoot(string name, Transform parent, Vector3 localPosition)
        {
            RootGameObject = new GameObject(name);
            RootGameObject.SetActive(false);
            Helper.HideGameObject(RootGameObject);

            var filter = RootGameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateGridMesh();

            var renderer = RootGameObject.AddComponent<MeshRenderer>();
            m_GridMaterial = new Material(Shader.Find("Unlit/Texture"));
            var mtls = new Material[m_GridMesh.Mesh.subMeshCount];
            for (var i = 0; i < mtls.Length; ++i)
            {
                mtls[i] = m_GridMaterial;
            }
            renderer.sharedMaterials = mtls;
            CreateTexture();
            m_GridMaterial.SetTexture("_MainTex", m_Texture);

            RootGameObject.transform.SetParent(parent, worldPositionStays: false);
            RootGameObject.transform.localPosition = localPosition;
        }

        Mesh CreateGridMesh()
        {
            var hResolution = m_HorizontalGridCount + 1;
            var vResolution = m_VerticalGridCount + 1;
            var vertices = new Vector3[hResolution * vResolution];
            var uvs = new Vector2[hResolution * vResolution];
            var indices = new int[m_HorizontalGridCount * m_VerticalGridCount * 6];

            var vertexIdx = 0;
            var index = 0;
            var stepU = 1.0f / m_HorizontalGridCount;
            var stepV = 1.0f / m_VerticalGridCount;
            for (var y = 0; y <= m_VerticalGridCount; ++y)
            {
                for (var x = 0; x <= m_HorizontalGridCount; ++x)
                {
                    vertices[vertexIdx] = new Vector3(x * m_GridSize, 0, y * m_GridSize);
                    uvs[vertexIdx] = new Vector2(x * stepU, y * stepV);
                    ++vertexIdx;

                    if (x < m_HorizontalGridCount && y < m_VerticalGridCount)
                    {
                        var v0 = y * hResolution + x;
                        var v1 = v0 + 1;
                        var v2 = v1 + hResolution;
                        var v3 = v2 - 1;

                        indices[index] = v0;
                        indices[index + 1] = v3;
                        indices[index + 2] = v2;
                        indices[index + 3] = v0;
                        indices[index + 4] = v2;
                        indices[index + 5] = v1;

                        index += 6;
                    }
                }
            }

            m_GridMesh = new LargeMesh(vertices, indices, uvs, null);
            return m_GridMesh.Mesh;
        }

        void CreateTexture()
        {
            m_Texture = new Texture2D(m_HorizontalGridCount, m_VerticalGridCount, TextureFormat.RGBA32, false);
            m_Texture.wrapMode = TextureWrapMode.Clamp;
            m_Texture.filterMode = FilterMode.Point;
        }

        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < m_HorizontalGridCount &&
                y >= 0 && y < m_VerticalGridCount;
        }

        protected abstract Color GetColor(int x, int y);

        public virtual void EditorSerialize(ISerializer writer, string label, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_BaseVersion, "BaseLayer.Version");

            writer.WriteObjectID(m_ID, "ID", translator);
            writer.WriteString(m_Name, "Name");
            writer.WriteInt32(m_HorizontalGridCount, "Horizontal Grid Count");
            writer.WriteInt32(m_VerticalGridCount, "Vertical Grid Count");
            writer.WriteSingle(m_GridSize, "Grid Size");
        }

        public virtual void EditorDeserialize(IDeserializer reader, string label)
        {
            reader.ReadInt32("BaseLayer.Version");

            m_ID = reader.ReadInt32("ID");
            m_Name = reader.ReadString("Name");
            m_HorizontalGridCount = reader.ReadInt32("Horizontal Grid Count");
            m_VerticalGridCount = reader.ReadInt32("Vertical Grid Count");
            m_GridSize = reader.ReadSingle("Grid Size");
        }

        private int m_ID;
        private string m_Name;
        private LargeMesh m_GridMesh;
        private Material m_GridMaterial;
        private int m_HorizontalGridCount;
        private int m_VerticalGridCount;
        private float m_GridSize;
        private Texture2D m_Texture;
        private protected Grid m_Grid;
        private const int m_BaseVersion = 1;
    }
}