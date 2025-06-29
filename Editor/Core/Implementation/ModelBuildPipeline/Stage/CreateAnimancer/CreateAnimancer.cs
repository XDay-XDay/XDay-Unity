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
using System.Collections.Generic;
using System.IO;
using Animancer;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.ModelBuildPipeline.Editor
{
    /// <summary>
    /// 创建动画信息
    /// </summary>
    [Serializable]
    [StageDescription("Animancer动画", "创建Animancer动画组件")]
    public class CreateAnimancer : ModelBuildPipelineStage
    {
        public override Type SettingType => typeof(CreateAnimancerStageSetting);

        public CreateAnimancer(int id) : base(id)
        {
        }

        protected override bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            m_AnimationCount = new();

            var setting = GetStageSetting<CreateAnimancerStageSetting>(rootFolder);
            if (root.GetComponent<Animator>() == null)
            {
                root.AddComponent<Animator>();
            }
            var animancer = root.AddComponent<AnimancerComponent>();
            animancer.ActionOnDisable = setting.DisableAction;
            var clipSetting = animancer.gameObject.AddComponent<AnimationClips>();
            clipSetting.Clips.Clear();
            var anims = FindAnimationClips(rootFolder);
            foreach (var animInfo in anims)
            {
                ReimportClip(AssetDatabase.GetAssetPath(animInfo.Clip), setting);

                AddAnimation(animInfo.FileName);

                clipSetting.Clips.Add(new AnimClipSetting() { Name = animInfo.FileName, Clip = animInfo.Clip });
            }
            return true;
        }

        public override void SyncSetting(GameObject root, string rootFolder)
        {
        }

        /// <summary>
        /// 获取某种动画的个数
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public int GetAnimationCount(string prefix)
        {
            m_AnimationCount.TryGetValue(prefix, out var count);
            return count;
        }

        public void AddAnimation(string fileName)
        {
            string prefix;
            var pos = fileName.IndexOf("_");
            if (pos >= 0)
            {
                prefix = fileName.Substring(0, pos);
            }
            else
            {
                prefix = fileName;
            }
            if (m_AnimationCount.TryGetValue(prefix, out var count))
            {
                m_AnimationCount[prefix] = count + 1;
            }
            else
            {
                m_AnimationCount.Add(prefix, 1);
            }
        }

        private List<AnimInfo> FindAnimationClips(string rootFolder)
        {
            List<AnimInfo> anims = new();
            foreach (var filePath in Directory.EnumerateFiles($"{rootFolder}/{ModelBuildPipeline.ANIMATION_FOLDER_NAME}"))
            {
                if (filePath.ToLower().EndsWith(".fbx"))
                {
                    var validFilePath = Helper.ToUnityPath(filePath);
                    var fileName = Helper.GetPathName(validFilePath, false);
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(validFilePath);
                    anims.Add(new AnimInfo()
                    {
                        Clip = clip,
                        FileName = fileName
                    });
                }
            }
            return anims;
        }

        private void ReimportClip(string modelPath, CreateAnimancerStageSetting animSetting)
        {
            ModelImporter modelImporter = ModelImporter.GetAtPath(modelPath) as ModelImporter;
            if (modelImporter == null) return;

            var fileName = Helper.GetPathName(modelPath, false);
            // 启用动画导入
            modelImporter.importAnimation = true;

            // 获取所有动画剪辑
            ModelImporterClipAnimation[] clipAnimations = modelImporter.defaultClipAnimations;

            // 修改每个动画剪辑的设置
            foreach (var clip in clipAnimations)
            {
                if (fileName.StartsWith(animSetting.IdleAnimationPrefix) ||
                    fileName.StartsWith(animSetting.RunAnimationPrefix))
                {
                    clip.loopTime = true;
                }
                clip.name = fileName;
            }
            // 应用修改后的剪辑设置
            modelImporter.clipAnimations = clipAnimations;
            modelImporter.SaveAndReimport();
        }

        private class AnimInfo
        {
            public string FileName;
            public AnimationClip Clip;
        }

        private Dictionary<string, int> m_AnimationCount = new();
    }
}