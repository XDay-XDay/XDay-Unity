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
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System.IO;
using XDay.UtilityAPI.Editor;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Math;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TexturePainter
    {
        public void Start()
        {
            End();

            var errorMsg = GeneratePaintInfo(m_Resolution, m_MaskName);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                EditorUtility.DisplayDialog("Error", errorMsg, "OK");
            }

            //设置_SplatMask_After
            m_TileSystem.SetSplatMaskAfter();
        }

        public void End()
        {
            if (m_TextureToPaintInfo.Count == 0)
            {
                return;
            }

            SaveToFile();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var kv in m_TextureToPaintInfo)
                {
                    ReimportTexture(kv.Value.OriginalSetting, kv.Key);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();

                foreach (var kv in m_TextureToPaintInfo)
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(kv.Key));
                    kv.Value.OriginalSetting.Material.SetTexture(m_MaskName, texture);
                }
            }

            m_TextureToPaintInfo = new Dictionary<Texture2D, PaintInfo>();
            m_Tiles = null;
            m_Indicator.Enabled = false;
        }

        public void EndPainting(IntBounds2D area)
        {
            m_Painting = false;
            for (var y = 0; y < m_TileSystem.YTileCount; ++y)
            {
                for (var x = 0; x < m_TileSystem.XTileCount; ++x)
                {
                    var tile = GetTileInfo(x, y);
                    tile?.EndPainting(this, area);
                }
            }
        }

        public void SaveToFile()
        {
            if (m_TextureToPaintInfo.Count == 0)
            {
                return;
            }

            for (var y = 0; y < m_TileSystem.YTileCount; ++y)
            {
                for (var x = 0; x < m_TileSystem.XTileCount; ++x)
                {
                    var tile = GetTileInfo(x, y);
                    var paintInfo = tile?.PaintInfo;
                    if (paintInfo != null &&
                        paintInfo.Dirty)
                    {
                        var path = AssetDatabase.GetAssetPath(paintInfo.Texture);
                        File.WriteAllBytes(path, paintInfo.Texture.EncodeToTGA());
                        AssetDatabase.ImportAsset(path);
                        paintInfo.Dirty = false;
                    }
                }
            }
        }

        public void Prepare()
        {
            m_Painting = true;
            for (var y = 0; y < m_TileSystem.YTileCount; ++y)
            {
                for (var x = 0; x < m_TileSystem.XTileCount; ++x)
                {
                    var tile = GetTileInfo(x, y);
                    tile?.Prepare();
                }
            }
        }

        public TileInfo GetTileInfo(int x, int y)
        {
            TileInfo tile = null;
            if (x >= 0 && x < m_TileSystem.XTileCount &&
                y >= 0 && y < m_TileSystem.YTileCount)
            {
                tile = m_Tiles[y * m_TileSystem.XTileCount + x];
            }
            return tile;
        }

        public void StartAndChangeMaskInfo()
        {
            ParameterWindow.Open("Paint Texture",
                new List<ParameterWindow.Parameter>
                {
                    new ParameterWindow.IntParameter("Resolution", "", Resolution),
                    new ParameterWindow.StringParameter("Mask Name", "", MaskName),
                },
                (p) =>
                {
                    var ok = ParameterWindow.GetInt(p[0], out var resolution);
                    ok &= ParameterWindow.GetString(p[1], out var maskName);
                    if (ok)
                    {
                        End();

                        var errorMsg = GeneratePaintInfo(resolution, maskName);
                        if (string.IsNullOrEmpty(errorMsg))
                        {
                            return true;
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", errorMsg, "OK");
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Invalid parameters", "OK");
                    }
                    return false;
                });
        }

        private void ReimportTexture(ImportSetting setting, Texture2D texture)
        {
            var platform = EditorHelper.QueryPlatformName();
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            importer.alphaSource = setting.AlphaSource;
            importer.isReadable = setting.Readable;
            importer.filterMode = setting.Filter;
            importer.mipmapEnabled = setting.MipMapEnabled;
            importer.textureType = setting.Type;
            importer.wrapMode = setting.Wrap;
            importer.alphaIsTransparency = setting.AlphaIsTransparency;
            importer.sRGBTexture = setting.SRGBTexture;
            var compressionSetting = importer.GetPlatformTextureSettings(platform);
            compressionSetting.overridden = true;
            compressionSetting.format = setting.Format;
            compressionSetting.textureCompression = setting.Compression;
            importer.SetPlatformTextureSettings(compressionSetting);
            importer.SaveAndReimport();
        }

        //将center所在的tile的mask重置为(1,0,0,0)
        private void ResetMask(Vector3 center)
        {
            var coord = m_TileSystem.UnrotatedPositionToCoordinate(center.x, center.z);
            var tile = m_TileSystem.GetTile(coord.x, coord.y);
            if (tile == null)
            {
                return;
            }
            var data = m_Pool.Rent(m_Resolution * m_Resolution);
            var red = new Color(1, 0, 0, 0);
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = red;
            }
            var tileInfo = GetTileInfo(coord.x, coord.y);
            tileInfo.PaintInfo.SetPixels(new Vector2Int(0, 0), new Vector2Int(m_Resolution, m_Resolution), data);
            m_Pool.Return(data);
        }

        private void PaintInternal(Vector3 center, bool subtract)
        {
            if (m_BrushStyleManager.SelectedStyle == null)
            {
                return;
            }

            var rotateBrush = EnableRotation;
            var brush = m_BrushStyleManager.SelectedStyle;
            var paintChannel = Channel;

            var mipLevel = brush.CalculateMipMapLevel(rotateBrush, m_Range);
            var centerPixelX = Mathf.FloorToInt((center.x - m_TileSystem.Origin.x) / m_TileSystem.Width * (m_Resolution * m_TileSystem.XTileCount - 1));
            var centerPixelY = Mathf.FloorToInt((center.z - m_TileSystem.Origin.y) / m_TileSystem.Height * (m_Resolution * m_TileSystem.YTileCount - 1));
            var brushPixelMinX = centerPixelX - m_Range / 2;
            var brushPixelMaxX = brushPixelMinX + m_Range - 1;
            var brushPixelMinY = centerPixelY - m_Range / 2;
            var brushPixelMaxY = brushPixelMinY + m_Range - 1;
            var centerTileCoord = m_TileSystem.UnrotatedPositionToCoordinate(center.x, center.z);

            for (var tileY = 0; tileY < m_TileSystem.YTileCount; ++tileY)
            {
                for (var tileX = 0; tileX < m_TileSystem.XTileCount; ++tileX)
                {
                    var tile = GetTileInfo(tileX, tileY);
                    if (tile != null && tile.PaintInfo != null)
                    {
                        if (m_PaintOneTile && (tileX != centerTileCoord.x || tileY != centerTileCoord.y))
                        {
                            continue;
                        }

                        var ignoreAlpha = tile.PaintInfo.Texture.format == TextureFormat.RGB24 || m_IgnoreAlpha;
                        var tilePixelMinX = m_Resolution * tileX;
                        var tilePixelMinY = m_Resolution * tileY;
                        var overlapMinX = Mathf.Max(brushPixelMinX, tilePixelMinX);
                        var overlapMaxX = Mathf.Min(brushPixelMaxX, tilePixelMinX + m_Resolution - 1);
                        var overlapMinY = Mathf.Max(brushPixelMinY, tilePixelMinY);
                        var overlapMaxY = Mathf.Min(brushPixelMaxY, tilePixelMinY + m_Resolution - 1);
                        if (overlapMinX <= overlapMaxX && overlapMinY <= overlapMaxY)
                        {
                            PaintTileTexture(brush, tile, overlapMinX, overlapMinY, overlapMaxX, overlapMaxY, tilePixelMinX, tilePixelMinY, brushPixelMinX, brushPixelMinY, mipLevel, rotateBrush, paintChannel, ignoreAlpha, subtract);
                        }
                    }
                }
            }
        }
        
        private string Validate(int textureResolution, string maskName)
        {
            var paintableTileCount = 0;
            m_TextureToTileCoordinates = new();
            m_TextureToMaterial = new();
            m_TextureToImportSetting = new();
            for (var i = 0; i < m_TileSystem.YTileCount; ++i)
            {
                for (var j = 0; j < m_TileSystem.XTileCount; ++j)
                {
                    var tile = m_TileSystem.GetTile(j, i);
                    if (tile != null)
                    {
                        var err = QueryMaterialAndMaskTexture(textureResolution, m_TileSystem.Renderer.GetTileGameObject(j, i), maskName, out var ownerMaterial, out var maskTexture);
                        if (string.IsNullOrEmpty(err))
                        {
                            m_TextureToTileCoordinates.TryGetValue(maskTexture, out var list);
                            if (list == null)
                            {
                                list = new List<Vector2Int>();
                                m_TextureToTileCoordinates.Add(maskTexture, list);
                            }
                            list.Add(new Vector2Int(j, i));
                            m_TextureToMaterial[maskTexture] = ownerMaterial;
                            ++paintableTileCount;
                        }
                        else
                        {
                            return err;
                        }
                    }
                }
            }

            if (paintableTileCount == 0)
            {
                return "No paintable tile";
            }

            return "";
        }

        private void CreateTilePaintData(Texture2D texture, List<Vector2Int> sharedTileCoordinates, ImportSetting originalSetting)
        {
            m_TextureToPaintInfo.TryGetValue(texture, out var paintData);
            if (paintData == null)
            {
                paintData = new PaintInfo(texture, originalSetting);
                m_TextureToPaintInfo.Add(texture, paintData);
            }

            for (var i = 0; i < sharedTileCoordinates.Count; ++i)
            {
                var tile = GetTileInfo(sharedTileCoordinates[i].x, sharedTileCoordinates[i].y);
                if (tile != null)
                {
                    tile.PaintInfo = paintData;
                }
            }
        }

        private string QueryMaterialAndMaskTexture(int textureResolution, GameObject gameObject, string maskName, out Material material, out Texture2D maskTexture)
        {
            material = null;
            maskTexture = null;
            foreach (var renderer in gameObject.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.sharedMaterial != null)
                {
                    var texture = renderer.sharedMaterial.GetTexture(maskName) as Texture2D;
                    if (texture != null)
                    {
                        if (textureResolution == texture.width && texture.width == texture.height)
                        {
                            material = renderer.sharedMaterial;
                            maskTexture = texture;
                            return "";
                        }
                    }
                }
            }

            return $"{gameObject.name} has no renderer";
        }

        private void CreateTilesInfoInternal(string maskName)
        {
            foreach (var kv in m_TextureToTileCoordinates)
            {
                var texture = kv.Key;
                var coords = kv.Value;
                foreach (var coord in coords)
                {
                    m_Tiles[coord.y * m_TileSystem.XTileCount + coord.x] = new TileInfo(coord);
                }
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(texture));
                if (texture != null)
                {
                    m_TextureToMaterial[texture].SetTexture(maskName, texture);
                    CreateTilePaintData(texture, coords, m_TextureToImportSetting[texture]);
                }
            }
        }

        private string GeneratePaintInfo(int textureResolution, string maskName)
        {
            var errorMsg = Validate(textureResolution, maskName);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                return errorMsg;
            }

            m_Resolution = textureResolution;
            m_MaskName = maskName;
            m_Tiles = new TileInfo[m_TileSystem.XTileCount * m_TileSystem.YTileCount];

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var kv in m_TextureToTileCoordinates)
                {
                    var texture = kv.Key;
                    var setting = CreateImportSetting(texture, out var importer);
                    m_TextureToImportSetting[texture] = setting;
                    ReimportTexture(texture, importer);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                CreateTilesInfoInternal(m_MaskName);
            }

            m_TextureToImportSetting = null;
            m_TextureToTileCoordinates = null;
            m_TextureToMaterial = null;

            return "";
        }

        private ImportSetting CreateImportSetting(Texture2D texture, out TextureImporter importer)
        {
            importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            var textureCompressionSettings = importer.GetPlatformTextureSettings(EditorHelper.QueryPlatformName());
            return new ImportSetting
            {
                Type = importer.textureType,
                SRGBTexture = importer.sRGBTexture,
                Filter = importer.filterMode,
                Material = m_TextureToMaterial[texture],
                AlphaSource = importer.alphaSource,
                MipMapEnabled = importer.mipmapEnabled,
                Format = textureCompressionSettings.format,
                Readable = importer.isReadable,
                Wrap = importer.wrapMode,
                Compression = textureCompressionSettings.textureCompression,
                AlphaIsTransparency = importer.alphaIsTransparency,
            };
        }

        private void PaintTileTexture(
            IBrushStyle brush,
            TileInfo tile, 
            int overlapMinX, 
            int overlapMinY, 
            int overlapMaxX, 
            int overlapMaxY, 
            int tilePixelMinX, 
            int tilePixelMinY, 
            int brushPixelMinX, 
            int brushPixelMinY, 
            int mipLevel, 
            bool rotateBrush,
            int channel, 
            bool ignoreAlpha,
            bool subtract)
        {

            var overlapWidth = overlapMaxX - overlapMinX + 1;
            var overlapHeight = overlapMaxY - overlapMinY + 1;
            var minX = overlapMinX - tilePixelMinX;
            var minY = overlapMinY - tilePixelMinY;
            var data = m_Pool.Rent(overlapWidth * overlapHeight);
            var newPixels = tile.PaintInfo.NewPixels;
            var intensity = subtract ? -m_Intensity : m_Intensity;
            for (var y = 0; y < overlapHeight; ++y)
            {
                for (var x = 0; x < overlapWidth; ++x)
                {
                    var u = (x + overlapMinX - brushPixelMinX) / (float)m_Range;
                    var v = (y + overlapMinY - brushPixelMinY) / (float)m_Range;
                    var brushPixel = brush.TextureLODLinear(rotateBrush, u, v, mipLevel);
                    var value = newPixels[(minY + y) * m_Resolution + minX + x];
                    if (channel < 4 && Mathf.Approximately(brushPixel.a, 0))
                    {
                        data[y * overlapWidth + x] = value;
                        continue;
                    }

                    var changeDelta = brushPixel * intensity;
                    if (m_NormalizeColor)
                    {
                        value[channel] = Mathf.Max(0, value[channel] + changeDelta.a);
                        var total = value.r + value.g + value.b;
                        if (!ignoreAlpha)
                        {
                            total += value.a;
                        }

                        if (total > 0)
                        {
                            value.r /= total;
                            value.g /= total;
                            value.b /= total;
                            value.a /= total;
                        }
                        else
                        {
                            value.r = 1;
                            value.g = 0;
                            value.b = 0;
                            value.a = 0;
                        }
                        data[y * overlapWidth + x] = value;
                    }
                    else
                    {
                        value[channel] = Mathf.Clamp01(value[channel] + changeDelta.a);
                        data[y * overlapWidth + x] = value;
                    }
                }
            }
            tile.PaintInfo.SetPixels(new Vector2Int(minX, minY), new Vector2Int(overlapWidth, overlapHeight), data);
            m_Pool.Return(data);
        }

        private void ReimportTexture(Texture2D texture, TextureImporter importer)
        {
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = false;
            importer.isReadable = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            var textureCompressionSettings = importer.GetPlatformTextureSettings(EditorHelper.QueryPlatformName());
            textureCompressionSettings.overridden = true;
            textureCompressionSettings.format = GraphicsFormatUtility.HasAlphaChannel(texture.graphicsFormat) ? TextureImporterFormat.RGBA32 : TextureImporterFormat.RGB24;
            textureCompressionSettings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(textureCompressionSettings);
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(texture));
        }
    }
}

//XDay
