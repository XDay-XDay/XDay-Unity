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
using UnityEngine.Pool;
using XDay.RenderingAPI.BRG;

namespace XDay.AnimationAPI
{
    internal class InstanceAnimatorManager : IInstanceAnimatorManager
    {
        public InstanceAnimatorManager(Func<string, InstanceAnimatorData> loader)
        {
            m_MeshDataPool = new ObjectPool<AnimatedInstanceMeshData>(createFunc: () => { 
                return new AnimatedInstanceMeshData(); 
            });
            m_RigInstancePool = new ObjectPool<RigAnimatedInstance>(
                createFunc: () => { return new RigAnimatedInstance(); }
                );
            m_VertexInstancePool = new ObjectPool<VertexAnimatedInstance>(
                createFunc: () => { return new VertexAnimatedInstance(); }
                );

            m_InstanceAnimatorDataLoader = loader;
            m_GPUBatchManager = IGPUBatchManager.Create();
        }

        public void Dirty()
        {
            m_Dirty = true;
        }

        public IInstanceAnimator CreateInstance(string path, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            var data = QueryInstanceAnimatorData(path);
            if (data == null)
            {
                return null;
            }
            return CreateInstance(data, pos, scale, rot);
        }

        public IInstanceAnimator CreateInstance(string brgDataPath)
        {
            var data = QueryInstanceAnimatorData(brgDataPath);
            return CreateInstance(data, Vector3.zero, Vector3.one, Quaternion.identity);
        }

        public void DestroyInstance(int id)
        {
            if (QueryInstance(id) is InstanceAnimator instance)
            {
                if (instance is VertexAnimatedInstance)
                {
                    m_VertexInstancePool.Release(instance as VertexAnimatedInstance);
                }
                else
                {
                    m_RigInstancePool.Release(instance as RigAnimatedInstance);
                }

                instance.Uninit();
            }
            m_Instances.Remove(id);
        }

        public void OnDestroy()
        {
            m_Instances.Clear();
            m_GPUBatchManager.OnDestroy();
            m_MeshDataPool.Clear();
        }

        public IInstanceAnimator QueryInstance(int id)
        {
            m_Instances.TryGetValue(id, out var instance);
            return instance;
        }

        public void Update()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                foreach (var instance in m_Instances)
                {
                    instance.Value.Update();
                }

                m_GPUBatchManager.Sync();
            }
        }

        public IInstanceAnimator CreateInstance(InstanceAnimatorData animatorData, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            Dirty();

            var meshesDataManager = new AnimatedInstanceMeshDataManager[animatorData.Meshes.Length];
            for (var i = 0; i < meshesDataManager.Length; i++)
            {
                List<AnimatedInstanceMeshData> meshes = CreateSubMeshesData(i, animatorData);
                meshesDataManager[i] = new AnimatedInstanceMeshDataManager(meshes);
            }

            InstanceAnimator instance = animatorData.AnimType == AnimationType.Rig ? m_RigInstancePool.Get() : m_VertexInstancePool.Get();
            instance.Init(++m_NextInstanceID, meshesDataManager, this, animatorData.Metadata, m_GPUBatchManager, pos, rot, scale);
            m_Instances.Add(instance.ID, instance);

            return instance;
        }

        private InstanceAnimatorData QueryInstanceAnimatorData(string path)
        {
            m_InstanceAnimatorData.TryGetValue(path, out var data);
            if (data == null)
            {
                data = m_InstanceAnimatorDataLoader.Invoke(path);
                if (data == null)
                {
                    Debug.LogError($"load prefab {path} failed!");
                }
                else
                {
                    m_InstanceAnimatorData[path] = data;
                }
            }
            return data;
        }

        private GPUDynamicBatch CreateBatch(Mesh mesh, Material material, ushort submeshIndex)
        {
            var properties = new IShaderPropertyDeclaration[2]
            {
                IShaderPropertyDeclaration.Create(ShaderPropertyType.Vector, AnimationDefine.ANIM_FRAME_NAME),
                IShaderPropertyDeclaration.Create(ShaderPropertyType.Vector, AnimationDefine.ANIM_PLAY_NAME),
            };

            var batchCreateInfo = IGPUBatchCreateInfo.Create(m_MaxInstanceCountInOneBatch, mesh, submeshIndex, material, properties);
            return m_GPUBatchManager.CreateBatch<GPUDynamicBatch>(batchCreateInfo);
        }

        private List<AnimatedInstanceMeshData> CreateSubMeshesData(
            int index, 
            InstanceAnimatorData animatorData)
        {
            List<AnimatedInstanceMeshData> meshes = new();
            var mesh = animatorData.Meshes[index];
            ushort submesh = 0;
            //create data foreach submesh
            foreach (var material in animatorData.Materials[index].Materials)
            {
                var batch = m_GPUBatchManager.QueryRenderableBatch<GPUDynamicBatch>(mesh, material);
                batch ??= CreateBatch(mesh, material, submesh);
                var instanceIndex = batch.AddInstance();

                var meshData = m_MeshDataPool.Get();
                meshData.Init(mesh, instanceIndex, batch, m_MeshDataPool);
                meshes.Add(meshData);
                ++submesh;
            }
            return meshes;
        }

        private Dictionary<int, InstanceAnimator> m_Instances = new();
        private int m_MaxInstanceCountInOneBatch = 1000;
        private Func<string, InstanceAnimatorData> m_InstanceAnimatorDataLoader;
        private ObjectPool<RigAnimatedInstance> m_RigInstancePool;
        private ObjectPool<VertexAnimatedInstance> m_VertexInstancePool;
        private IGPUBatchManager m_GPUBatchManager;
        private Dictionary<string, InstanceAnimatorData> m_InstanceAnimatorData = new();
        private int m_NextInstanceID = 0;
        private bool m_Dirty = true;
        private ObjectPool<AnimatedInstanceMeshData> m_MeshDataPool;
    }
}


//XDay