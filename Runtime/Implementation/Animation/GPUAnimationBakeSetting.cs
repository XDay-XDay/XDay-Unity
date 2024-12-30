/*
 * Copyright (c) 2024 XDay
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
using System.Collections.Generic;
using UnityEngine;

namespace XDay.AnimationAPI
{
    public enum RenderMode
    {
        BatchRendererGroup,
        GPUInstancing,
    }

    public class GPUAnimationBakeSetting : MonoBehaviour
    {
        public BakeSetting Setting;
    }

    [Serializable]
    public class AdvancedBakeSetting
    {
        public AnimationClip DefaultAnimation;
        public string OutputFolderName = "../Baked/";
        public int RenderQueue = 0;
        public bool DeleteOutputFolder = true;
        public bool DeleteRigs = true;
        public float SampleFrameInterval = 1;
        [HideInInspector]
        public InstanceAnimatorData InstanceAnimatorData;
    }

    [Serializable]
    public class BakeSetting
    {
        [HideInInspector]
        public GameObject Prefab;
        public Shader Shader;
        public AnimationType AnimationType = AnimationType.Vertex;
        public RenderMode RenderMode = RenderMode.GPUInstancing;
        public List<AnimationClip> Animations = new();
        public AdvancedBakeSetting AdvancedSetting = new();
        public string OutputFolder => $"{m_OutputFolder}/{AdvancedSetting.OutputFolderName}";

        public AnimationClip GetDefaultAnimation()
        {
            if (AdvancedSetting.DefaultAnimation != null)
            {
                return AdvancedSetting.DefaultAnimation;
            }

            if (Animations.Count > 0)
            {
                return Animations[0];
            }

            return null;
        }

        public string BatchRendererGroupDataPath()
        {
            return ToPath("brg.asset");
        }

        public string AnimationMetadataPath()
        {
            return ToPath("meta.asset");
        }

        public string PrefabPath()
        {
            return ToPath("prefab.prefab");
        }

        public string MeshPath(int id)
        {
            return ToPath($"mesh_{id}.asset");
        }

        public string MaterialPath(int id = 0)
        {
            return ToPath($"material_{id}.mat");
        }

        public string TexturePath(int id)
        {
            return ToPath($"texture_{id}.asset");
        }

        public void SetOutputFolder(string folder)
        {
            m_OutputFolder = folder;
        }

        private string ToPath(string postfix)
        {
            return $"{OutputFolder}/{Prefab.name.ToLower()}_bake_{postfix}";
        }

        private string m_OutputFolder;
    }    
}

//XDay