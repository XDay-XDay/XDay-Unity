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

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.ModelBuildPipeline.Editor
{
    public partial class ModelBuildPipeline
    {
        /// <summary>
        /// 创建缺失的模型配置文件,只创建不删除
        /// </summary>
        /// <param name="rootFolder"></param>
        public void CreateSettings(string rootFolder)
        {
            string modelFolder = $"{rootFolder}/{MODEL_FOLDER_NAME}";
            var fbxModel = GetModel(modelFolder);
            if (fbxModel == null)
            {
                foreach (var dir in Directory.EnumerateDirectories(rootFolder))
                {
                    CreateSettings(Helper.ToUnityPath(dir));
                }
                return;
            }

            string settingFolder = $"{rootFolder}/{SETTING_FOLDER_NAME}";
            var existedSettings = EditorHelper.QueryAssets<ModelBuildPipelineStageSetting>(new string[] { settingFolder }, true);

            foreach (var stage in m_Stages)
            {
                bool found = false;
                foreach (var setting in existedSettings)
                {
                    if (stage.SettingType == setting.GetType())
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    CreateSetting(stage.SettingType, settingFolder);
                }
            }
        }

        private void CreateSetting(Type settingType, string settingFolder)
        {
            var setting = ScriptableObject.CreateInstance(settingType) as ModelBuildPipelineStageSetting;
            setting.OnCreate();
            AssetDatabase.CreateAsset(setting, $"{settingFolder}/{settingType.Name}.asset");
        }
    }
}
