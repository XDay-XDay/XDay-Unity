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
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.ModelBuildPipeline.Editor
{
    public static class ModelPipelineBuilder
    {
        public static void BuildFolders(List<string> rootFolders)
        {
            var editor = ModelBuildPipelineEditor.Open();

            foreach (var folderPath in rootFolders)
            {
                var unityFolderPath = Helper.ToUnityPath(folderPath);

                var pipeline = GetPipeline(unityFolderPath);
                if (pipeline == null)
                {
                    pipeline = editor.GetActivePipeline();
                    if (pipeline == null)
                    {
                        Debug.LogError("没有找到ModelBuildePipeline");
                        return;
                    }
                }

                m_BuildItems.Clear();

                m_ActivePipelines.Push(pipeline);

                BuildOneFolder(unityFolderPath);

                m_ActivePipelines.Pop();

                m_BuildItems.Sort((a, b) =>
                {
                    return a.Pipeline.SortOrder.CompareTo(b.Pipeline.SortOrder);
                });
                foreach (var item in m_BuildItems)
                {
                    Debug.Log($"--------------使用{item.Pipeline.name}构建模型{item.RootFolderPath}----------------");
                    item.Pipeline.CreateSettings(item.RootFolderPath);
                    item.Pipeline.Build(item.RootFolderPath, false);
                }
                m_BuildItems.Clear();
            }
        }

        private static void BuildOneFolder(string rootFolder)
        {
            bool pushed = false;
            var pipeline = GetPipeline(rootFolder);
            if (pipeline != null)
            {
                pushed = true;
                m_ActivePipelines.Push(pipeline);
            }

            m_ActivePipelines.TryPeek(out var activePipeline);
            if (activePipeline != null)
            {
                if (activePipeline.IsModelFolder($"{rootFolder}/{ModelBuildPipeline.MODEL_FOLDER_NAME}"))
                {
                    m_BuildItems.Add(new BuildFolder() { Pipeline = activePipeline, RootFolderPath = rootFolder });
                }
                else
                {
                    foreach (var dir in Directory.EnumerateDirectories(rootFolder))
                    {
                        BuildOneFolder(Helper.ToUnityPath(dir));
                    }
                }
            }

            if (pushed)
            {
                m_ActivePipelines.Pop();
            }
        }

        /// <summary>
        /// 获取文件夹应该用哪个Pipeline来构建
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private static ModelBuildPipeline GetPipeline(string folder)
        {
            var metadata = EditorHelper.QueryOneAsset<ModelBuildMetadata>(folder);
            if (metadata != null)
            {
                var pipelines = EditorHelper.QueryAssets<ModelBuildPipeline>();
                foreach (var pipeline in pipelines)
                {
                    if (pipeline.name == metadata.ModelBuildPipelineName)
                    {
                        return pipeline;
                    }
                }
                Debug.LogError($"{folder}Metadata存在但是没有找到名为{metadata.ModelBuildPipelineName}的Pipeline!");
            }
            return null;
        }

        private static readonly Stack<ModelBuildPipeline> m_ActivePipelines = new();
        private static List<BuildFolder> m_BuildItems = new();
        
        private class BuildFolder
        {
            public string RootFolderPath;
            public ModelBuildPipeline Pipeline;
        }
    }
}
