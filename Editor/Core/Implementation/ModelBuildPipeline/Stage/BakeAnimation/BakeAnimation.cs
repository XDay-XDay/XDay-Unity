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
using UnityEditor;
using UnityEngine;
using XDay.AnimationAPI;
using XDay.AnimationAPI.Editor;

namespace XDay.ModelBuildPipeline.Editor
{
    [Serializable]
    [StageDescription("Bake Animation", "使用烘培动画")]
    internal class BakeAnimation : ModelBuildPipelineStage
    {
        public override Type SettingType => typeof(BakeAnimationStageSetting);

        public BakeAnimation(int id) : base(id)
        {
        }

        protected override bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            var s = GetStageSetting<BakeAnimationStageSetting>(rootFolder);

            var bakeShader = SelectBakeShader(s, pipeline);
            if (bakeShader == null)
            {
                Fail($"{model.name}没有设置烘培Shader");
                return false;
            }

            var animations = pipeline.GetAnimations(rootFolder);
            if (animations.Count == 0)
            {
                Fail($"{model.name}没有找到动画文件");
                return false;
            }

            root.tag = AnimationDefine.ANIM_SAMPLE_NAME;

            var bakeSetting = root.AddComponent<GPUAnimationBakeSetting>();
            bakeSetting.Setting.Shader = bakeShader;
            bakeSetting.Setting.Animations = animations;
            bakeSetting.Setting.AnimationType = s.AnimationType;
            bakeSetting.Setting.RenderMode = s.RenderMode;
            bakeSetting.Setting.AdvancedSetting.OutputFolderName = "Baked";
            bakeSetting.Setting.AdvancedSetting.RenderQueue = s.RenderQueue;
            bakeSetting.Setting.AdvancedSetting.DeleteOutputFolder = s.DeleteOutputFolder;
            bakeSetting.Setting.AdvancedSetting.DeleteRigs = s.DeleteRigs;
            bakeSetting.Setting.AdvancedSetting.SampleFrameInterval = s.SampleFrameInterval;

            bool success = GPUAnimationBakerManager.Bake(root, rootFolder);
            if (!success)
            {
                Fail($"烘培{root.name}失败");
            }

            m_BakedPrefabPath = bakeSetting.Setting.PrefabPath();

            return success;
        }

        protected override bool OnPostBuild(GameObject prefab, ModelBuildPipeline pipeline)
        {
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            AssetDatabase.DeleteAsset(prefabPath);
            string msg = AssetDatabase.MoveAsset(m_BakedPrefabPath, prefabPath);
            if (!string.IsNullOrEmpty(msg))
            {
                return false;
            }
            AssetDatabase.Refresh();
            return true;
        }

        public override void SyncSetting(GameObject root, string rootFolder)
        {
        }

        private Shader SelectBakeShader(BakeAnimationStageSetting setting, ModelBuildPipeline pipeline)
        {
            if (setting.BakeShader != null)
            {
                return setting.BakeShader;
            }

            if (setting.AnimationType == AnimationType.Vertex)
            {
                if (setting.RenderMode == AnimationAPI.RenderMode.BatchRendererGroup)
                {
                    return pipeline.Setting.DefaultVertexAnimationBRGBakeShader;
                }
                else if (setting.RenderMode == AnimationAPI.RenderMode.GPUInstancing)
                {
                    return pipeline.Setting.DefaultVertexAnimationGPUInstancingBakeShader;
                }
                else
                {
                    Debug.Assert(false, $"TODO: {setting.RenderMode}");
                }
            }
            else if (setting.AnimationType == AnimationType.Rig)
            {
                if (setting.RenderMode == AnimationAPI.RenderMode.BatchRendererGroup)
                {
                    return pipeline.Setting.DefaultRigAnimationBRGBakeShader;
                }
                else if (setting.RenderMode == AnimationAPI.RenderMode.GPUInstancing)
                {
                    return pipeline.Setting.DefaultRigAnimationGPUInstancingBakeShader;
                }
                else
                {
                    Debug.Assert(false, $"TODO: {setting.RenderMode}");
                }
            }
            else
            {
                Debug.Assert(false, $"TODO: {setting.AnimationType}");
            }

            return null;
        }

        private string m_BakedPrefabPath;
    }
}
