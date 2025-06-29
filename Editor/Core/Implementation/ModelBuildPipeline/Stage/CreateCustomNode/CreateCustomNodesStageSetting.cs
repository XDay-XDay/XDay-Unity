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

using System.Collections.Generic;
using UnityEngine;

namespace XDay.ModelBuildPipeline.Editor
{
    [System.Serializable]
    public class CustomNode
    {
        public string Name;
        public GameObject Prefab;
        public string PrefabPath;
        public bool UseGlobal = true;
        public Vector3 LocalPosition;
        public Vector3 LocalScale = new(1, 1, 1);
        public Quaternion LocalRotation = new(0, 0, 0, 1);
        //要挂在哪个节点上,null或空表示挂在根节点
        public string AttachParentName = null;
    }

    [CreateAssetMenu(fileName = "CreateCustomNodesStageSetting.asset", menuName = "XDay/Model/Stage/Create Custom Nodes Stage Setting")]
    public class CreateCustomNodesStageSetting : ModelBuildPipelineStageSetting
    {
        public List<CustomNode> Nodes = new();

        public override void CopyFrom(ModelBuildPipelineStageSetting setting)
        {
            var s = setting as CreateCustomNodesStageSetting;

            Nodes.Clear();
            foreach (var otherNode in s.Nodes)
            {
                var node = new CustomNode
                {
                    Name = otherNode.Name,
                    Prefab = otherNode.Prefab,
                    PrefabPath = otherNode.PrefabPath,
                    UseGlobal = otherNode.UseGlobal
                };
                Nodes.Add(node);
            }
        }
    }
}
