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

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using XDay.UtilityAPI.Math;

namespace XDay.RenderingAPI.BRG
{
    public class GPUStaticBatch : GPUBatch
    {
        public unsafe int AddInstance(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var index = NextIndex();
            if (index < 0)
            {
                return -1;
            }

            PackedMatrix* world2ObjectTemp = (PackedMatrix*)m_World2ObjectTemp.GetUnsafePtr();
            PackedMatrix* object2WorldTemp = (PackedMatrix*)m_Object2WorldTemp.GetUnsafePtr();
            PackedMatrix.TRSWithInverse(ref position, ref rotation, ref scale, object2WorldTemp, world2ObjectTemp);

            SetData(m_Object2WorldTemp, index, 0);
            SetData(m_World2ObjectTemp, index, 1);

            return index;
        }

        protected override void OnInit()
        {
            m_Object2WorldTemp = new NativeArray<PackedMatrix>(1, Allocator.Persistent);
            m_World2ObjectTemp = new NativeArray<PackedMatrix>(1, Allocator.Persistent);
        }

        protected override void OnUninit()
        {
            m_Object2WorldTemp.Dispose();
            m_World2ObjectTemp.Dispose();
        }

        private NativeArray<PackedMatrix> m_Object2WorldTemp;
        private NativeArray<PackedMatrix> m_World2ObjectTemp;
    }
}

//XDay