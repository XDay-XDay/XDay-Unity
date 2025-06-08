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
using XDay.UtilityAPI;

namespace XDay.ModelBuildPipeline.Editor
{
    /// <summary>
    /// 设置GameObject的Layer
    /// </summary>
    [Serializable]
    [StageDescription("Set Layer", "设置GameObject的Layer")]
    internal class SetLayer : ModelBuildPipelineStage
    {
        public override Type SettingType => typeof(SetLayerStageSetting);

        public SetLayer(int id) : base(id)
        {
        }

        protected override bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            var setting = GetStageSetting<SetLayerStageSetting>(rootFolder);

            if (setting.SetRootOnly)
            {
                root.layer = setting.Layer;
            }
            else
            {
                Helper.Traverse(root.transform, false, (transform) =>
                {
                    transform.gameObject.layer = setting.Layer;
                });
            }

            return true;
        }

        public override void SyncSetting(GameObject root, string rootFolder)
        {
            var setting = GetStageSetting<SetLayerStageSetting>(rootFolder);
            setting.Layer = root.layer;
        }
    }
}
