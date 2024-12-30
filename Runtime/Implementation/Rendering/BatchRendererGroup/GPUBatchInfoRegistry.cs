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
using UnityEngine.Scripting;
using XDay.AnimationAPI;

namespace XDay.RenderingAPI.BRG
{
    [Preserve]
    public class GPUBatchInfoRegistry : ScriptableObject
    {
        public void InstanceCountInc(int batchIndex)
        {
            m_Batches[batchIndex].InstanceCountInc();
        }

        public GPUBatchInfo GetBatch(int index)
        {
            return m_Batches[index];
        }

        public int CreateBatchInfo()
        {
            m_Batches.Add(new GPUBatchInfo());
            return m_Batches.Count - 1;
        }

        public bool CheckInstanceRendering(GameObject prefab, out bool isAnimator)
        {
            isAnimator = false;
            var skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderers.Length > 0)
            {
                isAnimator = true;
                return CheckSkinnedMeshInstanceRendering(prefab);
            }

            var meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length != 1)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because mesh filter count is {meshFilters.Length}");
                return false;
            }

            var filter = meshFilters[0];

            if (filter.sharedMesh == null)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because no mesh");
                return false;
            }

            if (filter.sharedMesh.subMeshCount > 1)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because {filter.sharedMesh.name} has more than 1 submesh");
                return false;
            }

            var meshRenderers = prefab.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderers.Length != 1)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because mesh renderer count is {meshRenderers.Length}");
                return false;
            }

            var renderer = meshRenderers[0];

            if (renderer.sharedMaterial == null)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because has no material");
                return false;
            }

            if (renderer.sharedMaterials.Length > 1)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because {renderer.name} has {renderer.sharedMaterials.Length} materials");
                return false;
            }

            if (renderer.gameObject != filter.gameObject)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because mesh filter and mesh renderer belongs to different game object");
                return false;
            }

            return true;
        }

        private bool CheckSkinnedMeshInstanceRendering(GameObject prefab)
        {
            var meshRenderers = prefab.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderers.Length > 0)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because has both mesh renderer and skinned mesh renderer");
                return false;
            }

            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length > 1)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because has more than 1 SkinnedMeshRenderers");
                return false;
            }

            var renderer = renderers[0];

            if (renderer.sharedMesh == null)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because no mesh");
                return false;
            }

            if (renderer.sharedMesh.subMeshCount > 1)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because {renderer.sharedMesh.name} has more than 1 submesh");
                return false;
            }

            if (renderer.sharedMaterial == null)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because has no material");
                return false;
            }

            if (renderer.sharedMaterials.Length > 1)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because {renderer.name} has {renderer.sharedMaterials.Length} materials");
                return false;
            }

            var bakeSetting = prefab.GetComponent<GPUAnimationBakeSetting>();
            if (bakeSetting == null)
            {
                Debug.LogError($"{prefab.name} can't use instance rendering because has no GPUAnimationBakeSetting component");
                return false;
            }

            return true;
        }

        [SerializeField]
        private List<GPUBatchInfo> m_Batches = new();
    }

    [Serializable]
    public class GPUBatchLODInfo
    {
        public GPUBatchLODInfo(Mesh mesh, Material material)
        {
            Mesh = mesh;
            Material = material;
        }

        public Mesh Mesh;
        public Material Material;
    }

    [Serializable]
    public class GPUBatchInfo
    {
        public int InstanceCount => m_InstanceCount;
        public List<GPUBatchLODInfo> LODs => m_LODs;

        public GPUBatchInfo()
        {
            m_InstanceCount = 1;
        }

        public void AddLOD(Mesh mesh, Material material)
        {
            m_LODs.Add(new GPUBatchLODInfo(mesh, material));
        }

        public void InstanceCountInc()
        {
            ++m_InstanceCount;
        }

        [SerializeField]
        private int m_InstanceCount;
        [SerializeField]
        private List<GPUBatchLODInfo> m_LODs = new();

    }
}


//XDay