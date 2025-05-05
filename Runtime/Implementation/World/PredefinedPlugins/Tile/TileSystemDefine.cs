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

namespace XDay.WorldAPI.Tile
{
    public class TileSystemDefine
    {
        public static string GetTerrainMeshPath(string mapDataFolder, int x, int y, int lod)
        {
            Debug.Assert(!string.IsNullOrEmpty(mapDataFolder));
            return $"{GetFullTerrainMeshFolderPath(mapDataFolder)}/terrain_Mesh_{x}_{y}_lod{lod}.asset";
        }

        public static string GetFullTerrainMeshFolderPath(string mapDataFolder)
        {
            Debug.Assert(!string.IsNullOrEmpty(mapDataFolder));
            return $"{mapDataFolder}/{WorldDefine.CONSTANT_FOLDER_NAME}/{MAP_TERRAIN_MESH_FOLDER_NAME}";
        }

        public static string GetFullTerrainPrefabFolderPath(string mapDataFolder)
        {
            Debug.Assert(!string.IsNullOrEmpty(mapDataFolder));
            return $"{mapDataFolder}/{WorldDefine.CONSTANT_FOLDER_NAME}/{MAP_TERRAIN_PREFAB_FOLDER_NAME}";
        }

        private static readonly string MAP_TERRAIN_MESH_FOLDER_NAME = "TerrainMesh";
        private static readonly string MAP_TERRAIN_PREFAB_FOLDER_NAME = "TerrainPrefab";
    }
}
