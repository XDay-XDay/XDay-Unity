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
using UnityEngine;

namespace XDay.ModelBuildPipeline.Editor
{
    /// <summary>
    /// 创建音效播放
    /// </summary>
    [Serializable]
    [StageDescription("音效", "创建AudioSource组件")]
    internal class CreateAudioSource : ModelBuildPipelineStage
    {
        public override Type SettingType => typeof(CreateAudioSourceStageSetting);

        public CreateAudioSource(int id) : base(id)
        {
        }

        protected override bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            var audioSetting = GetStageSetting<CreateAudioSourceStageSetting>(rootFolder);
            var audioSource = root.AddComponent<AudioSource>();
            audioSource.spatialBlend = audioSetting.SpatialBlend;
            audioSource.playOnAwake = audioSetting.PlayOnAwake;
            audioSource.volume = audioSetting.Volume;
            audioSource.rolloffMode = audioSetting.RolloffMode;
            audioSource.minDistance = audioSetting.MinDistance;
            audioSource.maxDistance = audioSetting.MaxDistance;
            audioSource.dopplerLevel = audioSetting.DopplerLevel;
            audioSource.loop = audioSetting.Loop;
            return true;
        }

        public override void SyncSetting(GameObject root, string rootFolder)
        {
            if (!root.TryGetComponent<AudioSource>(out var audioSource))
            {
                return;
            }

            var audioSetting = GetStageSetting<CreateAudioSourceStageSetting>(rootFolder);
            audioSetting.SpatialBlend = audioSource.spatialBlend;
            audioSetting.PlayOnAwake = audioSource.playOnAwake;
            audioSetting.Volume = audioSource.volume;
            audioSetting.RolloffMode = audioSource.rolloffMode;
            audioSetting.MinDistance = audioSource.minDistance;
            audioSetting.MaxDistance = audioSource.maxDistance;
            audioSetting.DopplerLevel = audioSource.dopplerLevel;
            audioSetting.Loop = audioSource.loop;
        }
    }
}
