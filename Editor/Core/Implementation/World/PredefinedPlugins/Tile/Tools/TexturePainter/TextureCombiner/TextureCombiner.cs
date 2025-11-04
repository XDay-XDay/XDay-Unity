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
using System.IO;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile.Editor
{
    internal class TextureCombiner
    {
        /// <summary>
        /// 将某个区域的所有mask贴图合并成一张
        /// </summary>
        public Texture2D Combine(List<Texture2D> maskTextures, int minX, int minY, int maxX, int maxY, string outputPath)
        {
            var width = maxX - minX + 1;
            var height = maxY - minY + 1;
            Debug.Assert(maskTextures.Count == width * height);

            var errorMsg = Validate(maskTextures, width, height);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                Debug.LogError(errorMsg);
                return null;
            }

            var resolution = maskTextures[0].width;
            var combinedResolutionX = resolution * width;
            var combinedResolutionY = resolution * height;

            if (combinedResolutionX > m_MaxTextureSize ||
                combinedResolutionY > m_MaxTextureSize)
            {
                Debug.LogError($"贴图大小{combinedResolutionX}X{combinedResolutionY}超过最大支持的大小{m_MaxTextureSize},无法合并贴图!");
                return null;
            }

            Color32[] combinedPixels = new Color32[combinedResolutionX * combinedResolutionY];
            var idx = 0;
            for (var i = minY; i <= maxY; i++)
            {
                for (var j = minX; j <= maxX; j++)
                {
                    SetBlock(combinedPixels, j - minX, i - minY, maskTextures[idx++], combinedResolutionX);
                }
            }

            var texture = new Texture2D(combinedResolutionX, combinedResolutionY, TextureFormat.RGBA32, false);
            texture.SetPixels32(combinedPixels);
            texture.Apply();

            var data = texture.EncodeToTGA();
            var path = $"{outputPath}/combined_mask.tga";
            File.WriteAllBytes(path, data);
            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.maxTextureSize = m_MaxTextureSize;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// 将合并的贴图分解到各个地块
        /// </summary>
        public List<byte[]> Split(Texture2D combinedTexture, int minX, int minY, int maxX, int maxY, int blockSize)
        {
            if (combinedTexture == null)
            {
                return new();
            }

            var width = maxX - minX + 1;
            var height = maxY - minY + 1;
            if (combinedTexture.width / blockSize != width ||
                combinedTexture.height / blockSize != height)
            {
                var msg = "贴图数量不匹配, 无法分解";
                EditorUtility.DisplayDialog("出错了", msg, "确定");
                Debug.LogError(msg);
                return new();
            }

            Texture2D temp = new Texture2D(blockSize, blockSize, TextureFormat.RGBA32, false);
            List<byte[]> textures = new();
            Color32[] combinedPixels = combinedTexture.GetPixels32();
            for (var i = minY; i <= maxY; i++)
            {
                for (var j = minX; j <= maxX; j++)
                {
                    var pixels = GetBlock(combinedPixels, j - minX, i - minY, combinedTexture.width, blockSize);
                    temp.SetPixels32(pixels);
                    temp.Apply();
                    textures.Add(temp.EncodeToTGA());
                }
            }

            Helper.DestroyUnityObject(temp);

            return textures;
        }

        private string Validate(List<Texture2D> maskTextures, int width, int height)
        {
            if (maskTextures.Count != width * height)
            {
                return "贴图数量和区域大小不相同!";
            }
           
            if (maskTextures.Count == 0)
            {
                return "没有贴图";
            }

            if (!maskTextures[0].isReadable)
            {
                return "贴图不能读写";
            }

            var rx = maskTextures[0].width;
            var ry = maskTextures[0].height;
            if (rx != ry)
            {
                return "贴图宽高不同";
            }

            for (var i = 1; i < maskTextures.Count; i++)
            {
                if (maskTextures[i] == null)
                {
                    return "贴图为空";
                }

                if (maskTextures[i].width != rx ||
                    maskTextures[i].height != ry)
                {
                    return "贴图之间大小不同";
                }
            }

            return null;
        }

        private void SetBlock(Color32[] combinedPixels, int blockX, int blockY, Texture2D texture2D, int combinedWidth)
        {
            var blockSize = texture2D.width;
            var pixels = texture2D.GetPixels32();
            var x = blockX * blockSize;
            var y = blockY * blockSize;
            for (var i = 0; i < blockSize; ++i)
            {
                for (var j = 0; j < blockSize; ++j)
                {
                    var dstIdx = (y + i) * combinedWidth + x + j;
                    var srcIdx = i * blockSize + j;
                    combinedPixels[dstIdx] = pixels[srcIdx];
                }
            }
        }

        private Color32[] GetBlock(Color32[] combinedPixels, int blockX, int blockY, int combinedWidth, int blockSize)
        {
            Color32[] pixels = new Color32[blockSize * blockSize];
            var x = blockX * blockSize;
            var y = blockY * blockSize;
            for (var i = 0; i < blockSize; ++i)
            {
                for (var j = 0; j < blockSize; ++j)
                {
                    var srcIdx = (y + i) * combinedWidth + x + j;
                    var dstIdx = i * blockSize + j;
                    pixels[dstIdx] = combinedPixels[srcIdx];
                }
            }
            return pixels;
        }

        private const int m_MaxTextureSize = 8192;
    }
}
