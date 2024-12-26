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
    [ExecuteInEditMode]
    public class AnimationEntrance : MonoBehaviour
    {
        public string DefaultAnimationName;
        public AnimationType AnimationType;
        public AnimationBakeMetadata Metadata;

        private void Awake()
        {
            m_Animator = CreateAnimator(gameObject, AnimationType);
            PlayAnimation(false);
        }

        public void PlayAnimation(bool alwaysPlay)
        {
            if (Metadata != null) 
            {
                if (string.IsNullOrEmpty(DefaultAnimationName))
                {
                    DefaultAnimationName = Metadata.DefaultAnimName;
                }
                m_Animator?.Play(DefaultAnimationName, alwaysPlay);
            }
        }

        private IGameObjectAnimator CreateAnimator(GameObject gameObject, AnimationType type)
        {
            switch (type)
            {
                case AnimationType.Rig:
                    return new GameObjectRigAnimator().Create(gameObject);
                case AnimationType.Vertex:
                    return new GameObjectVertexAnimator().Create(gameObject);
                default:
                    Debug.Assert(false, $"todo: {type}");
                    return null;
            }
        }

        private IGameObjectAnimator m_Animator;
    }
}

//XDay