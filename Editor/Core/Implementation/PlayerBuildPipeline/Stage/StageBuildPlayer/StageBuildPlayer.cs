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
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using XDay.UtilityAPI;
#if !ENABLE_TUANJIE
using static UnityEditor.Android.UserBuildSettings;
#endif

namespace XDay.Build.Editor
{
    /// <summary>
    /// 编译unity player
    /// </summary>
    [Serializable]
    internal class StageBuildPlayer : PlayerBuildPipelineStage
    {
        public BuildOptions BuildOptions = BuildOptions.None;
        //输出目录
        public string OutputDir = "./Build";
        public string BundleVersion;
        public bool BuildAndroidABB = false;
        public List<string> ExtraScriptDefines = new();

        public StageBuildPlayer(int id) : base(id)
        {
        }

        protected override async UniTask<PreBuildReport> OnPreBuild(PlayerBuildPipeline pipeline)
        {
            if (string.IsNullOrEmpty(OutputDir))
            {
                return await UniTask.FromResult(new PreBuildReport() { ErrorMessage="输出目录未设置", Success = false});
            }

            m_Options = CreateBuildOptions(pipeline);
            return await UniTask.FromResult(new PreBuildReport() { Success = true });
        }

        protected override async UniTask<BuildReport> OnBuild(PlayerBuildPipeline pipeline)
        {
            var report = BuildPipeline.BuildPlayer(m_Options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                return await UniTask.FromResult(new BuildReport() { Success = true, TimeCostSec = report.summary.totalTime.TotalSeconds });
            }
            return await UniTask.FromResult(new BuildReport() { Success = false, ErrorMessage = report.SummarizeErrors(), TimeCostSec = report.summary.totalTime.TotalSeconds });
        }

        private BuildPlayerOptions CreateBuildOptions(PlayerBuildPipeline pipeline)
        {
            bool isAAB = IsBuildAAB(pipeline);

            var target = pipeline.GetTarget();
            var options = new BuildPlayerOptions
            {
                target = target,
                assetBundleManifestPath = "",
                extraScriptingDefines = ExtraScriptDefines.ToArray(),
                locationPathName = GetAppOutputPath(pipeline, target, isAAB),
                options = GetOption(pipeline),
            };

            List<string> scenePaths = new();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenePaths.Add(scene.path);
                }
            }
            options.scenes = scenePaths.ToArray();

            //set build settings
            SetBuildSettings(options.target, pipeline, isAAB);

            return options;
        }

        private string GetAppOutputPath(PlayerBuildPipeline pipeline, BuildTarget target, bool isAAB)
        {
            var outputDir = pipeline.GetCommandLineArgValue("output_path");
            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = OutputDir;
            }
            Debug.Log($"App output path: {outputDir}");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var ext = isAAB ? "apk" : "aab";
            return $"{outputDir}/{PlayerSettings.applicationIdentifier}-{PlayerSettings.bundleVersion}-{GetBuildNumber(target)}.{ext}";
        }

        private string GetBuildNumber(BuildTarget target)
        {
            if (target == BuildTarget.iOS)
            {
                return PlayerSettings.iOS.buildNumber;
            }
            else if (target == BuildTarget.Android)
            {
                return PlayerSettings.Android.bundleVersionCode.ToString();
            }
            else
            {
                Debug.LogError($"Unknown target: {target}");
                return "";
            }
        }

        private void SetBuildSettings(BuildTarget target, PlayerBuildPipeline pipeline, bool isAAB)
        {
            PlayerSettings.bundleVersion = GetBundleVersion(pipeline);
            if (target == BuildTarget.iOS)
            {
                PlayerSettings.iOS.buildNumber = (int.Parse(PlayerSettings.iOS.buildNumber) + 1).ToString();
                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            }
            else if (target == BuildTarget.Android)
            {
                PlayerSettings.Android.bundleVersionCode = PlayerSettings.Android.bundleVersionCode + 1;
                PlayerSettings.Android.keystoreName = EditorHelper.GetJsonValue("Env/env.json", "Android.KeystoreName");
                PlayerSettings.Android.keyaliasName = EditorHelper.GetJsonValue("Env/env.json", "Android.KeyAliasName");
                PlayerSettings.Android.keystorePass = EditorHelper.GetJsonValue("Env/env.json", "Android.KeystorePassword");
                PlayerSettings.Android.keyaliasPass = EditorHelper.GetJsonValue("Env/env.json", "Android.KeyAliasPassword");
                if (isAAB)
                {
#if ENABLE_TUANJIE
                    PlayerSettings.Android.useAPKExpansionFiles = true;
#else
                    PlayerSettings.Android.splitApplicationBinary = true;
#endif
                    EditorUserBuildSettings.buildAppBundle = true;
                }
                else
                {
                    
                    EditorUserBuildSettings.buildAppBundle = false;
                }
                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
#if !ENABLE_TUANJIE
                DebugSymbols.level = Unity.Android.Types.DebugSymbolLevel.Full;
#endif
            }
        }

        private bool IsBuildAAB(PlayerBuildPipeline pipeline)
        {
            string buildAAB = pipeline.GetCommandLineArgValue("build_aab");
            if (buildAAB.ToLower() == "true")
            {
                return true;
            }
            return false;
        }

        private string GetBundleVersion(PlayerBuildPipeline pipeline)
        {
            string versionText = pipeline.GetCommandLineArgValue("bundle_version");
            if (!string.IsNullOrEmpty(versionText))
            {
                return versionText;
            }

            if (!string.IsNullOrEmpty(BundleVersion))
            {
                return BundleVersion;
            }

            return PlayerSettings.bundleVersion;
        }

        private BuildOptions GetOption(PlayerBuildPipeline pipeline)
        {
            string optionText = pipeline.GetCommandLineArgValue("build_option");
            if (string.IsNullOrEmpty(optionText))
            {
                return BuildOptions;
            }

            if (optionText.ToLower() == "debug")
            {
                return BuildOptions.AllowDebugging | 
                    BuildOptions.Development | 
                    BuildOptions.EnableDeepProfilingSupport | 
                    BuildOptions.DetailedBuildReport | 
                    BuildOptions.WaitForPlayerConnection;
            }
            return BuildOptions.None;
        }

        private BuildPlayerOptions m_Options;
    }
}
