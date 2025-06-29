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
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    internal class EditorResourceDescriptorSystem : ResourceDescriptorSystem, IEditorResourceDescriptorSystem
    {
        public override string TypeName => "EditorResourceDescriptorSystem";
        public string GameTypeName => "ResourceDescriptorSystem";

        public IEditorResourceDescriptor CreateDescriptorIfNotExists(string path, IWorld world)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.Assert(false, $"invalid path {path}");
                return null;
            }

            var descriptor = QueryDescriptor(path);
            if (descriptor != null)
            {
                return descriptor as IEditorResourceDescriptor;
            }

            var newDescriptor = CreateDescriptor(world.AllocateObjectID(), m_Descriptors.Count, path);
            newDescriptor.Init(world);
            AddDescriptor(path, newDescriptor);
            return newDescriptor as IEditorResourceDescriptor;
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("EditorResourceDescriptorSystem.Version");

            m_UninitedDescriptors = deserializer.ReadList("Resource Descriptors", (index) =>
            {
                return deserializer.ReadSerializable<IResourceDescriptor>($"Resource Descriptor {index}", false);
            });
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            //RemoveExpiredDescriptors();

            serializer.WriteInt32(m_Version, "EditorResourceDescriptorSystem.Version");

            var descriptors = new List<IResourceDescriptor>();
            foreach (var kv in m_Descriptors)
            {
                descriptors.Add(kv.Value);
            }

            serializer.WriteList(descriptors, "Resource Descriptors", (descriptor, index) =>
            {
                serializer.WriteSerializable(descriptor, $"Resource Descriptor {index}", converter, false);
            });
        }

        public override void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "ResourceDescriptorSystem.Version");

            var descriptors = new List<IResourceDescriptor>();
            foreach (var descriptor in m_Descriptors.Values)
            {
                descriptors.Add(descriptor);
            }

            serializer.WriteList(descriptors, "Resource Descriptors", (descriptor, index) =>
            {
                serializer.WriteSerializable(descriptor, $"Resource Descriptor {index}", converter, true);
            });
        }

        protected override IResourceDescriptor CreateDescriptor(int id, int index, string path)
        {
            return new EditorResourceDescriptor(id, index, path);
        }

        private void RemoveExpiredDescriptors()
        {
            foreach (var path in GetExpiredDescriptorsPath())
            {
                m_Descriptors[path].Uninit();
                m_Descriptors.Remove(path);
            }
        }

        private List<string> GetExpiredDescriptorsPath()
        {
            var paths = new List<string>();
            foreach (var kv in m_Descriptors)
            {
                var descriptor = kv.Value;
                var path = descriptor.GetPath(0);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var newPath = AssetDatabase.GetAssetPath(asset);
                if (newPath != path || asset == null)
                {
                    paths.Add(kv.Key);
                }
            }
            return paths;
        }

        private void AddDescriptor(string path, IResourceDescriptor descriptor)
        {
            var lod0Path = descriptor.GetPath(0);
            Debug.Assert(path == lod0Path);
            if (string.IsNullOrEmpty(lod0Path))
            {
                Debug.Assert(false);
                return;
            }

            if (!m_Descriptors.ContainsKey(lod0Path))
            {
                m_Descriptors.Add(lod0Path, descriptor);
            }
        }

        private const int m_Version = 1;
        private const int m_RuntimeVersion = 1;
    }
}

//XDay