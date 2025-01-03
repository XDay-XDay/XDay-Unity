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

namespace XDay.AnimationAPI.Editor
{
    internal class RigManager
    {
        public int RigCount => m_Rigs.Count;

        public RigManager(SkinnedMeshRenderer renderer)
        {
            var bindposes = renderer.sharedMesh.bindposes;
            var bones = renderer.bones;
            if (bones.Length != bindposes.Length)
            {
                Debug.LogError($"{renderer.name} bind pose number and rig number not equal!");
            }

            for (var k = 0; k < bones.Length; ++k)
            {
                var rig = QueryRigTransform(bones[k].name);
                if (rig == null)
                {
                    AddRig(bones[k], bindposes[k]);
                }
                else
                {
                    var pose = QueryBindpose(bones[k].name);
                    if (bindposes[k] != pose)
                    {
                        Debug.Assert(false);
                    }
                }
            }
        }

        public string GetRootRigName()
        {
            var root = GetRootRig();
            return root.name;
        }

        public Transform GetRootRig()
        {
            if (m_RootRig == null)
            {
                m_RootRig = CalculateRootRig();
            }
            return m_RootRig;
        }

        public int QueryRigIndex(Transform transform)
        {
            for (var i = 0; i < m_Rigs.Count; ++i)
            {
                if (m_Rigs[i].Transform.name == transform.name)
                {
                    return i;
                }
            }
            return -1;
        }

        public Transform QueryRigTransform(string name)
        {
            var rig = QueryRig(name);
            return rig?.Transform;
        }

        public void AddRig(Transform rig, Matrix4x4 bindpose)
        {
            if (QueryRig(rig.name) == null)
            {
                m_Rigs.Add(new RigInfo(rig, bindpose));
            }
        }

        public void AddBindpose(Transform[] rigs, Matrix4x4[] bindposes)
        {
            for (var i = 0; i < rigs.Length; ++i)
            {
                var rig = QueryRig(rigs[i].name);
                rig.Bindpose = bindposes[i];
            }
        }

        public Matrix4x4 QueryBindpose(string name)
        {
            var rig = QueryRig(name);
            if (rig != null)
            {
                return rig.Bindpose;
            }

            Debug.Assert(false, $"get bind pose {name} failed");
            return Matrix4x4.identity;
        }

        public Matrix4x4 GetBindpose(int idx)
        {
            return m_Rigs[idx].Bindpose;
        }

        private RigInfo QueryRig(string name)
        {
            for (var i = 0; i < m_Rigs.Count; i++)
            {
                if (m_Rigs[i].Transform.name == name)
                {
                    return m_Rigs[i];
                }
            }
            return null;
        }

        private Transform CalculateRootRig()
        {
            var rootRigs = new List<Transform>();
            for (var i = 0; i < m_Rigs.Count; ++i)
            {
                var isRoot = true;
                var rig = m_Rigs[i];
                for (var r = 0; r < m_Rigs.Count; ++r)
                {
                    if (r != i && 
                        m_Rigs[r].Transform.gameObject.IsChild(rig.Transform.gameObject))
                    {
                        isRoot = false;
                        break;
                    }
                }
                if (isRoot)
                {
                    rootRigs.Add(m_Rigs[i].Transform);
                }
            }
            if (rootRigs.Count == 1)
            {
                return rootRigs[0];
            }
            return rootRigs[0].parent;
        }

        private class RigInfo
        {
            public Transform Transform => m_Transform;
            public Matrix4x4 Bindpose { get => m_Bindpose; set => m_Bindpose = value; }

            public RigInfo(Transform transform)
            {
                m_Transform = transform;
            }

            public RigInfo(Transform transform, Matrix4x4 bindpose)
            {
                m_Transform = transform;
                m_Bindpose = bindpose;
            }

            private Transform m_Transform;
            private Matrix4x4 m_Bindpose;
        }

        private Transform m_RootRig;
        private List<RigInfo> m_Rigs = new();
    }
}


//XDay