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
using System.IO;
using System.Collections.Generic;
using XDay.WorldAPI.Editor;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile.Editor
{
    public class TileSystemCreateWindow : GridBasedWorldPluginCreateWindow
    {
        protected override bool SetGridCount => !m_UseTerrainLOD;
        protected override string DisplayName => "Tile System";

        protected override void CreateInternal()
        {
            var createInfo = new TileSystemCreateInfo
            {
                TileWidth = m_GridWidth,
                TileHeight = m_GridHeight,
                HorizontalTileCount = Mathf.CeilToInt(m_Width / m_GridWidth),
                VerticalTileCount = Mathf.CeilToInt(m_Height / m_GridHeight),
                Origin = CalculateOrigin(m_Width, m_Height),
                Rotation = m_Rotation,
                Name = DisplayName,
                ObjectIndex = World.PluginCount,
                ID = World.AllocateObjectID(),
            };

            var ok = true;
            if (m_TileSource == TileSource.CreateNewTile)
            {
                Helper.CreateDirectory(GetAssetPath());

                var tileCreateInfo = new TileCreateInfo
                {
                    TileName = m_TileName,
                    Material = m_CustomMaterial,
                    TileSize = m_GridWidth,
                    OutputPath = GetAssetPath(),
                    MaskResolution = m_MaskTextureResolution,
                    MaskTextureChannelCount = m_MaskFormat == TextureFormat.RGB ? 3 : 4,
                    XTileCount = Mathf.CeilToInt(m_Width / m_GridWidth),
                    YTileCount = Mathf.CeilToInt(m_Height / m_GridHeight),
                    CreateRoot = true,
                    MaskName = m_MaskName,
                };

                ok = new TileAssetCreator().Create(tileCreateInfo);
            }

            if (ok)
            {
                var system = new TileSystem(createInfo);
                UndoSystem.CreateObject(system, World.ID, "Create Tile System");
                if (m_TileSource == TileSource.CreateNewTile)
                {
                    CreateTiles();
                }
            }
            else
            {
                Debug.LogError("Create tile failed!");
            }
        }

        protected override void GUIInternal()
        {
            base.GUIInternal();

            m_Rotation = EditorGUILayout.FloatField("Rotation", m_Rotation);

            m_TileSource = (TileSource)EditorGUILayout.EnumPopup("Type", m_TileSource);
            if (m_TileSource == TileSource.CreateNewTile)
            {
                var oldColor = EditorStyles.label.normal.textColor;
                m_UseTerrainLOD = EditorGUILayout.ToggleLeft("Use Terrain LOD", m_UseTerrainLOD);
                EditorStyles.label.normal.textColor = Color.red;
                m_TileName = EditorGUILayout.TextField("Name", m_TileName);
                m_OutputDir = Helper.ToUnityPath(EditorHelper.DirectoryField("Output Folder", m_OutputDir, "", () => { Repaint(); }, tooltip: null));
                EditorStyles.label.normal.textColor = oldColor;
                m_MaskFormat = (TextureFormat)EditorGUILayout.EnumPopup("Splat Mask Format", m_MaskFormat);
                m_MaskTextureResolution = EditorGUILayout.IntField("Splat Mask Resolution", m_MaskTextureResolution);
                GUI.enabled = m_CustomMaterial != null;
                m_MaskName = EditorGUILayout.TextField("Splat Mask Property Name", m_MaskName);
                GUI.enabled = true;
                m_CustomMaterial = EditorGUILayout.ObjectField("Material", m_CustomMaterial, typeof(Material), allowSceneObjects: false) as Material;
            }
        }

        protected override string ValidateInternal()
        {
            if (m_TileSource != TileSource.CreateNewTile)
            {
                return null;
            }

            if (string.IsNullOrEmpty(m_TileName))
            {
                return "tile name not set";
            }

            if (!Helper.IsPOT(m_MaskTextureResolution))
            {
                return "Mask resolution is not power of 2";
            }

            var path = GetAssetPath();
            if (Directory.Exists(path))
            {
                return "output dir already exists";
            }

            if (!EditorHelper.IsEmptyDirectory(path))
            {
                return "directory is not empty";
            }

            if (string.IsNullOrEmpty(m_OutputDir))
            {
                return "Output dir is null";
            }

            if (!Directory.Exists(m_OutputDir))
            {
                return "output dir does not exist";
            }

            if (string.IsNullOrEmpty(Helper.ToUnityPath(m_OutputDir)))
            {
                return "invalid output path";
            }

            if (m_CustomMaterial == null)
            {
                return "material not set!";
            }

            var texturePropertyNames = new List<string>(m_CustomMaterial.GetTexturePropertyNames());
            if (!texturePropertyNames.Contains(m_MaskName))
            {
                return $"shader property {m_MaskName} not found in shader";
            }

            if (m_UseTerrainLOD)
            {
                if (!Helper.IsPOT((int)m_GridWidth) ||
                    !Helper.IsPOT((int)m_GridHeight))
                {
                    return "Terrain LOD Tile size must be power of 2";
                }
            }

            return null;
        }

        private void CreateTiles()
        {
            var system = World.QueryPlugin<TileSystem>();
            system.ResourceGroupSystem.SelectedGroup.AddInDirectory(GetAssetPath(), true);
            system.TilingUseSelectedGroup();
            system.TexturePainter.MaskName = m_MaskName;
            system.TexturePainter.Resolution = m_MaskTextureResolution;
            (World as EditorWorld).EditorSerialize();
        }

        private string GetAssetPath()
        {
            return $"{m_OutputDir}/{m_TileName}";
        }

        private TileSource m_TileSource = TileSource.CreateNewTile;
        private string m_OutputDir;
        private string m_TileName;
        private float m_Rotation;
        private Material m_CustomMaterial;
        private bool m_UseTerrainLOD = false;
        private TextureFormat m_MaskFormat = TextureFormat.RGBA;
        private string m_MaskName = "_SplatMask";
        private int m_MaskTextureResolution = 512;

        private enum TextureFormat
        {
            RGB,
            RGBA,
        }

        private enum TileSource
        {
            CreateNewTile,
            UserDefined,
        }
    }
}

//XDay