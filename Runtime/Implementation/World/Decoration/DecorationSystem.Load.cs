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

namespace XDay.WorldAPI.Decoration
{
    internal partial class DecorationSystem
    {
        protected override void LoadGameDataInternal(string pluginName, IWorld world)
        {
            var deserializer = world.QueryGameDataDeserializer(world.ID, $"decoration@{pluginName}");

            deserializer.ReadInt32("GridData.Version");

            m_GridWidth = deserializer.ReadSingle("Grid Width");
            m_GridHeight = deserializer.ReadSingle("Grid Height");
            m_XGridCount = deserializer.ReadInt32("X Grid Count");
            m_YGridCount = deserializer.ReadInt32("Y Grid Count");
            m_Bounds = deserializer.ReadBounds("Bounds");
            m_Name = deserializer.ReadString("Name");
            m_ID = deserializer.ReadInt32("ID");
            m_ResourceDescriptorSystem = deserializer.ReadSerializable<ResourceDescriptorSystem>("Resource Descriptor System", true);
            m_LODSystem = deserializer.ReadSerializable<IPluginLODSystem>("Plugin LOD System", true);
            m_MaxLODObjectCount = deserializer.ReadInt32("Max LOD0 Object Count");

            var gridCount = m_XGridCount * m_YGridCount;
            m_GridData = new GridData[gridCount];
            for (var i = 0; i < gridCount; ++i)
            {
                var x = i % m_XGridCount;
                var y = i / m_XGridCount;
                m_GridData[i] = new GridData(m_LODSystem.LODCount, x, y);
                for (var lod = 0; lod < m_LODSystem.LODCount; ++lod)
                {
                    m_GridData[i].LODs[lod].ObjectGlobalIndices = deserializer.ReadInt32List("Object Index");
                }
            }

            m_DecorationMetaData = new DecorationMetaData
            {
                LODResourceChangeMasks = deserializer.ReadByteArray("LOD Resource Change Masks"),
                ResourceMetadataIndex = deserializer.ReadInt32Array("Resource Metadata Index"),
                Position = deserializer.ReadVector2Array("Position XZ"),
            };

            var resourceMetadataCount = deserializer.ReadInt32("Resource Metadata Count");
            m_ResourceMetadata = new List<ResourceMetadata>(resourceMetadataCount);
            for (var i = 0; i < resourceMetadataCount; ++i)
            {
                var batchIndex = deserializer.ReadInt32("GPU Batch ID");
                var rotation = deserializer.ReadQuaternion("Rotation");
                var scale = deserializer.ReadVector3("Scale");
                var bounds = deserializer.ReadRect("Bounds");
                var assetPath = deserializer.ReadString("Resource Path");
                m_ResourceMetadata.Add(new ResourceMetadata(batchIndex, rotation, scale, bounds, assetPath));
            }

            deserializer.Uninit();
        }
    }
}

//XDay