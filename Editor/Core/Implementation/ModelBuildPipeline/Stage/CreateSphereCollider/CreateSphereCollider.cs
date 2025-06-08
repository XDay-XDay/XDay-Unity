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
    /// 给模型增加创建SphereCollider
    /// </summary>
    [Serializable]
    [StageDescription("SphereCollider", "创建SphereCollider组件")]
    [StageGroup("Collider")]
    internal class CreateSphereCollider : ModelBuildPipelineStage
    {
        public override Type SettingType => typeof(CreateSphereColliderStageSetting);

        public CreateSphereCollider(int id) : base(id)
        {
        }

        protected override bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            var setting = GetStageSetting<CreateSphereColliderStageSetting>(rootFolder);
            var collider = root.AddComponent<UnityEngine.SphereCollider>();
            collider.radius = setting.Radius;
            collider.center = setting.Center;
            collider.isTrigger = setting.IsTrigger;

            return true;
        }

        public override void SyncSetting(GameObject root, string rootFolder)
        {
            if (!root.TryGetComponent<UnityEngine.SphereCollider>(out var collider))
            {
                return;
            }

            var setting = GetStageSetting<CreateSphereColliderStageSetting>(rootFolder);
            setting.Radius = collider.radius;
            setting.Center = collider.center;
            setting.IsTrigger = collider.isTrigger;
        }
    }
}
