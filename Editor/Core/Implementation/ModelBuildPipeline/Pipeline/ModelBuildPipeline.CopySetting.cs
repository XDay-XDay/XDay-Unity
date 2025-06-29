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

namespace XDay.ModelBuildPipeline.Editor
{
    public partial class ModelBuildPipeline
    {
        /// <summary>
        /// 将一个模型的Setting复制到同级的其他模型中
        /// </summary>
        /// <param name="rootFolder"></param>
        public static void CopySettings(string rootFolder)
        {
            string modelFolder = $"{rootFolder}/{MODEL_FOLDER_NAME}";
            var fbxModel = GetModel(modelFolder);
            if (fbxModel == null)
            {
                Debug.LogError("不是有效的模型文件夹");
                return;
            }

            List<string> otherRootFolders = GetSameLevelRootFolders(rootFolder);
            foreach (var otherRootFolder in otherRootFolders)
            {
                DoCopySettings(rootFolder, otherRootFolder);
            }
        }

        private static List<string> GetSameLevelRootFolders(string rootFolder)
        {
            List<string> rootFolders = new();
            var parentFolder = Helper.GetFolderPath(rootFolder);
            foreach (var dir in Directory.EnumerateDirectories(parentFolder))
            {
                var validDir = Helper.ToUnityPath(dir);
                string modelFolder = $"{validDir}/{MODEL_FOLDER_NAME}";
                var model = GetModel(modelFolder);
                if (model != null && validDir != rootFolder)
                {
                    rootFolders.Add(validDir);
                }
            }
            return rootFolders;
        }

        private static void DoCopySettings(string srcRootFolder, string dstRootFolder)
        {
            List<ModelBuildPipelineStageSetting> srcSettings = GetStageSettings(srcRootFolder);
            List<ModelBuildPipelineStageSetting> dstSettings = GetStageSettings(dstRootFolder);

            foreach (var srcSetting in srcSettings)
            {
                bool found = false;
                foreach (var dstSetting in dstSettings)
                {
                    if (dstSetting.GetType() == srcSetting.GetType())
                    {
                        found = true;
                        dstSetting.CopyFrom(srcSetting);
                        break;
                    }
                }
                if (!found)
                {
                    var srcPath = AssetDatabase.GetAssetPath(srcSetting);
                    var newPath = $"{dstRootFolder}/{SETTING_FOLDER_NAME}/{Helper.GetPathName(srcPath, true)}";
                    AssetDatabase.CopyAsset(srcPath, newPath);
                }
            }
        }

        private static List<ModelBuildPipelineStageSetting> GetStageSettings(string rootFolder)
        {
            var settingFolder = $"{rootFolder}/{SETTING_FOLDER_NAME}";
            return EditorHelper.QueryAssets<ModelBuildPipelineStageSetting>(new string[] { settingFolder }, true);
        }

        internal void RemoveInvalidStages()
        {
            for (var i = m_Stages.Count - 1; i >= 0; --i)
            {
                if (m_Stages[i] == null)
                {
                    m_Stages.RemoveAt(i);
                }
            }

            foreach (var stage in m_Stages)
            {
                if (stage != null)
                {
                    for (var i = stage.PreviousStageIDs.Count - 1; i >= 0; --i)
                    {
                        if (!ContainsStage(stage.PreviousStageIDs[i]))
                        {
                            stage.RemovePreviousStage(stage.PreviousStageIDs[i]);
                        }
                    }
                }
            }
        }
    }
}
