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
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystemLayerRenderer
    {
        public GameObject Root => m_Root;

        public RegionSystemLayerRenderer(Transform parent, RegionSystemLayer layer)
        {
            m_Layer = layer;

            CreateRoot(parent);

            CreateGrid();

            UpdateColors(0, 0, m_Layer.HorizontalGridCount - 1, m_Layer.VerticalGridCount - 1);
        }

        public void OnDestroy()
        {
            m_GridMesh.OnDestroy();

            Helper.DestroyUnityObject(m_Mesh);
            Helper.DestroyUnityObject(m_Texture);
            Helper.DestroyUnityObject(m_GridMaterial);

            foreach (var renderer in m_Renderers.Values)
            {
                renderer.OnDestroy();
            }
            Helper.DestroyUnityObject(m_Root);
        }

        public GameObject QueryGameObject(int objectID)
        {
            m_Renderers.TryGetValue(objectID, out var renderer);
            if (renderer != null)
            {
                return renderer.Root;
            }
            return null;
        }

        public void SetAspect(int objectID, string name)
        {
            var region = m_Layer.System.World.QueryObject<RegionObject>(objectID);

            if (name == "Layer Name")
            {
                Root.name = m_Layer.Name;
                return;
            }

            if (name == "Layer Visibility")
            {
                Root.SetActive(m_Layer.IsActive);
                return;
            }

            if (name == "Grid Visible")
            {
                ShowGrid(m_Layer.GridVisible);
                return;
            }

            if (name == RegionDefine.ENABLE_REGION_NAME)
            {
                ToggleVisibility(region);
                return;
            }

            if (name == RegionDefine.REGION_NAME)
            {
                if (m_Renderers.TryGetValue(objectID, out var renderer))
                {
                    renderer.Root.name = region.Name;
                }
                return;
            }

            if (name == RegionDefine.COLOR_NAME)
            {
                return;
            }

            Debug.Assert(false, $"OnSetAspect todo: {name}");
        }

        public void ShowGrid(bool show)
        {
            m_GridMesh.SetActive(show);
        }

        public bool Destroy(RegionObject data)
        {
            if (m_Renderers.TryGetValue(data.ID, out var renderer))
            {
                renderer.OnDestroy();
                m_Renderers.Remove(data.ID);
                return true;
            }
            return false;
        }

        public void Create(RegionObject region)
        {
            if (m_Renderers.ContainsKey(region.ID))
            {
                return;
            }

            var renderer = new RegionRenderer(region, m_Root.transform);
            m_Renderers.Add(region.ID, renderer);

            foreach (var kv in m_Renderers)
            {
                var obj = m_Layer.System.QueryObjectUndo(kv.Key);
                kv.Value.Root.transform.SetSiblingIndex(obj.ObjectIndex);
            }
        }

        public void ToggleVisibility(RegionObject obj)
        {
            if (m_Renderers.TryGetValue(obj.ID, out var renderer))
            {
                renderer?.SetActive(obj.IsActive);
            }
            else
            {
                Create(obj);
            }
        }

        public void SetDirty(int objectID)
        {
            if (m_Renderers.TryGetValue(objectID, out var renderer))
            {
                renderer?.SetDirty();
            }
        }

        public int QueryObjectID(GameObject gameObject)
        {
            foreach (var kv in m_Renderers)
            {
                if (kv.Value.Root == gameObject)
                {
                    return kv.Key;
                }
            }
            return 0;
        }

        public void Update()
        {
            if (Root.activeInHierarchy)
            {
                foreach (var renderer in m_Renderers.Values)
                {
                    renderer.Draw(true);
                }
            }
        }

        private void CreateRoot(Transform parent)
        {
            m_Root = new GameObject(m_Layer.Name);
            m_Root.transform.SetParent(parent, true);
            m_Root.transform.position = new Vector3(0, m_Layer.YOffset, 0);
            Selection.activeGameObject = m_Root;
            var filter = m_Root.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateMesh();

            var renderer = m_Root.AddComponent<MeshRenderer>();
            m_GridMaterial = new Material(Shader.Find("XDay/TextureTransparent"))
            {
                renderQueue = 3200 + m_Layer.ObjectIndex * 5
            };
            renderer.sharedMaterial = m_GridMaterial;

            m_Texture = new Texture2D(m_Layer.HorizontalGridCount, m_Layer.VerticalGridCount, TextureFormat.RGBA32, false);
            m_Texture.wrapMode = TextureWrapMode.Clamp;
            m_Texture.filterMode = FilterMode.Point;
            m_GridMaterial.SetTexture("_MainTex", m_Texture);
        }

        private Mesh CreateMesh()
        {
            m_Mesh = new Mesh();

            var bounds = m_Layer.Bounds;
            m_Mesh.vertices = new Vector3[4]
            {
                bounds.min,
                new(bounds.min.x, 0, bounds.max.z),
                new(bounds.max.x, 0, bounds.max.z),
                new(bounds.max.x, 0, bounds.min.z),
            };
            m_Mesh.uv = new Vector2[4]
            {
                Vector2.zero,
                new(0, 1),
                Vector2.one,
                new(1, 0),
            };
            m_Mesh.triangles = new int[6]
            {
                0, 1, 2, 0, 2, 3
            };
            m_Mesh.RecalculateBounds();

            return m_Mesh;
        }

        private void CreateGrid()
        {
            var shader = Shader.Find("XDay/Grid");
            var material = new Material(shader);
            m_GridMesh = new GridMesh(m_Layer.Name, m_Layer.Origin, m_Layer.HorizontalGridCount, m_Layer.VerticalGridCount,
                m_Layer.GridWidth, m_Layer.GridHeight, material, new Color32(255, 190, 65, 150), m_Root.transform, true, 
                3000 + m_Layer.ObjectIndex * 5 + 1);
            ShowGrid(m_Layer.GridVisible);
            UnityEngine.Object.DestroyImmediate(material);
        }

        public void UpdateColors(int minX, int minY, int maxX, int maxY)
        {
            var validRange = CheckRange(minX, minY, maxX, maxY, out var validMinX, out var validMinY, out var validMaxX, out var validMaxY);
            if (!validRange)
            {
                return;
            }

            var bw = validMaxX - validMinX + 1;
            var bh = validMaxY - validMinY + 1;
            var colors = m_Layer.System.Renderer.Pool.Rent(bw * bh);
            var idx = 0;
            for (var y = validMinY; y <= validMaxY; ++y)
            {
                for (var x = validMinX; x <= validMaxX; ++x)
                {
                    colors[idx] = m_Layer.GetColor(x, y);
                    ++idx;
                }
            }

            SetGridColors(validMinX, validMinY, validMaxX, validMaxY, colors);

            m_Layer.System.Renderer.Pool.Return(colors);
        }

        private bool CheckRange(int minX, int minY, int maxX, int maxY, 
            out int validMinX, out int validMinY, out int validMaxX, out int validMaxY)
        {
            validMinX = 0;
            validMinY = 0;
            validMaxX = 0;
            validMaxY = 0;

            if (minX >= m_Layer.HorizontalGridCount ||
                minY >= m_Layer.VerticalGridCount ||
                maxX < 0 ||
                maxY < 0)
            {
                return false;
            }

            validMinX = Mathf.Max(minX, 0);
            validMinY = Mathf.Max(minY, 0);
            validMaxX = Mathf.Min(maxX, m_Layer.HorizontalGridCount - 1);
            validMaxY = Mathf.Min(maxY, m_Layer.VerticalGridCount - 1);

            return true;
        }

        public void SetGridColors(int minX, int minY, int maxX, int maxY, Color32[] colors)
        {
            m_Texture.SetPixels32(minX, minY, maxX - minX + 1, maxY - minY + 1, colors);
            m_Texture.Apply();
        }

        internal Vector3 GetRegionBuildingPosition(int id)
        {
            m_Renderers.TryGetValue(id, out var renderer);
            return renderer.BuildingPosition;
        }

        internal GameObject GetRegionGameObject(int id)
        {
            m_Renderers.TryGetValue(id, out var renderer);
            return renderer.BuildingGameObject;
        }

        private readonly RegionSystemLayer m_Layer;
        private GameObject m_Root;
        private readonly Dictionary<int, RegionRenderer> m_Renderers = new();
        private GridMesh m_GridMesh;
        private Mesh m_Mesh;
        private Material m_GridMaterial;
        private Texture2D m_Texture;
    }
}

