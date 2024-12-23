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

using XDay.SerializationAPI;
using System.Collections.Generic;
using UnityEngine;

namespace XDay.WorldAPI
{
    public class ResourceDescriptor : WorldObject, IResourceDescriptor
    {
        public List<int> LODs => m_LODs;
        public string[] LODPaths => m_LODPaths;
        public int LODCount => m_LODs == null ? 0 : m_LODs.Count;
        public override string TypeName => "ResourceDescriptor";

        public ResourceDescriptor()
        {
        }

        public ResourceDescriptor(int id, int index, string path)
            : base(id, index)
        {
            m_LOD0 = path;
        }

        public bool HasLOD(int lod)
        {
            if (m_LODs == null) 
            {
                return false;
            }
 
            foreach (var foundLOD in m_LODs)
            {
                if (foundLOD == lod)
                {
                    return true;
                }
            }
            return false;
        }

        public int SelectLOD(int lod)
        {
            if (m_LODs == null)
            {
                return -1;
            }
            
            for (var i = m_LODs.Count - 1; i >= 0; --i)
            {
                if (lod > m_LODs[i])
                {
                    return m_LODs[i];
                }
            }
            return -1;
        }

        public int QueryLODGroup(int lod)
        {
            return m_LODGroup[lod];
        }

        public string GetPath(int lod)
        {
            if (m_LODs != null)
            {
                lod = Mathf.Clamp(lod, 0, m_LODPaths.Length - 1);
                return m_LODPaths[lod];
            }
            return m_LOD0;
        }

        public override void GameDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("ResourceDescriptor.Version");

            base.GameDeserialize(deserializer, label);

            m_LODGroup = deserializer.ReadInt32Array("LOD Group");
            m_LODPaths = deserializer.ReadStringArray("LOD Paths");
            m_LOD0 = deserializer.ReadString("LOD 0");
            m_LODs = deserializer.ReadInt32List("Found LODs");
            if (m_LODs.Count == 0)
            {
                m_LODPaths = null;
                m_LODs = null;
            }
        }

        protected string m_LOD0;
        protected string[] m_LODPaths;
        protected List<int> m_LODs;
        protected int[] m_LODGroup;
    }
}

//XDay