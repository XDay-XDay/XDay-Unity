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

namespace XDay.ModelBuildPipeline.Editor
{
    public static class ModelBuildePipelineMenu
    {
        [MenuItem("Assets/模型管线/1.选中文件夹,生成标准模型文件夹结构", false, 1)]
        private static void CreateModelFolder()
        {
            foreach (var obj in Selection.objects)
            {
                var modelFolder = obj as DefaultAsset;
                if (modelFolder != null)
                {
                    ModelBuildPipeline.CreateModelFolder(AssetDatabase.GetAssetPath(modelFolder), recursive:true);
                }
            }
        }

        [MenuItem("Assets/模型管线/2.选中模型文件夹,构建Prefab", false, 2)]
        private static void BuildSelection()
        {
            List<string> rootFolders = new();
            foreach (var obj in Selection.objects)
            {
                var modelFolder = obj as DefaultAsset;
                if (modelFolder != null)
                {
                    rootFolders.Add(AssetDatabase.GetAssetPath(modelFolder));
                }
            }

            ModelPipelineBuilder.BuildFolders(rootFolders);
        }

        [MenuItem("GameObject/模型管线/将Prefab的修改同步到模型配置文件")]
        private static void Sync()
        {
            var scenePrefab = EditorHelper.GetEditingPrefab();
            if (scenePrefab != null)
            {
                var editor = ModelBuildPipelineEditor.Open();
                var pipeline = editor.GetActivePipeline();
                if (pipeline != null)
                {
                    pipeline.SyncSetting(scenePrefab);
                }
                else
                {
                    Debug.LogError("没有找到ModelBuildePipeline");
                }
            }
        }

        [MenuItem("Assets/模型管线/3.将一个模型的Setting复制到同级的其他模型中", false, 3)]
        private static void CopySettingFiles()
        {
            var modelFolder = Selection.activeObject as DefaultAsset;
            if (modelFolder != null)
            {
                ModelBuildPipeline.CopySettings(AssetDatabase.GetAssetPath(modelFolder));
            }
        }
    }
}
