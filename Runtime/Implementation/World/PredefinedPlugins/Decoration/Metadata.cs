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
using UnityEngine;

namespace XDay.WorldAPI.Decoration
{
    internal class ResourceMetadata
    {
        public ResourceMetadata(int gpuBatchID, Quaternion rotation, Vector3 scale, Rect bounds, string path, DecorationTagType type)
        {
            GPUBatchID = gpuBatchID;
            Rotation = rotation;
            Scale = scale;
            Bounds = bounds;
            Path = path;
            Type = type;
        }

        public void Init(ResourceDescriptorSystem descriptorSystem)
        {
            ResourceDescriptor = descriptorSystem.QueryDescriptor(Path);
            Debug.Assert(ResourceDescriptor != null);
        }

        public int QueryLODGroup(int lod)
        {
            return ResourceDescriptor.QueryLODGroup(lod);
        }

        public IResourceDescriptor ResourceDescriptor { get; private set; }
        public int GPUBatchID { get; set; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }
        public Rect Bounds { get; }
        public string Path { get; }
        public DecorationTagType Type { get; }
    }

    internal class DecorationMetaData
    {
        public byte[] LODResourceChangeMasks { get; set; }
        public int[] ResourceMetadataIndex { get; set; }
        public Vector3[] Position { get; set; }
    }

    internal class GridData
    {
        public GridLODData[] LODs { get; set; }
        public int ActiveStateCounter { set; get; } = 0;
        public int X { get; }
        public int Y { get; }
        public int LODCount => LODs.Length;

        public GridData(int lodCount, int x, int y)
        {
            LODs = new GridLODData[lodCount];
            for (var lod = 0; lod < lodCount; ++lod)
            {
                LODs[lod] = new GridLODData();
            }
            X = x;
            Y = y;
        }

        public void Init(DecorationSystem system)
        {
            m_LODTasks = new List<FrameTaskDecorationToggle>(LODs.Length);
            for (var lod = 0; lod < LODs.Length; ++lod)
            {
                m_LODTasks.Add(new FrameTaskDecorationToggle(system, LODs[lod].ObjectGlobalIndices.Count, lod, X, Y));
            }
        }

        public FrameTaskDecorationToggle GetFrameTask(int lod)
        {
            return m_LODTasks[lod];
        }

        public int GetObjectIndex(int index, int lod)
        {
            return LODs[lod].ObjectGlobalIndices[index];
        }

        public int GetObjectCount(int lod)
        {
            return LODs[lod].ObjectGlobalIndices.Count;
        }

        private List<FrameTaskDecorationToggle> m_LODTasks;
    }

    internal class GridLODData
    {
        public List<int> ObjectGlobalIndices { get; set; } = new();
    }
}

//XDay
