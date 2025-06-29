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
using XDay.UtilityAPI;

namespace XDay.ModelBuildPipeline.Editor
{
    /// <summary>
    /// 创建自定义节点
    /// </summary>
    [Serializable]
    [StageDescription("自定义节点", "创建自定义GameObject节点")]
    internal class CreateCustomNode : ModelBuildPipelineStage
    {
        public override Type SettingType => typeof(CreateCustomNodesStageSetting);

        public CreateCustomNode(int id) : base(id)
        {
        }

        protected override bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            var setting = GetStageSetting<CreateCustomNodesStageSetting>(rootFolder);

            foreach (var node in setting.Nodes)
            {
                GameObject obj = null;
                if (node.Prefab != null)
                {
                    obj = UnityEngine.Object.Instantiate(node.Prefab);
                }
                else if (!string.IsNullOrEmpty(node.PrefabPath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(node.PrefabPath);
                    if (prefab != null)
                    {
                        obj = UnityEngine.Object.Instantiate(prefab);
                    }
                }
                else if (pipeline.Setting != null)
                {
                    var prefab = pipeline.Setting.GetGameObject(node.Name);
                    if (prefab != null)
                    {
                        obj = UnityEngine.Object.Instantiate(prefab);
                    }
                }

                if (obj == null)
                {
                    obj = new GameObject();
                }
                obj.name = node.Name;

                var parent = Helper.FindChild(root.transform, node.AttachParentName);
                if (parent == null)
                {
                    parent = root.transform;
                }
                obj.transform.SetParent(parent);
                obj.transform.localPosition = node.LocalPosition;

                Vector3 scale = node.LocalScale;
                if (scale == Vector3.zero)
                {
                    scale = Vector3.one;
                }
                obj.transform.localScale = scale;

                Quaternion rot = node.LocalRotation;
                if (rot == new Quaternion(0, 0, 0, 0))
                {
                    rot = Quaternion.identity;
                }
                obj.transform.localRotation = rot;
            }
            return true;
        }

        public override void SyncSetting(GameObject root, string rootFolder)
        {
            var setting = GetStageSetting<CreateCustomNodesStageSetting>(rootFolder);

            foreach (var node in setting.Nodes)
            {
                var child = Helper.FindChild(root.transform, node.Name);
                if (child != null)
                {
                    node.LocalPosition = child.localPosition;
                    node.LocalScale = child.localScale;
                    node.LocalRotation = child.localRotation;
                }
            }
        }
    }
}
