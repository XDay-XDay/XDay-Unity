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

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TileSystem
    {
        protected override void GenerateGameDataInternal(IObjectIDConverter converter)
        {
            FileUtil.DeleteFileOrDirectory(GetGameFilePath("tile"));
            FileUtil.DeleteFileOrDirectory(GetGameFilePath("terrain_lod_tile"));
            
            if (m_GameTileType == GameTileType.FlatTile)
            {
                GenerateFlatTileData(converter);
            }
            else if (m_GameTileType == GameTileType.TerrainLOD)
            {
                GenerateTerrainLODTileData(converter);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private void GenerateFlatTileData(IObjectIDConverter converter)
        {
            ISerializer serializer = ISerializer.CreateBinary();

            serializer.WriteInt32(m_FlatTileVersion, "FlatTile.Version");

            serializer.WriteSerializable(m_ResourceDescriptorSystem, "Resource Descriptor System", converter, true);
            serializer.WriteSerializable(m_PluginLODSystem, "Plugin LOD System", converter, true);

            serializer.WriteQuaternion(m_Rotation, "Rotation");
            serializer.WriteString(m_TilePrefabFolder, "TilePrefabFolder");
            serializer.WriteBoolean(m_EnableDynamicMaskTextureLoading, "EnableDynamicMaskTextureLoading");

            serializer.WriteInt32(m_XTileCount, "X Tile Count");
            serializer.WriteInt32(m_YTileCount, "Y Tile Count");
            serializer.WriteVector2(m_Origin, "Origin");
            serializer.WriteSingle(m_TileWidth, "Tile Width");
            serializer.WriteSingle(m_TileHeight, "Tile Height");
            serializer.WriteString(m_Name, "Name");

            for (var y = 0; y < m_YTileCount; ++y)
            {
                for (var x = 0; x < m_XTileCount; ++x)
                {
                    var tile = m_Tiles[y * m_XTileCount + x];
                    serializer.WriteString(tile?.AssetPath, "");
                    bool hasHeight = false;
                    bool exportHeight = false;
                    if (tile.VertexHeights != null && tile.VertexHeights.Length > 0)
                    {
                        hasHeight = true;
                        if (m_GetGroundHeightInGame)
                        {
                            exportHeight = true;
                        }
                    }
                    serializer.WriteBoolean(hasHeight, "Has Height");
                    serializer.WriteBoolean(exportHeight, "Export Height");
                    if (exportHeight)
                    {
                        serializer.WriteSingleArray(tile.VertexHeights, "Vertex Heights");
                    }
                }
            }

            var usedPrefabPaths = GetUsedTilePrefabPaths();
            serializer.WriteStringArray(usedPrefabPaths.ToArray(), "Used Tile Prefab Paths");

            serializer.Uninit();
            EditorHelper.WriteFile(serializer.Data, GetGameFilePath("tile"));
        }

        private void GenerateTerrainLODTileData(IObjectIDConverter translator)
        {
            var heightMapWidth = Mathf.FloorToInt(m_XTileCount * m_TileWidth);
            var heightMapHeight = Mathf.FloorToInt(m_YTileCount * m_TileHeight);
            var heightMapData = new float[heightMapWidth * heightMapHeight];

            ISerializer serializer = ISerializer.CreateBinary();

            serializer.WriteInt32(m_TerrainLODTileVersion, "TerrainLODTile.Version");

            serializer.WriteInt32(heightMapWidth, "Height World Width");
            serializer.WriteInt32(heightMapHeight, "Height World Height");
            serializer.WriteSingleArray(heightMapData, "Heights");

            serializer.WriteSerializable(m_ResourceDescriptorSystem, "Resource Descriptor System", translator, true);
            serializer.WriteSerializable(m_PluginLODSystem, "Plugin LOD System", translator, true);

            serializer.WriteSingle(m_TerrainLODMorphRatio, "LOD Morph Ratio");
            serializer.WriteQuaternion(m_Rotation, "Rotation");
            serializer.WriteInt32(m_XTileCount, "X Tile Count");
            serializer.WriteInt32(m_YTileCount, "Y Tile Count");
            serializer.WriteVector2(m_Origin, "Origin");
            serializer.WriteSingle(m_TileWidth, "Tile Width");
            serializer.WriteSingle(m_TileHeight, "Tile Height");
            serializer.WriteString(m_Name, "Name");

            for (var y = 0; y < m_YTileCount; ++y)
            {
                for (var x = 0; x < m_XTileCount; ++x)
                {
                    var tile = m_Tiles[y * m_XTileCount + x];
                    string materialPath = null;
                    if (tile != null)
                    {
                        var material = AssetDatabase.LoadAssetAtPath<GameObject>(tile.AssetPath).GetComponentInChildren<MeshRenderer>().sharedMaterial;
                        materialPath = material == null ? null : AssetDatabase.GetAssetPath(material);
                    }
                    serializer.WriteString(materialPath, "");
                }
            }

            serializer.Uninit();
            EditorHelper.WriteFile(serializer.Data, GetGameFilePath("terrain_lod_tile"));
        }

        private List<string> GetUsedTilePrefabPaths()
        {
            List<string> usedPrefabsOfHeightTile = new();
            for (int i = 0; i < m_YTileCount; ++i)
            {
                for (int j = 0; j < m_XTileCount; ++j)
                {
                    var tile = GetTile(j, i);
                    if (tile != null && 
                        tile.VertexHeights != null)
                    {
                        string prefabPath = tile.AssetPath;
                        if (!usedPrefabsOfHeightTile.Contains(prefabPath))
                        {
                            usedPrefabsOfHeightTile.Add(prefabPath);
                        }
                    }
                }
            }

            return usedPrefabsOfHeightTile;
        }

        private const int m_TerrainLODTileVersion = 1;
        private const int m_FlatTileVersion = 1;
    }
}

//XDay