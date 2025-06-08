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
    [Serializable]
    [StageDescription("Add MonoBehaviour", "添加脚本")]
    internal class AddMonoBehaviour : ModelBuildPipelineStage
    {
        public override Type SettingType => typeof(AddMonoBehaviourStageSetting);

        public AddMonoBehaviour(int id) : base(id)
        {
        }

        protected override bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            bool success = true;
            var setting = GetStageSetting<AddMonoBehaviourStageSetting>(rootFolder);

            foreach (var behaviour in setting.Behaviours)
            {
                var type = Helper.SearchTypeByName(behaviour.ComponentTypeName);
                if (type == null)
                {
                    Fail($"{behaviour.ComponentTypeName}找不到");
                    success = false;
                }
                else
                {
                    if (root.GetComponent(type) == null)
                    {
                        root.AddComponent(type);
                    }
                }
            }
            return success;
        }

        public override void SyncSetting(GameObject root, string rootFolder)
        {
        }
    }
}
