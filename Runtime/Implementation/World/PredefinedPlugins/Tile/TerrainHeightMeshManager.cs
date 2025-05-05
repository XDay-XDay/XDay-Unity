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
using XDay.AssetAPI;

namespace XDay.WorldAPI.Tile
{
    public class TerrainHeightMeshManager
    {
        public TerrainHeightMeshManager(string worldDataFolder, IAssetLoader loader)
        {
            m_WorldDataFolder = worldDataFolder;
            m_AssetLoader = loader;
        }

        public Mesh GetHeightMesh(int tileX, int tileY, int lod)
        {
            var key = new Vector3Int(tileX, tileY, lod);
            bool found = m_MeshPath.TryGetValue(key, out var path);
            if (!found)
            {
                path = TileSystemDefine.GetTerrainMeshPath(m_WorldDataFolder, tileX, tileY, lod);
                m_MeshPath[key] = path;
            }
            var mesh = m_AssetLoader.Load<Mesh>(path);
            return mesh;
        }

        public void SetOriginalMesh(string prefabPath, Mesh mesh)
        {
            m_TileOriginalMesh[prefabPath] = mesh;
        }

        public Mesh GetOriginalMesh(string prefabPath)
        {
            m_TileOriginalMesh.TryGetValue(prefabPath, out Mesh result);
            return result;
        }

        private readonly Dictionary<string, Mesh> m_TileOriginalMesh = new();
        private readonly Dictionary<Vector3Int, string> m_MeshPath = new();
        private readonly string m_WorldDataFolder;
        private readonly IAssetLoader m_AssetLoader;
    }
}
