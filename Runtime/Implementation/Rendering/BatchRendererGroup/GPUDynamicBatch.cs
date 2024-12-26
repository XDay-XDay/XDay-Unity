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

using XDay.UtilityAPI;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace XDay.RenderingAPI.BRG
{
    public class GPUDynamicBatch : GPUBatch
    {
        public unsafe int AddInstance(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var freeIndex = NextIndex();
            if (freeIndex < 0)
            {
                return -1;
            }

            UpdateTransform(freeIndex, position, rotation, scale);

            return freeIndex;
        }

        public override void SetData<T>(NativeArray<T> data, int instanceIndex, int propertyIndex) where T : struct
        {
            Debug.LogError("not implemented, use SetVector and SetMatrix to set property!");
        }

        public void SetPackedMatrix(ref PackedMatrix matrix, int instanceIndex, int propertyIndex)
        {
            propertyIndex += m_PropertyIndexOffset;
            
            var index = QueryPropertyDataOffset(instanceIndex, propertyIndex, out _) / m_Float4Size;
            m_Buffer[index] = new float4(matrix.m00, matrix.m10, matrix.m20, matrix.m01);
            m_Buffer[index+1] = new float4(matrix.m11, matrix.m21, matrix.m02, matrix.m12);
            m_Buffer[index+2] = new float4(matrix.m22, matrix.m03, matrix.m13, matrix.m23);

            SetDirty();
        }

        public void SetVector(float4 value, int instanceIndex, int propertyIndex)
        {
            propertyIndex += m_PropertyIndexOffset;

            var index = QueryPropertyDataOffset(instanceIndex, propertyIndex, out _) / m_Float4Size;
            m_Buffer[index] = value;

            SetDirty();
        }

        public unsafe void UpdateTransform(int instanceIndex, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            PackedMatrix localToWorld;
            PackedMatrix worldToLocal;
            PackedMatrix.TRSWithInverse(ref pos, ref rot, ref scale, &localToWorld, &worldToLocal);
            SetPackedMatrix(ref localToWorld, instanceIndex, -2);
            SetPackedMatrix(ref worldToLocal, instanceIndex, -1);
        }

        public override void SetDirty()
        {
            m_Dirty = true;
        }

        public override void Sync()
        {
            if (m_Dirty)
            {
                GPUBuffer.SetData(m_Buffer);
                m_Dirty = false;
            }
        }

        protected override void OnInit()
        {
            m_Buffer = new NativeArray<float4>(MaxBufferSize / m_Float4Size, Allocator.Persistent);
        }

        protected override void OnUninit()
        {
            m_Buffer.Dispose();
        }

        private bool m_Dirty = true;
        protected NativeArray<float4> m_Buffer;
        private const int m_PropertyIndexOffset = 2;
        private const int m_Float4Size = 16;
    }
}


//XDay