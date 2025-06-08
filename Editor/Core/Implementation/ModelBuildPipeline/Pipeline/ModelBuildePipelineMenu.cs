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

using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.ModelBuildPipeline.Editor
{
    public static class ModelBuildePipelineMenu
    {
        [MenuItem("Assets/模型管线/1.整理模型文件夹", false, 1)]
        private static void RefactorModel()
        {
            var editor = ModelBuildPipelineEditor.Open();
            var pipeline = editor.GetActivePipeline();
            if (pipeline == null)
            {
                Debug.LogError("没有找到ModelBuildePipeline");
                return;
            }
            foreach (var obj in Selection.objects)
            {
                var modelFolder = obj as DefaultAsset;
                if (modelFolder != null)
                {
                    pipeline.RefractorModelFolder(AssetDatabase.GetAssetPath(modelFolder));
                }
            }
        }

        [MenuItem("Assets/模型管线/2.构建模型", false, 2)]
        private static void BuildSelection()
        {
            var editor = ModelBuildPipelineEditor.Open();
            var pipeline = editor.GetActivePipeline();
            if (pipeline == null)
            {
                Debug.LogError("没有找到ModelBuildePipeline");
                return;
            }
            foreach (var obj in Selection.objects)
            {
                var modelFolder = obj as DefaultAsset;
                if (modelFolder != null)
                {
                    var path = AssetDatabase.GetAssetPath(modelFolder);
                    pipeline.CreateSettings(path);
                    pipeline.Build(path);
                }
            }
        }

        [MenuItem("Assets/模型管线/3.创建缺失模型设置", false, 3)]
        private static void CreateModelSettings()
        {
            var editor = ModelBuildPipelineEditor.Open();
            var pipeline = editor.GetActivePipeline();
            if (pipeline == null)
            {
                Debug.LogError("没有找到ModelBuildePipeline");
                return;
            }
            foreach (var obj in Selection.objects)
            {
                var modelFolder = obj as DefaultAsset;
                if (modelFolder != null)
                {
                    pipeline.CreateSettings(AssetDatabase.GetAssetPath(modelFolder));
                }
            }
        }

        [MenuItem("GameObject/模型管线/同步模型")]
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

        [MenuItem("Assets/模型管线/4.复制设置文件", false, 4)]
        private static void CopySettingFiles()
        {
            var editor = ModelBuildPipelineEditor.Open();
            var pipeline = editor.GetActivePipeline();
            if (pipeline == null)
            {
                Debug.LogError("没有找到ModelBuildePipeline");
                return;
            }
            foreach (var obj in Selection.objects)
            {
                var modelFolder = obj as DefaultAsset;
                if (modelFolder != null)
                {
                    pipeline.CopySettings(AssetDatabase.GetAssetPath(modelFolder));
                }
            }
        }
    }
}
