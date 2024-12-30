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
using XDay.AnimationAPI;

namespace XDay.WorldAPI.Decoration
{
    [Serializable]
    public class InstanceAnimatorBatchInfo
    {
        public int InstanceCount = 1;
        public List<InstanceAnimatorData> LODs = new();

        public void AddLOD(InstanceAnimatorData data)
        {
            Debug.Assert(data != null);
            LODs.Add(data);
        }
    }

    public class InstanceAnimatorBatchInfoRegistry : ScriptableObject
    {
        public void InstanceCountInc(int index)
        {
            ++m_Batches[ToLocalIndex(index)].InstanceCount;
        }

        public InstanceAnimatorBatchInfo GetBatch(int index)
        {
            return m_Batches[ToLocalIndex(index)];
        }

        public InstanceAnimatorData GetBatchData(int index, int lod)
        {
            return m_Batches[ToLocalIndex(index)].LODs[lod];
        }

        public int CreateBatch()
        {
            m_Batches.Add(new InstanceAnimatorBatchInfo());
            return ToGlobalIndex(m_Batches.Count - 1);
        }

        private int ToLocalIndex(int index)
        {
            return index - GameDefine.ANIMATOR_BATCH_START_ID;
        }

        private int ToGlobalIndex(int index)
        {
            return index + GameDefine.ANIMATOR_BATCH_START_ID;
        }

        [SerializeField]
        private List<InstanceAnimatorBatchInfo> m_Batches = new();
    }
}
