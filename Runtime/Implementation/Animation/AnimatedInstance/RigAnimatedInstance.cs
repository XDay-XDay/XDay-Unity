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

using XDay.RenderingAPI.BRG;

namespace XDay.AnimationAPI
{
    internal class RigAnimatedInstance : InstanceAnimator
    {
        protected override void CommitAnimationData(int partIndex, AnimatedInstanceMeshData meshData, AnimationMetadata metadata)
        {
            var dynamicBatch = m_GPUBatchManager.QueryBatch<GPUDynamicBatch>(meshData.Batch.ID);

            metadata.CreateRigShaderInfo(partIndex, out var playState, out var frameData);

            dynamicBatch.SetVector(frameData, meshData.InstanceIndex, AnimationDefine.ANIM_FRAME_DATA_PROPERTY_INDEX);
            dynamicBatch.SetVector(playState, meshData.InstanceIndex, AnimationDefine.ANIM_PLAY_STATE_PROPERTY_INDEX);
        }
    }
}

//XDay