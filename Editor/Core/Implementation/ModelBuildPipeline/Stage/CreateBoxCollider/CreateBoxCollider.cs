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
    /// 给模型增加创建BoxCollider
    /// </summary>
    [Serializable]
    [StageDescription("BoxCollider", "创建BoxCollider组件")]
    [StageGroup("Collider")]
    internal class CreateBoxCollider : ModelBuildPipelineStage
    {
        public override Type SettingType => typeof(CreateBoxColliderStageSetting);

        public CreateBoxCollider(int id) : base(id)
        {
        }

        protected override bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            var setting = GetStageSetting<CreateBoxColliderStageSetting>(rootFolder);
            var boxCollider = root.AddComponent<UnityEngine.BoxCollider>();
            boxCollider.size = setting.Size;
            boxCollider.center = setting.Center;
            boxCollider.isTrigger = setting.IsTrigger;
            return true;
        }

        public override void SyncSetting(GameObject root, string rootFolder)
        {
            if (!root.TryGetComponent<UnityEngine.BoxCollider>(out var boxCollider))
            {
                return;
            }

            var setting = GetStageSetting<CreateBoxColliderStageSetting>(rootFolder);
            setting.Size = boxCollider.size;
            setting.Center = boxCollider.center;
            setting.IsTrigger = boxCollider.isTrigger;
        }
    }
}
