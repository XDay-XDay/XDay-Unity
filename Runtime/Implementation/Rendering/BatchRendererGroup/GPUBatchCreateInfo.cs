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
    internal class GPUBatchCreateInfo : IGPUBatchCreateInfo
    {
        public Mesh Mesh => m_Mesh;
        public Material Material => m_Material;
        public int InstanceSize
        {
            get
            {
                var size = 0;
                foreach (var propertyInfo in m_ShaderProperties)
                {
                    size += propertyInfo.DataSize;
                }
                return size;
            }
        }
        public int MaxInstanceCount => m_MaxInstanceCount;
        public int SubMeshIndex => m_SubMeshIndex;
        public int PropertyCount => m_ShaderProperties.Length;

        public GPUBatchCreateInfo(int maxInstanceCount, Mesh mesh, int subMeshIndex, Material material, IShaderPropertyDeclaration[] properties)
        {
            Debug.Assert(mesh != null, "GPUBatchCreateInfo: Invalid mesh");
            Debug.Assert(material != null, "GPUBatchCreateInfo: Invalid material");

            m_Mesh = mesh;
            m_Material = material;

            if (properties != null)
            {
                m_ShaderProperties = new IShaderPropertyDeclaration[properties.Length + 2];
            }
            else
            {
                m_ShaderProperties = new IShaderPropertyDeclaration[2];
            }

            m_ShaderProperties[0] = IShaderPropertyDeclaration.Create(ShaderPropertyType.PackedMatrix, "unity_ObjectToWorld");
            m_ShaderProperties[1] = IShaderPropertyDeclaration.Create(ShaderPropertyType.PackedMatrix, "unity_WorldToObject");

            if (properties != null)
            {
                for (var i = 0; i < properties.Length; ++i)
                {
                    m_ShaderProperties[i + 2] = properties[i];
                }
            }

            m_MaxInstanceCount = Mathf.Min(maxInstanceCount, m_InstanceCountLimit);
            m_SubMeshIndex = subMeshIndex;
        }

        public IShaderPropertyDeclaration GetProperty(int index)
        {
            return m_ShaderProperties[index];
        }

        private int m_MaxInstanceCount;
        private int m_SubMeshIndex;
        private Mesh m_Mesh;
        private Material m_Material;
        private IShaderPropertyDeclaration[] m_ShaderProperties;
        private const int m_InstanceCountLimit = 4096;
    }
}


//XDay