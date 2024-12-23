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

using UnityEngine.Rendering;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

namespace XDay.RenderingAPI.BRG
{
    public abstract class GPUBatch
    {
        public int ID => m_ID;
        public int SubMeshIndex => m_CreateInfo.SubMeshIndex;
        public int InstanceCount => m_UsedInstanceCount;
        public bool IsFull => InstanceCount >= m_MaxInstanceCount;
        public int MaxInstanceCount => m_MaxInstanceCount;
        public int MaxBufferSize => m_MaxBufferSize;
        public BatchID HandleID => m_HandleID;
        public BatchMaterialID MaterialHandleID => m_MaterialHandleID;
        public BatchMeshID MeshHandleID => m_MeshHandleID;
        public int[] UsedInstances => m_UsedInstanceIndices;
        public Mesh Mesh => m_Mesh;
        public Material Material => m_Material;

        internal void Init(int id, BatchRendererGroup groupHandle, IGPUBatchCreateInfo createInfo)
        {
            m_ID = id;
            m_GroupHandle = groupHandle;
            m_CreateInfo = createInfo;
            m_InstanceSize = createInfo.InstanceSize;
            m_Mesh = createInfo.Mesh;
            m_MeshHandleID = m_GroupHandle.RegisterMesh(m_Mesh);
            m_Material = createInfo.Material;
            m_MaterialHandleID = m_GroupHandle.RegisterMaterial(m_Material);
            m_DataStartOffsets = new int[createInfo.PropertyCount];

            var batchMetadata = new NativeArray<MetadataValue>(m_CreateInfo.PropertyCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            int stride = 4;
            GraphicsBuffer.Target target = GraphicsBuffer.Target.Raw;
            m_MaxInstanceCount = m_CreateInfo.MaxInstanceCount;
            m_FreeInstanceIndices = new List<int>(m_MaxInstanceCount);
            for (var i = 0; i < m_MaxInstanceCount; ++i)
            {
                m_FreeInstanceIndices.Add(i);
            }

            bool useConstantBuffer = BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;

            if (useConstantBuffer)
            {
                m_MaxInstanceCount = BatchRendererGroup.GetConstantBufferMaxWindowSize() / m_InstanceSize;
                stride = 16;
                target = GraphicsBuffer.Target.Constant;
            }

            var size = 0;
            for (var i = 0; i < m_CreateInfo.PropertyCount; ++i)
            {
                m_DataStartOffsets[i] = size;
                var property = m_CreateInfo.GetProperty(i);
                batchMetadata[i] = CreateMetadataValue(Shader.PropertyToID(property.Name), size, property.IsPerInstance);
                size += property.DataSize * m_MaxInstanceCount;
            }

            m_MaxBufferSize = m_MaxInstanceCount * m_InstanceSize;
            m_GPUBuffer = new GraphicsBuffer(target, count: m_MaxBufferSize / stride, stride);
            m_HandleID = m_GroupHandle.AddBatch(batchMetadata, m_GPUBuffer.bufferHandle, 0, useConstantBuffer ? (uint)BatchRendererGroup.GetConstantBufferMaxWindowSize() : 0);
            m_UsedInstanceIndices = new int[m_MaxInstanceCount];

            OnInit();
        }

        public void OnDestroy()
        {
            OnUninit();

            m_Material = null;
            m_Mesh = null;

            m_GPUBuffer.Dispose();
            m_GPUBuffer = null;
        }

        public void RemoveInstance(int instanceIndex)
        {
            for (var idx = 0; idx < m_UsedInstanceCount; ++idx)
            {
                if (m_UsedInstanceIndices[idx] == instanceIndex)
                {
                    m_FreeInstanceIndices.Add(instanceIndex);
                    m_UsedInstanceIndices[idx] = m_UsedInstanceIndices[m_UsedInstanceCount - 1];
                    --m_UsedInstanceCount;
                    SetDirty();
                    return;
                }
            }
            
            Debug.LogError($"Remove instance {instanceIndex} failed!");
        }

        public virtual void SetData<T>(NativeArray<T> data, int instanceIndex, int propertyIndex) where T : struct 
        {
            if (instanceIndex >= 0 && instanceIndex < m_MaxInstanceCount)
            {
                var dataOffset = QueryPropertyDataOffset(instanceIndex, propertyIndex, out var dataSize);
                m_GPUBuffer.SetData(data, 0, dataOffset / dataSize, 1);
            }
        }

        public void GetData<T>(T[] data, int instanceIndex, int propertyIndex) where T : struct
        {
            if (instanceIndex >= 0 && instanceIndex < m_MaxInstanceCount)
            {
                var dataOffset = QueryPropertyDataOffset(instanceIndex, propertyIndex, out var dataSize);
                m_GPUBuffer.GetData(data, 0, dataOffset / dataSize, 1);
            }
        }

        public int QueryPropertyDataOffset(int instanceIndex, int propertyIndex, out int dataSize)
        {
            dataSize = m_CreateInfo.GetProperty(propertyIndex).DataSize;
            return m_DataStartOffsets[propertyIndex] + instanceIndex * dataSize;
        }

        public bool IsRenderable(Mesh mesh, Material material)
        {
            return !IsFull &&
                ReferenceEquals(mesh, m_Mesh) &&
                ReferenceEquals(material, m_Material);
        }

        protected int NextIndex()
        {
            if (m_FreeInstanceIndices.Count > 0)
            {
                var index = m_FreeInstanceIndices[^1];
                m_FreeInstanceIndices.RemoveAt(m_FreeInstanceIndices.Count - 1);

                m_UsedInstanceIndices[m_UsedInstanceCount] = index;
                ++m_UsedInstanceCount;
                return index;
            }

            Debug.Assert(false, $"Instance count is more than {m_MaxInstanceCount}!");
            return -1;
        }

        public virtual void SetDirty()
        {
        }

        public virtual void Sync()
        {
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnUninit()
        {
        }

        private MetadataValue CreateMetadataValue(int nameID, int dataOffset, bool isPerInstance)
        {
            return new MetadataValue
            {
                NameID = nameID,
                Value = (uint)dataOffset | (isPerInstance ? 0x80000000 : 0),
            };
        }

        protected GraphicsBuffer GPUBuffer => m_GPUBuffer;

        private int m_ID;
        private int[] m_UsedInstanceIndices;
        private int m_UsedInstanceCount = 0;
        private IGPUBatchCreateInfo m_CreateInfo;
        private BatchRendererGroup m_GroupHandle;
        private List<int> m_FreeInstanceIndices;
        private BatchID m_HandleID;
        private BatchMeshID m_MeshHandleID;
        private GraphicsBuffer m_GPUBuffer;
        private BatchMaterialID m_MaterialHandleID;
        private int m_MaxInstanceCount;
        private int m_MaxBufferSize;
        private Mesh m_Mesh;
        private int[] m_DataStartOffsets;
        private int m_InstanceSize;
        private Material m_Material;
    }
}

//XDay