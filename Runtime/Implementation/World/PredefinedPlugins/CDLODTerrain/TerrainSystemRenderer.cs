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
using UnityEngine.Pool;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.CDLODTerrain
{
    internal class TerrainSystemRenderer
    {
        public Mesh FullMesh => m_FullMesh;
        public Mesh TopLeftMesh => m_TopLeftMesh;
        public Mesh TopRightMesh => m_TopRightMesh;
        public Mesh BottomLeftMesh => m_BottomLeftMesh;
        public Mesh BottomRightMesh => m_BottomRightMesh;
        public Texture2D HeightMapTexture => m_HeightMapTexture;

        public TerrainSystemRenderer(TerrainSystem terrain)
        {
            m_Terrain = terrain;

            CreateHeightMapTexture(terrain.HeightMapData);

            CreateGridMesh();

            m_RenderablePool = new ObjectPool<Renderable>(createFunc: () =>
            {
                return new Renderable();
            }, actionOnDestroy: (r) => { r.OnDestroy(); });

            var h = terrain.XTileCount;
            var v = terrain.YTileCount;
            m_TileRenderers = new TileRenderer[h * v];
            for (var y = 0; y < v; ++y)
            {
                for (var x = 0; x < h; ++x)
                {
                    var tile = terrain.GetTile(x, y);
                    if (tile != null)
                    {
                        m_TileRenderers[y * h + x] = new TileRenderer(tile, m_HeightMapTexture, terrain.World.AssetLoader);
                    }
                }
            }
        }

        public void Uninitialize()
        {
            ClearRenderables();

            Helper.DestroyUnityObject(m_FullMesh);
            m_FullMesh = null;
            Helper.DestroyUnityObject(m_TopLeftMesh);
            m_TopLeftMesh = null;
            Helper.DestroyUnityObject(m_BottomLeftMesh);
            m_BottomLeftMesh = null;
            Helper.DestroyUnityObject(m_TopRightMesh);
            m_TopRightMesh = null;
            Helper.DestroyUnityObject(m_BottomRightMesh);
            m_BottomRightMesh = null;
            Helper.DestroyUnityObject(m_HeightMapTexture);
            m_HeightMapTexture = null;

            m_RenderablePool.Clear();

            m_InvalidRenderables.Clear();

            m_Renderables.Clear();

            foreach (var tile in m_TileRenderers)
            {
                tile?.OnDestroy();
            }
            m_TileRenderers = null;
        }

        public void UpdateLOD(Vector3 lodSelectionPosition, Rect viewport)
        {
            Shader.SetGlobalVector("_CameraWorldPos", lodSelectionPosition);

            Unmark();

            for (var i = 0; i < m_TileRenderers.Length; ++i)
            {
                if (m_TileRenderers != null && m_TileRenderers[i].IsVisible)
                {
                    m_TileRenderers[i].UpdateLOD(lodSelectionPosition, viewport);
                }
            }

            RemoveUnmarkedRenderables();
        }

        public bool Mark(long id)
        {
            m_Renderables.TryGetValue(id, out var renderable);
            if (renderable != null)
            {
                renderable.Marked = true;
                return true;
            }
            return false;
        }

        public void AddRenderable(long id, Vector3 position, Vector3 scale, int nodeDepth, Mesh mesh, string meshName, ObjectPool<Material> materialPool)
        {
            //x = morph start distance, y = end, z = end / (end-start), w = 1.0f / (end-start)
            var start = m_Terrain.MorphStartDistanceAtNodeDepth[nodeDepth];
            var end = m_Terrain.MorphEndDistanceAtNodeDepth[nodeDepth];
            //
            //const float errorFudge = 0.01f;
            //end = Lerp(end, start, errorFudge);
            //
            var z = end / (end - start);
            var w = 1.0f / (end - start);
            var material = materialPool.Get();
            material.SetVector("_MorphParameters", new Vector4(scale.x, scale.z, z, w));

            var renderable = m_RenderablePool.Get();

            renderable.Init(id, position, scale, material, mesh, meshName, materialPool);
            m_Renderables.Add(id, renderable);
        }

        public void ClearRenderables()
        {
            foreach (var p in m_Renderables)
            {
                p.Value.Uninit();
                m_RenderablePool.Release(p.Value);
            }
            m_Renderables.Clear();
        }

        private void CreateGridMesh()
        {
            m_FullMesh = new Mesh
            {
                name = "Full Mesh"
            };
            m_TopLeftMesh = new Mesh
            {
                name = "Top Left Mesh"
            };
            m_BottomLeftMesh = new Mesh
            {
                name = "Bottom Left Mesh"
            };
            m_TopRightMesh = new Mesh
            {
                name = "Top Right Mesh"
            };
            m_BottomRightMesh = new Mesh
            {
                name = "Bottom Right Mesh"
            };

            var leafNodeSize = m_Terrain.LeafNodeSize;

            var scale = 1.0f / leafNodeSize;

            var idx = 0;
            var meshResolution = leafNodeSize + 1;
            var vertices = new Vector3[meshResolution * meshResolution];
            var uvs = new Vector2[meshResolution * meshResolution];
            for (var i = 0; i < meshResolution; ++i)
            {
                for (var j = 0; j < meshResolution; ++j)
                {
                    vertices[idx] = new Vector3(j, 0, i) * scale;
                    uvs[idx] = new Vector2(j / (float)(meshResolution - 1), i / (float)(meshResolution - 1));
                    ++idx;
                }
            }

            var n = leafNodeSize * leafNodeSize * 6;

            var indices = new int[n];
            var topLeftIndices = new int[n / 4];
            var topRightIndices = new int[n / 4];
            var bottomLeftIndices = new int[n / 4];
            var bottomRightIndices = new int[n / 4];
            idx = 0;
            var bottomLeftIdx = 0;
            var bottomRightIdx = 0;
            var topLeftIdx = 0;
            var topRightIdx = 0;
            var halfSize = leafNodeSize / 2;
            for (var i = 0; i < leafNodeSize; ++i)
            {
                for (var j = 0; j < leafNodeSize; ++j)
                {
                    var v0 = i * meshResolution + j;
                    var v1 = v0 + 1;
                    var v2 = v1 + meshResolution;
                    var v3 = v2 - 1;
                    indices[idx] = v0;
                    indices[idx + 1] = v3;
                    indices[idx + 2] = v1;
                    indices[idx + 3] = v3;
                    indices[idx + 4] = v2;
                    indices[idx + 5] = v1;

                    if (i < halfSize && j < halfSize)
                    {
                        bottomLeftIndices[bottomLeftIdx] = indices[idx];
                        bottomLeftIndices[bottomLeftIdx + 1] = indices[idx + 1];
                        bottomLeftIndices[bottomLeftIdx + 2] = indices[idx + 2];
                        bottomLeftIndices[bottomLeftIdx + 3] = indices[idx + 3];
                        bottomLeftIndices[bottomLeftIdx + 4] = indices[idx + 4];
                        bottomLeftIndices[bottomLeftIdx + 5] = indices[idx + 5];

                        bottomLeftIdx += 6;
                    }
                    else if (i < halfSize && j >= halfSize)
                    {
                        bottomRightIndices[bottomRightIdx] = indices[idx];
                        bottomRightIndices[bottomRightIdx + 1] = indices[idx + 1];
                        bottomRightIndices[bottomRightIdx + 2] = indices[idx + 2];
                        bottomRightIndices[bottomRightIdx + 3] = indices[idx + 3];
                        bottomRightIndices[bottomRightIdx + 4] = indices[idx + 4];
                        bottomRightIndices[bottomRightIdx + 5] = indices[idx + 5];

                        bottomRightIdx += 6;
                    }
                    else if (i >= halfSize && j >= halfSize)
                    {
                        topRightIndices[topRightIdx] = indices[idx];
                        topRightIndices[topRightIdx + 1] = indices[idx + 1];
                        topRightIndices[topRightIdx + 2] = indices[idx + 2];
                        topRightIndices[topRightIdx + 3] = indices[idx + 3];
                        topRightIndices[topRightIdx + 4] = indices[idx + 4];
                        topRightIndices[topRightIdx + 5] = indices[idx + 5];

                        topRightIdx += 6;
                    }
                    else if (i >= halfSize && j < halfSize)
                    {
                        topLeftIndices[topLeftIdx] = indices[idx];
                        topLeftIndices[topLeftIdx + 1] = indices[idx + 1];
                        topLeftIndices[topLeftIdx + 2] = indices[idx + 2];
                        topLeftIndices[topLeftIdx + 3] = indices[idx + 3];
                        topLeftIndices[topLeftIdx + 4] = indices[idx + 4];
                        topLeftIndices[topLeftIdx + 5] = indices[idx + 5];

                        topLeftIdx += 6;
                    }

                    idx += 6;
                }
            }

            var maxBounds = new Bounds();
            float min = -100;
            float max = 100;
            maxBounds.SetMinMax(new Vector3(min, min, min), new Vector3(max, max, max));

            m_FullMesh.vertices = vertices;
            m_FullMesh.uv = uvs;
            m_FullMesh.triangles = indices;
            m_FullMesh.RecalculateNormals();
            m_FullMesh.bounds = maxBounds;

            m_TopLeftMesh.vertices = vertices;
            m_TopLeftMesh.uv = uvs;
            m_TopLeftMesh.triangles = topLeftIndices;
            m_TopLeftMesh.bounds = maxBounds;
            m_TopLeftMesh.RecalculateNormals();

            m_BottomLeftMesh.vertices = vertices;
            m_BottomLeftMesh.uv = uvs;
            m_BottomLeftMesh.triangles = bottomLeftIndices;
            m_BottomLeftMesh.bounds = maxBounds;
            m_BottomLeftMesh.RecalculateNormals();

            m_BottomRightMesh.vertices = vertices;
            m_BottomRightMesh.uv = uvs;
            m_BottomRightMesh.triangles = bottomRightIndices;
            m_BottomRightMesh.bounds = maxBounds;
            m_BottomRightMesh.RecalculateNormals();

            m_TopRightMesh.vertices = vertices;
            m_TopRightMesh.uv = uvs;
            m_TopRightMesh.triangles = topRightIndices;
            m_TopRightMesh.bounds = maxBounds;
            m_TopRightMesh.RecalculateNormals();
        }

        private void RemoveUnmarkedRenderables()
        {
            foreach (var p in m_Renderables)
            {
                var renderable = p.Value;
                if (renderable.Marked == false)
                {
                    m_InvalidRenderables.Add(renderable);
                }
                renderable.Marked = false;
            }

            foreach (var renderable in m_InvalidRenderables)
            {
                renderable.Uninit();
                m_RenderablePool.Release(renderable);
                m_Renderables.Remove(renderable.ID);
            }
            m_InvalidRenderables.Clear();
        }

        private void CreateHeightMapTexture(float[] heights)
        {
            var heightMapWidth = m_Terrain.HeightMapWidth;
            var heightMapHeight = m_Terrain.HeightMapHeight;

            var pixels = new Color[heightMapWidth * heightMapHeight];
            for (var i = 0; i < heights.Length; ++i)
            {
                pixels[i] = new Color(heights[i], 0, 0, 0);
            }
            m_HeightMapTexture = new Texture2D(heightMapWidth, heightMapHeight, TextureFormat.RFloat, false);
            m_HeightMapTexture.SetPixels(pixels);
            m_HeightMapTexture.Apply();
            m_HeightMapTexture.filterMode = FilterMode.Bilinear;
            m_HeightMapTexture.wrapMode = TextureWrapMode.Clamp;
        }

        private void Unmark()
        {
            foreach (var p in m_Renderables)
            {
                p.Value.Marked = false;
            }
        }

        internal class Renderable
        {
            public bool Marked { get; set; }
            public long ID => m_ID;

            public Renderable()
            {
                var gameObject = new GameObject();
                m_Renderer =gameObject.AddComponent<MeshRenderer>();
                m_Filter = gameObject.AddComponent<MeshFilter>();
                m_Transform = gameObject.transform;
            }

            public void Init(long id, Vector3 position, Vector3 scale, Material material, Mesh mesh, string name, ObjectPool<Material> materialPool)
            {
                m_ID = id;
                m_Transform.position = position;
                m_Transform.localScale = scale;
                m_Transform.gameObject.SetActive(true);
                m_Transform.gameObject.name = name;
                
                m_Renderer.sharedMaterial = material;
                m_Filter.sharedMesh = mesh;
                m_MaterialPool = materialPool;

                Marked = true;
            }

            public void Uninit()
            {
                if (m_Renderer != null)
                {
                    m_Renderer.sharedMaterial = null;
                    m_MaterialPool.Release(m_Renderer.sharedMaterial);
                }
                m_MaterialPool = null;
                Debug.Assert(Marked == false);

                if (m_Transform != null)
                {
                    m_Transform.gameObject.SetActive(false);
                }
            }

            public void OnDestroy()
            {
                if (m_Transform != null)
                {
                    Helper.DestroyUnityObject(m_Transform.gameObject);
                }
            }

            private long m_ID;
            private readonly Transform m_Transform;
            private readonly MeshRenderer m_Renderer;
            private readonly MeshFilter m_Filter;
            private ObjectPool<Material> m_MaterialPool;
        }

        private Mesh m_FullMesh;
        private Mesh m_TopLeftMesh;
        private Mesh m_BottomLeftMesh;
        private Mesh m_TopRightMesh;
        private Mesh m_BottomRightMesh;
        private Texture2D m_HeightMapTexture;
        private readonly Dictionary<long, Renderable> m_Renderables = new();
        private readonly List<Renderable> m_InvalidRenderables = new();
        private ObjectPool<Renderable> m_RenderablePool;
        private readonly TerrainSystem m_Terrain;
        private TileRenderer[] m_TileRenderers;
    }
}