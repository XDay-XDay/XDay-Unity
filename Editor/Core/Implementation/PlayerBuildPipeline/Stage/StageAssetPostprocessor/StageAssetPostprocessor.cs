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

using Cysharp.Threading.Tasks;
using System;

namespace XDay.PlayerBuildPipeline.Editor
{
    /// <summary>
    /// 后处理资源
    /// </summary>
    [Serializable]
    internal class StageAssetPostprocessor : PlayerBuildPipelineStage
    {
        public AssetPipeline.Editor.AssetPipeline Pipeline;

        public StageAssetPostprocessor(int id) : base(id)
        {
        }

        protected override async UniTask<PreBuildReport> OnPreBuild(PlayerBuildPipeline pipeline)
        {
            var ret = new PreBuildReport();
            if (Pipeline == null)
            {
                ret.Success = false;
                ret.ErrorMessage = "Asset Pipeline没设置!";
            }
            return await UniTask.FromResult(ret);
        }

        protected override async UniTask<BuildReport> OnBuild(PlayerBuildPipeline pipeline)
        {
            await Pipeline.Build(null);

            return await UniTask.FromResult(new BuildReport());
        }
    }
}
