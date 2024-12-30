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
using UnityEngine.Pool;
using XDay.RenderingAPI.BRG;

namespace XDay.AnimationAPI
{
    internal abstract class InstanceAnimator : IInstanceAnimator
    {
        public Vector3 WorldScale => m_LocalScale;
        public Vector3 WorldPosition
        {
            get => m_Set == null ? m_LocalPos : m_Set.Transform(m_LocalPos);
            set
            {
                if (m_Set == null)
                {
                    m_LocalPos = value;
                }
                else
                {
                    m_LocalPos = m_Set.InverseTransform(value);
                }
                m_Dirty = true;
            }
        }
        public Quaternion WorldRotation
        {
            get => m_Set == null ? m_LocalRot : m_Set.Transform(m_LocalRot);
            set
            {
                if (m_Set == null)
                {
                    m_LocalRot = value;
                }
                else
                {
                    m_LocalRot = m_Set.InverseTransform(value);
                }
                m_Dirty = true;
            }
        }
        public Vector3 LocalScale
        {
            get => m_LocalScale;
            set
            {
                if (value != m_LocalScale)
                {
                    m_LocalScale = value;
                    m_Dirty = true;
                }
            }
        }
        public Vector3 LocalPosition
        {
            get => m_LocalPos;
            set
            {
                if (value != m_LocalPos)
                {
                    m_LocalPos = value;
                    m_Dirty = true;
                }
            }
        }
        public Quaternion LocalRotation
        {
            get => m_LocalRot;
            set
            {
                if (value != m_LocalRot)
                {
                    m_LocalRot = value;
                    m_Dirty = true;
                }
            }
        }
        public IInstanceAnimatorSet Set 
        {
            set 
            {
                if (m_Set != value)
                {
                    m_Set?.RemoveAnimator(this);
                    m_Set = value;
                    m_Set?.AddAnimator(this);
                    m_AnimatorManager.Dirty();
                }
            } 
        }
        public int ID => m_ID;

        public void Init(int id, 
            AnimatedInstanceMeshDataManager[] meshesDataManager, 
            InstanceAnimatorManager animatorManager, 
            AnimationBakeMetadata metadata, 
            IGPUBatchManager batchManager, 
            Vector3 localPos, 
            Quaternion localRot,
            Vector3 localScale)
        {
            m_ID = id;
            m_Dirty = true;
            m_GPUBatchManager = batchManager;
            m_MeshesDataManager = meshesDataManager;
            m_AnimatorManager = animatorManager;
            m_BakeMetadata = metadata;
            LocalPosition = localPos;
            LocalRotation = localRot;
            LocalScale = localScale;
        }

        public void Uninit()
        {
            if (m_ID != 0)
            {
                m_ID = 0;
                m_AnimatorManager = null;
                m_BakeMetadata = null;
                m_GPUBatchManager = null;

                foreach (var meshDataManager in m_MeshesDataManager)
                {
                    foreach (var meshData in meshDataManager.Meshes)
                    {
                        meshData.Uninit();
                    }
                }
                m_MeshesDataManager = null;
            }
        }

        public void Dirty()
        {
            m_Dirty = true;
        }

        public void PlayAnimation(string name, bool alwaysPlay = false)
        {
            if (m_CurAnim == name && 
                !alwaysPlay)
            {
                return;
            }

            var metadata = m_BakeMetadata.QueryMetadata(name);
            if (metadata != null)
            {
                m_CurAnim = name;
                foreach (var meshDataManager in m_MeshesDataManager)
                {
                    for (var i = 0; i < meshDataManager.Meshes.Count; i++)
                    {
                        CommitAnimationData(i, meshDataManager.Meshes[i], metadata);
                    }
                }
                m_AnimatorManager.Dirty();
            }
            else
            {
                Debug.LogError($"no anim: {name}");
            }
        }

        public void Update()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                foreach (var renderData in m_MeshesDataManager)
                {
                    foreach (var meshData in renderData.Meshes)
                    {
                        meshData.Batch.UpdateTransform(meshData.InstanceIndex, WorldPosition, WorldRotation, m_LocalScale);
                    }
                }
                m_AnimatorManager.Dirty();
            }
        }

        protected abstract void CommitAnimationData(int meshIndex, AnimatedInstanceMeshData meshData, AnimationMetadata metadata);

        private int m_ID;
        private string m_CurAnim;
        private Vector3 m_LocalPos;
        private Vector3 m_LocalScale = Vector3.one;
        private Quaternion m_LocalRot = Quaternion.identity;
        private bool m_Dirty = true;
        private IInstanceAnimatorSet m_Set;
        private InstanceAnimatorManager m_AnimatorManager;
        private AnimatedInstanceMeshDataManager[] m_MeshesDataManager;
        protected AnimationBakeMetadata m_BakeMetadata;
        protected IGPUBatchManager m_GPUBatchManager;
    }

    internal class AnimatedInstanceMeshData
    {
        public GPUDynamicBatch Batch => m_Batch;
        public Mesh Mesh => m_Mesh;
        public int InstanceIndex => m_InstanceIndex;

        public void Init(Mesh mesh, int instanceIndex, GPUDynamicBatch batch, ObjectPool<AnimatedInstanceMeshData> pool)
        {
            m_Mesh = mesh;
            m_InstanceIndex = instanceIndex;
            m_Batch = batch;
            m_Pool = pool;
        }

        public void Uninit()
        {
            m_Mesh = null;
            m_Batch.RemoveInstance(m_InstanceIndex);
            m_InstanceIndex = -1;
            m_Batch = null;
            m_Pool.Release(this);
            m_Pool = null;
        }

        private int m_InstanceIndex;
        private Mesh m_Mesh;
        private GPUDynamicBatch m_Batch;
        private ObjectPool<AnimatedInstanceMeshData> m_Pool;
    }

    internal class AnimatedInstanceMeshDataManager
    {
        public List<AnimatedInstanceMeshData> Meshes => m_MeshesData;

        public AnimatedInstanceMeshDataManager(List<AnimatedInstanceMeshData> meshesData)
        {
            m_MeshesData = meshesData;
        }

        private List<AnimatedInstanceMeshData> m_MeshesData;
    }
}


//XDay