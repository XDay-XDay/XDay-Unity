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
        /// build模型的prefab
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <returns></returns>
        public bool Build(string rootFolder)
        {
            string modelFolder = $"{rootFolder}/{MODEL_FOLDER_NAME}";
            var fbxModel = GetModel(modelFolder);
            if (fbxModel == null)
            {
                foreach (var dir in Directory.EnumerateDirectories(rootFolder))
                {
                    Build(Helper.ToUnityPath(dir));
                }
                return false;
            }

            var inst = GameObject.Instantiate(fbxModel);
            inst.name = fbxModel.name;

            SortStages();

            foreach (var stage in m_Stages)
            {
                stage.Build(fbxModel, inst, rootFolder, this);
            }

            var root = new GameObject(fbxModel.name);
            inst.name = "Root";
            inst.transform.SetParent(root.transform, true);
            root.layer = inst.layer;
            var prefabPath = $"{rootFolder}/{root.name}.prefab";
            FileUtil.DeleteFileOrDirectory(prefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.Refresh();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            foreach (var stage in m_Stages)
            {
                stage.PostBuild(prefab, this);
            }
            return true;
        }

        /// <summary>
        /// 按照依赖关系排序
        /// </summary>
        private void SortStages()
        {
            TopologySort<int> depGraph = new();
            foreach (var stage in m_Stages)
            {
                foreach (var prevStageID in stage.PreviousStageIDs)
                {
                    depGraph.AddEdge(prevStageID, stage.ID);
                }
            }

            List<int> sortedStageIDs = new();
            depGraph.Sort(sortedStageIDs);

            m_Stages.Sort((a, b) =>
            {
                var ai = sortedStageIDs.IndexOf(a.ID);
                var bi = sortedStageIDs.IndexOf(b.ID);
                return ai.CompareTo(bi);
            });
        }
    }
}
