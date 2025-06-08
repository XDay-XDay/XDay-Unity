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
using XDay.AnimationAPI;

namespace XDay.ModelBuildPipeline.Editor
{
    [CreateAssetMenu(fileName = "BakeAnimationStageSetting.asset", menuName = "XDay/Model/Stage/Bake Animation Stage Setting")]
    public class BakeAnimationStageSetting : ModelBuildPipelineStageSetting
    {
        public Shader BakeShader;
        public AnimationType AnimationType = AnimationType.Vertex;
        public AnimationAPI.RenderMode RenderMode = AnimationAPI.RenderMode.GPUInstancing;
        public int RenderQueue = 0;
        public bool DeleteOutputFolder = true;
        public bool DeleteRigs = true;
        public float SampleFrameInterval = 1;

        public override void CopyFrom(ModelBuildPipelineStageSetting setting)
        {
            var s = setting as BakeAnimationStageSetting;

            BakeShader = s.BakeShader;
            AnimationType = s.AnimationType;
            RenderQueue = s.RenderQueue;
            DeleteOutputFolder = s.DeleteOutputFolder;
            RenderMode = s.RenderMode;
            SampleFrameInterval = s.SampleFrameInterval;
            DeleteRigs= s.DeleteRigs;
        }
    }
}
