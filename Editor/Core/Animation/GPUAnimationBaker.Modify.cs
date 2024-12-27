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



using System.Collections.Generic;
using UnityEngine;

namespace XDay.AnimationAPI.Editor
{
    internal partial class GPUAnimationBaker
    {
        private void Modify(BakeSetting setting, GameObject baked)
        {
            Object.DestroyImmediate(baked.GetComponentInChildren<GPUAnimationBakeSetting>());

            foreach (var animator in baked.GetComponentsInChildren<Animator>(true))
            {
                Object.DestroyImmediate(animator);
            }

            if (setting.AdvancedSetting.DeleteRigs)
            {
                DeleteRigs(setting.Prefab, baked);
            }
        }

        private void DeleteRigs(GameObject originalPrefab, GameObject bakedPrefab)
        {
            var checker = new Rigs(originalPrefab);
            foreach (var rig in checker.RootRigs)
            {
                var transform = bakedPrefab.QueryChild(rig.name);
                if (transform != null)
                {
                    Object.DestroyImmediate(transform.gameObject);
                }
            }
        }
    }

    internal class Rigs
    {
        public List<Transform> RootRigs => m_RootRigs;

        public Rigs(GameObject gameObject)
        {
            var renderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var bone in renderer.bones)
                {
                    if (!m_Rigs.Contains(bone))
                    {
                        m_Rigs.Add(bone);
                    }
                }
            }

            foreach (var renderer in renderers)
            {
                foreach (var bone in renderer.bones)
                {
                    AddRoot(bone);
                }
            }
        }

        private void AddRoot(Transform bone)
        {
            while (bone.parent != null)
            {
                if (!m_Rigs.Contains(bone.parent))
                {
                    if (!m_RootRigs.Contains(bone))
                    {
                        m_RootRigs.Add(bone);
                    }
                    break;
                }
                bone = bone.parent;
            }
        }

        private List<Transform> m_RootRigs = new();
        private List<Transform> m_Rigs = new();
    }
}

//XDay