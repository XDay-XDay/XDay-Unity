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



using UnityEngine;

namespace XDay.RenderingAPI.BRG
{
    public enum ShaderPropertyType
    {
        Float,
        Int,
        Vector,
        Color,
        PackedMatrix,
    }

    /// <summary>
    /// custom shader properties used in batch renderer group
    /// </summary>
    public interface IShaderPropertyDeclaration
    {
        static IShaderPropertyDeclaration Create(ShaderPropertyType type, string name, bool perInstance = true)
        {
            return new ShaderPropertyDeclaration(type, name, perInstance);
        }

        string Name { get; }
        int DataSize { get; }
        bool IsPerInstance { get; }
    }

    /// <summary>
    /// parameter when create a GPUBatch
    /// </summary>
    public interface IGPUBatchCreateInfo
    {
        static IGPUBatchCreateInfo Create(int maxInstanceCount, Mesh mesh, int subMeshIndex, Material material, IShaderPropertyDeclaration[] properties)
        {
            return new GPUBatchCreateInfo(maxInstanceCount, mesh, subMeshIndex, material, properties);
        }

        Mesh Mesh { get; }
        Material Material { get; }
        int PropertyCount { get; }
        int InstanceSize { get; }
        int MaxInstanceCount { get; }
        int SubMeshIndex { get; }

        /// <summary>
        /// get shader property declaration
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        IShaderPropertyDeclaration GetProperty(int index);
    }
    
    /// <summary>
    /// manage GPUBatches
    /// </summary>
    public interface IGPUBatchManager
    {
        static IGPUBatchManager Create()
        {
            return new GPUBatchManager();
        }

        /// <summary>
        /// number of active batches
        /// </summary>
        int BatchCount { get; }

        void OnDestroy();

        /// <summary>
        /// create batch
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createInfo"></param>
        /// <returns></returns>
        T CreateBatch<T>(IGPUBatchCreateInfo createInfo) where T : GPUBatch, new();
        GPUBatch GetBatch(int index);
        T QueryBatch<T>(int id) where T : GPUBatch;

        /// <summary>
        /// find batch using specified mesh and material
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mesh"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        T QueryRenderableBatch<T>(Mesh mesh, Material material) where T : GPUBatch;

        /// <summary>
        /// upload batch data to GPU
        /// </summary>
        void Sync();
    }
}
