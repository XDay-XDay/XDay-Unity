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

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.AssetPipeline.Editor
{
    [Serializable]
    internal partial class StageTextureProcess : AssetPipelineStage
    {
        //需处理的目录
        public string RootDirectory = "Assets";
        public List<TextureImportRule> Rules = new();

        public StageTextureProcess(int id) : base(id)
        {
        }

        protected override async UniTask<BuildReport> OnBuild(AssetPipeline pipeline)
        {
            SortRules();

            AssetDatabase.StartAssetEditing();
            try
            {
                var allTextures = EditorHelper.QueryAssets<Texture>();
                foreach (var texture in allTextures)
                {
                    var assetPath = AssetDatabase.GetAssetPath(texture);
                    MatchRule(assetPath, texture);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            return await UniTask.FromResult(new BuildReport() { Success = true });
        }

        private void MatchRule(string assetPath, Texture texture)
        {
            foreach (var rule in m_SortedRules)
            {
                if (rule.Enable)
                {
                    var pattern = rule.PathPattern.Replace("*", "[^/]+?");
                    if (Regex.IsMatch(assetPath, pattern, RegexOptions.IgnoreCase))
                    {
                        ReimportTexture(texture, assetPath, rule);
                        break;
                    }
                }
            }
        }

        private void SortRules()
        {
            m_SortedRules = new();
            m_SortedRules.AddRange(Rules);
            m_SortedRules.Sort((a, b) =>
            {
                var dirPathA = Helper.GetFolderPath(a.PathPattern);
                var dirPathB = Helper.GetFolderPath(b.PathPattern);
                return dirPathB.Length.CompareTo(dirPathA.Length);
            });
        }

        private void ReimportTexture(Texture texture, string assetPath, TextureImportRule rule)
        {
            Debug.LogError($"处理贴图: {AssetDatabase.GetAssetPath(texture)}, rule: {rule.PathPattern}");

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = rule.SpriteMeshType;
            importer.SetTextureSettings(settings);
            importer.textureCompression = rule.Compression;
            importer.mipmapEnabled = rule.EnableMipmap;
            if (rule.Readable != ReadableMode.DontCare)
            {
                importer.isReadable = rule.Readable == ReadableMode.Readable;
            }
            if (rule.WrapMode != TextureWrapModeEx.DontCare)
            {
                importer.wrapMode = (TextureWrapMode)rule.WrapMode;
            }
            if (rule.FilterMode != FilterModeEx.DontCare)
            {
                importer.filterMode = (FilterMode)rule.FilterMode;
            }
            if (importer.maxTextureSize > rule.MaxTextureSize)
            {
                importer.maxTextureSize = rule.MaxTextureSize;
            }
            importer.SaveAndReimport();
        }

        private List<TextureImportRule> m_SortedRules;
    }

    [Serializable]
    public class TextureImportRule
    {
        public string Description = "";
        //Assets/Res/*.tga或Assets/Res/*.*
        public string PathPattern = "";
        public int MaxTextureSize = 1024;
        public bool EnableMipmap = true;
        public ReadableMode Readable = ReadableMode.DontCare;
        public TextureWrapModeEx WrapMode = TextureWrapModeEx.DontCare;
        public FilterModeEx FilterMode = FilterModeEx.DontCare;
        public SpriteMeshType SpriteMeshType = SpriteMeshType.FullRect;
        public TextureImporterCompression Compression = TextureImporterCompression.Compressed;
        public bool Enable = true;
        public bool Show = true;
    }

    public enum ReadableMode
    {
        DontCare,
        Readable,
        NotReadable,
    }

    public enum TextureWrapModeEx
    {
        DontCare = -1,
        Repeat,
        Clamp,
        Mirror,
        MirrorOnce
    }

    public enum FilterModeEx
    {
        DontCare = -1,
        Point,
        Bilinear,
        Trilinear
    }
}
