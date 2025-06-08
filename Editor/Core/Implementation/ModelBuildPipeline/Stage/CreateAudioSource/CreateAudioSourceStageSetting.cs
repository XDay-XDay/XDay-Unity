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

using UnityEngine;

namespace XDay.ModelBuildPipeline.Editor
{
    [CreateAssetMenu(fileName = "CreateAudioSourceSetting.asset", menuName = "XDay/Model/Stage/Create Audio Source Stage Setting")]
    public class CreateAudioSourceStageSetting : ModelBuildPipelineStageSetting
    {
        public float SpatialBlend = 1.0f;
        public bool PlayOnAwake = false;
        public float Volume = 1.0f;
        public AudioRolloffMode RolloffMode = AudioRolloffMode.Linear;
        public float MinDistance = 10;
        public float MaxDistance = 30;
        public float DopplerLevel = 0;
        public bool Loop = false;

        public override void CopyFrom(ModelBuildPipelineStageSetting setting)
        {
            var s = setting as CreateAudioSourceStageSetting;

            SpatialBlend = s.SpatialBlend;
            Volume = s.Volume;
            PlayOnAwake = s.PlayOnAwake;
            RolloffMode = s.RolloffMode;
            MinDistance = s.MinDistance;
            MaxDistance = s.MaxDistance;
            DopplerLevel = s.DopplerLevel;
            Loop = s.Loop;
        }
    }
}
