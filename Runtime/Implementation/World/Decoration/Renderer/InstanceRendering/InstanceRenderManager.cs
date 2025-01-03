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
using UnityEngine.Scripting;
using UnityEngine.Pool;
using UnityEngine;
using XDay.RenderingAPI.BRG;
using XDay.AnimationAPI;

namespace XDay.WorldAPI.Decoration
{
    [Preserve]
    internal class InstanceRenderManager
    {
        public InstanceRenderManager(Transform parent, GPUBatchInfoRegistry batchInfoRegistry, InstanceAnimatorBatchInfoRegistry prefabManager)
        {
            if (batchInfoRegistry == null)
            {
                Debug.LogError($"batch info not found!, can't use instance rendering!");
            }
            m_GPUBatchInfoRegistry = batchInfoRegistry;
            m_AnimationPrefabManager = prefabManager;
            m_AnimatorManager = IInstanceAnimatorManager.Create();

            CreateBatchManager(parent);
        }

        public void OnDestroy()
        {
            m_GPUBatchManager.OnDestroy();
            m_AnimatorManager.OnDestroy();
        }

        public void DestroyRenderer(DecorationObject decoration)
        {
            if (decoration.IsStaticObject)
            {
                if (m_Renderers.TryGetValue(decoration.ID, out var renderer))
                {
                    if (renderer.ReleaseRef())
                    {
                        var batch = m_GPUBatchManager.QueryBatch<GPUBatch>(renderer.GPUBatchID);
                        batch.RemoveInstance(renderer.InstanceIndex);
                        m_Renderers.Remove(decoration.ID);
                        m_RendererPool.Release(renderer);
                    }
                }
            }
            else
            {
                if (m_Animators.TryGetValue(decoration.ID, out var animator))
                {
                    m_AnimatorManager.DestroyInstance(animator.ID);
                    m_Animators.Remove(decoration.ID);
                }
            }
        }

        public void CreateRenderer(DecorationObject decoration)
        {
            if (decoration.IsStaticObject)
            {
                m_Renderers.TryGetValue(decoration.ID, out var renderer);
                if (renderer != null)
                {
                    renderer.AddRef();
                }
                else
                {
                    var batch = QueryBatch(decoration.GPUBatchID, decoration.LOD);
                    var instanceIndex = batch.AddInstance(decoration.Position, decoration.Rotation, decoration.Scale);

                    renderer = m_RendererPool.Get();
                    renderer.Init(instanceIndex, batch.ID);
                    m_Renderers[decoration.ID] = renderer;
                }
            }
            else
            {
                var data = m_AnimationPrefabManager.GetBatchData(decoration.GPUBatchID, decoration.LOD);
                var animator = m_AnimatorManager.CreateInstance(data, decoration.Position, decoration.Scale, decoration.Rotation);
                animator.PlayAnimation(m_DefaultAnimName);
                m_Animators.Add(decoration.ID, animator);
            }
        }

        public void Update()
        {
            m_AnimatorManager.Update();
        }

        public void PlayAnimation(int id, string name, bool alwaysPlay)
        {
            if (m_Animators.TryGetValue(id, out var animator))
            {
                animator.PlayAnimation(name, alwaysPlay);
            }
        }

        private void CreateBatchManager(Transform parent)
        {
            m_GPUBatchManager = IGPUBatchManager.Create();
#if UNITY_EDITOR
            m_Root = new GameObject("Instance Rendering Debug");
            m_Root.transform.SetParent(parent);
            var batchManagerDebugger = m_Root.AddComponent<GPUBatchManagerDebugger>();
            batchManagerDebugger.Init(m_GPUBatchManager);
#endif
        }

        private GPUStaticBatch QueryBatch(int batchID, int lod)
        {
            var batchInfo = m_GPUBatchInfoRegistry.GetBatch(batchID);
            var lodInfo = batchInfo.LODs[lod];
            var batch = m_GPUBatchManager.QueryRenderableBatch<GPUStaticBatch>(lodInfo.Mesh, lodInfo.Material);
            if (batch == null)
            {
                var createInfo = IGPUBatchCreateInfo.Create(batchInfo.InstanceCount, lodInfo.Mesh, 0, lodInfo.Material, null);
                batch = m_GPUBatchManager.CreateBatch<GPUStaticBatch>(createInfo);
            }
            return batch;
        }

        private GameObject m_Root;
        private InstanceAnimatorBatchInfoRegistry m_AnimationPrefabManager;
        private readonly GPUBatchInfoRegistry m_GPUBatchInfoRegistry;
        private readonly ObjectPool<InstanceRenderer> m_RendererPool = new ObjectPool<InstanceRenderer>(() =>
        {
            return new InstanceRenderer();
        }, defaultCapacity: 2000);
        private IGPUBatchManager m_GPUBatchManager;
        private readonly Dictionary<int, InstanceRenderer> m_Renderers = new();
        private readonly Dictionary<int, IInstanceAnimator> m_Animators = new();
        private readonly IInstanceAnimatorManager m_AnimatorManager;
        private string m_DefaultAnimName = "Idle";
    }
}

//XDay