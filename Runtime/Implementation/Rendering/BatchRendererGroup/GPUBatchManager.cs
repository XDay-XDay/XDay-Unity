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
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace XDay.RenderingAPI.BRG
{
    internal class GPUBatchManager : IGPUBatchManager
    {
        public int BatchCount => m_Batches.Count;

        public GPUBatchManager()
        {
            BatchRendererGroupCreateInfo createInfo = new()
            {
                cullingCallback = OnPerformCulling,
                userContext = IntPtr.Zero,
            };
            m_Handle = new BatchRendererGroup(createInfo);
        }

        public void OnDestroy()
        {
            m_Handle.Dispose();

            foreach (var batch in m_Batches)
            {
                batch.OnDestroy();
            }
            m_Batches.Clear();
        }

        public unsafe JobHandle OnPerformCulling(BatchRendererGroup group, BatchCullingContext context, BatchCullingOutput output, IntPtr userContext)
        {
            var drawCommands = (BatchCullingOutputDrawCommands*)output.drawCommands.GetUnsafePtr();
            var drawCommandCount = m_Batches.Count;
            drawCommands->drawCommands = Malloc<BatchDrawCommand>(drawCommandCount);
            drawCommands->drawRanges = Malloc<BatchDrawRange>(1);
            var totalInstanceCount = 0;
            for (var i = 0; i < drawCommandCount; ++i)
            {
                totalInstanceCount += m_Batches[i].InstanceCount;
            }
            drawCommands->visibleInstanceCount = totalInstanceCount;
            drawCommands->visibleInstances = Malloc<int>(drawCommands->visibleInstanceCount);

            drawCommands->drawCommandCount = drawCommandCount;
            drawCommands->drawRangeCount = 1;
            drawCommands->instanceSortingPositions = null;
            drawCommands->instanceSortingPositionFloatCount = 0;
            drawCommands->drawCommandPickingInstanceIDs = null;

            drawCommands->drawRanges[0].filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff };
            drawCommands->drawRanges[0].drawCommandsBegin = 0;
            drawCommands->drawRanges[0].drawCommandsCount = (uint)drawCommandCount;
            drawCommands->drawRanges[0].drawCommandsType = BatchDrawCommandType.Direct;

            var count = 0;
            for (var i = 0; i < drawCommandCount; ++i)
            {
                var batch = m_Batches[i];
                var instanceCount = batch.InstanceCount;
                drawCommands->drawCommands[i].flags = 0;
                drawCommands->drawCommands[i].batchID = batch.HandleID;
                drawCommands->drawCommands[i].visibleOffset = (uint)count;
                drawCommands->drawCommands[i].splitVisibilityMask = 0xff;
                drawCommands->drawCommands[i].materialID = batch.MaterialHandleID;
                drawCommands->drawCommands[i].visibleCount = (uint)instanceCount;
                drawCommands->drawCommands[i].sortingPosition = 0;
                drawCommands->drawCommands[i].submeshIndex = (ushort)batch.SubMeshIndex;
                drawCommands->drawCommands[i].meshID = batch.MeshHandleID;

                fixed (int* p = &batch.UsedInstances[0])
                {
                    UnsafeUtility.MemCpy(drawCommands->visibleInstances + count, p, sizeof(int) * instanceCount);
                }

                count += instanceCount;
            }

            return new JobHandle();
        }

        public T QueryBatch<T>(int batchID) where T : GPUBatch
        {
            foreach (var batch in m_Batches)
            {
                if (batch.ID == batchID)
                {
                    return batch as T;
                }
            }

            Debug.LogError($"Batch {batchID} not found!");
            return null;
        }

        public GPUBatch GetBatch(int index)
        {
            if (index >= 0 && index < m_Batches.Count)
            {
                return m_Batches[index];
            }

            return null;
        }

        public T QueryRenderableBatch<T>(Mesh mesh, Material material) where T : GPUBatch
        {
            foreach (var batch in m_Batches)
            {
                if (batch.IsRenderable(mesh, material))
                {
                    return batch as T;
                }
            }

            return null;
        }

        public T CreateBatch<T>(IGPUBatchCreateInfo createInfo) where T : GPUBatch, new()
        {
            var batch = new T();
            batch.Init(++m_NextID, m_Handle, createInfo);
            m_Batches.Add(batch);
            return batch;
        }

        public void Sync()
        {
            foreach (var batch in m_Batches)
            {
                batch.Sync();
            }
        }

        private static unsafe T* Malloc<T>(int count) where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<T>() * count,
                UnsafeUtility.AlignOf<T>(),
                Allocator.TempJob);
        }

        private int m_NextID;
        private BatchRendererGroup m_Handle;
        private List<GPUBatch> m_Batches = new();
    }
}

//XDay