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

namespace XDay.AnimationAPI
{
    public class AnimationBakeMetadata : ScriptableObject
    {
        public string DefaultAnimName;
        public int FrameCountOfAllAnimations;
        public List<AnimationMetadata> Animations;

        public AnimationMetadata QueryMetadata(string name)
        {
            foreach (var info in Animations)
            {
                if (info.Name == name)
                {
                    return info;
                }
            }
            return null;
        }
    }

    [Serializable]
    public class AnimationMetadata
    {
        public string Name;
        public bool Loop;
        public string GUID;
        public int FrameCount;
        public float Length;
        public List<int> MeshBoneCount;
        public List<int> MeshFrameOffset;

        public void CreateRigShaderInfo(int part, out Vector4 playState, out Vector4 frameData)
        {
            playState = new Vector4(Time.time, Length, Loop ? 1 : 0, MeshBoneCount[part]);
            frameData = new Vector4(MeshFrameOffset[part], FrameCount - 1, 0, 0);
        }

        public void CreateVertexShaderInfo(int part, int vertexCount, int totalFrameCount, out Vector4 playState, out Vector4 frameData)
        {
            playState = new Vector4(Time.time, Length, Loop ? 1 : 0, vertexCount);
            frameData = new Vector4(MeshFrameOffset[part] / (float)totalFrameCount, (FrameCount - 1 ) / (float)totalFrameCount, 0, 0);
        }
    }
}


//XDay