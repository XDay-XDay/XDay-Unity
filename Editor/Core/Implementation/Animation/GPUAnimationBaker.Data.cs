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
    internal partial class GPUAnimationBaker
    {
        public abstract class AnimationBakeData
        {
            public abstract int FrameCount { get; }

            public bool Loop;
            public float Length;
            public AnimationClip Clip;
        }

        public class RiggedAnimationBakeData : AnimationBakeData
        {
            public override int FrameCount => RigTransformsEachFrame.Count;

            public List<Matrix4x4[]> RigTransformsEachFrame = new();
        }

        public class VertexAnimationBakeData : AnimationBakeData
        {
            public int VertexCount => VerticesEachFrame[0].Length;
            public override int FrameCount => VerticesEachFrame.Count;

            public List<Vector3[]> VerticesEachFrame = new();
            public float Framerate;
            public float NormalizedLength;
        }

        public abstract class RendererBakeData
        {
            public List<AnimationBakeData> AnimationsBakeData => m_AnimationsBakeData;
            public int FrameCountOfAllAnimations { get; private set; }

            public void Add(AnimationBakeData animation)
            {
                m_AnimationsBakeData.Add(animation);
                FrameCountOfAllAnimations += animation.FrameCount;
            }

            private List<AnimationBakeData> m_AnimationsBakeData = new();
        }

        public class RendererRiggedAnimationBakeData : RendererBakeData
        {
            public RendererRiggedAnimationBakeData(RigManager manager)
            {
                RigManager = manager;
            }

            public RigManager RigManager { get;}
        }

        public class RendererVertexAnimationBakeData : RendererBakeData
        {
            public int GetMeshVertexCount()
            {
                if (AnimationsBakeData.Count > 0)
                {
                    return (AnimationsBakeData[0] as VertexAnimationBakeData).VertexCount;
                }
                return 0;
            }
        }

        public class AnimationTextureData
        {
            public string Name;
            public Color[] Pixels;
            public Vector2Int Size;
            public Dictionary<string, int> AnimationsFrameOffset = new();
        }
    }
}

//XDay