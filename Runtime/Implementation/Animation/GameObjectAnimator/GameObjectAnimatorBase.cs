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

using UnityEngine;

namespace XDay.AnimationAPI
{
    internal abstract class GameObjectAnimatorBase : IGameObjectAnimator
    {
        public IGameObjectAnimator Create(GameObject gameObject)
        {
            m_BakeMetadata = gameObject.GetComponentInChildren<AnimationEntrance>().Metadata;
            m_MeshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
            m_MeshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
            return this;
        }

        public bool IsPlaying(string name)
        {
            return m_PlayingAnimName == name;
        }

        public void Play(string name, bool alwaysPlay = false)
        {
            if (!alwaysPlay && 
                IsPlaying(name))
            {
                return;
            }

            m_ActiveMetadata = m_BakeMetadata.QueryMetadata(name);
            if (m_ActiveMetadata != null)
            {
                for (var i = 0; i < m_MeshFilters.Length; i++)
                {
                    CreateShaderInfo(i, out var playState, out var frameData);

                    m_MeshRenderers[i].GetPropertyBlock(m_PropertyBlock);
                    m_PropertyBlock.SetVector(AnimationDefine.ANIM_FRAME_ID, frameData);
                    m_PropertyBlock.SetVector(AnimationDefine.ANIM_PLAY_ID, playState);
                    m_MeshRenderers[i].SetPropertyBlock(m_PropertyBlock);
                }
                m_PlayingAnimName = name;
            }
        }

        protected abstract void CreateShaderInfo(int part, out Vector4 playInfo, out Vector4 frameInfo);

        private string m_PlayingAnimName = "";
        protected AnimationMetadata m_ActiveMetadata;
        protected AnimationBakeMetadata m_BakeMetadata;
        private MaterialPropertyBlock m_PropertyBlock = new();
        private MeshRenderer[] m_MeshRenderers;
        protected MeshFilter[] m_MeshFilters;
    }
}

//XDay