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
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.PlayerBuildPipeline.Editor
{
    /// <summary>
    /// 生成asset bundle
    /// </summary>
    [Serializable]
    internal class StageBuildAssetBundle : PlayerBuildPipelineStage
    {
        /// <summary>
        /// asset bundle输出目录
        /// </summary>
        public string OutputDir = "./AssetBundle";
        public AssetBundleRuleConfig RuleConfig;

        public StageBuildAssetBundle(int id) : base(id)
        {
        }

        protected override async UniTask<PreBuildReport> OnPreBuild(PlayerBuildPipeline pipeline)
        {
            m_OutputDir = GetOutputDir(pipeline);
            string errorMsg = null;
            if (string.IsNullOrEmpty(m_OutputDir))
            {
                errorMsg = "输出目录未设置\n";
            }

            m_RuleConfig = RuleConfig;
            if (m_RuleConfig == null)
            {
                m_RuleConfig = EditorHelper.QueryAsset<AssetBundleRuleConfig>();
            }
            if (m_RuleConfig == null)
            {
                errorMsg += "没有AssetBundle分包规则\n";
            }

            return await UniTask.FromResult(new PreBuildReport() { Success = string.IsNullOrEmpty(errorMsg), ErrorMessage = errorMsg});
        }

        protected override async UniTask<BuildReport> OnBuild(PlayerBuildPipeline pipeline)
        {
            Helper.CreateDirectory(m_OutputDir);

            EditorHelper.DeleteFolderContent(m_OutputDir);

            var option = GetOption(pipeline);

            var bundleBuilds = m_RuleConfig.GenerateAssetBundleBuilds();

            Stopwatch watch = new();
            watch.Start();
            string errorMsg = null;
            AssetBundleManifest manifest = null;
            try
            {
                manifest = BuildPipeline.BuildAssetBundles(
                    m_OutputDir,
                    bundleBuilds,
                    option,
                    pipeline.GetTarget()
                );
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }

            var success = string.IsNullOrEmpty(errorMsg) && manifest != null;
            if (success)
            {
                Helper.CreateDirectory(Application.streamingAssetsPath);
                FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath);
                FileUtil.CopyFileOrDirectory(m_OutputDir, Application.streamingAssetsPath);
                AssetDatabase.Refresh();
            }

            return await UniTask.FromResult(new BuildReport() { 
                Success = success, 
                ErrorMessage = errorMsg, 
                TimeCostSec = watch.ElapsedMilliseconds / 1000f });
        }

        private BuildAssetBundleOptions GetOption(PlayerBuildPipeline pipeline)
        {
            return
                BuildAssetBundleOptions.AssetBundleStripUnityVersion |
                BuildAssetBundleOptions.ChunkBasedCompression | 
                BuildAssetBundleOptions.StrictMode;
        }

        private string GetOutputDir(PlayerBuildPipeline pipeline)
        {
            var output = pipeline.GetCommandLineArgValue("ab_output_dir");
            if (!string.IsNullOrEmpty(output))
            {
                return output;
            }
            return OutputDir;
        }

        private string m_OutputDir;
        private AssetBundleRuleConfig m_RuleConfig;
    }
}
