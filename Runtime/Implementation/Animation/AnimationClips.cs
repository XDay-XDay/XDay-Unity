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
using Animancer;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XDay
{
    [System.Serializable]
    public class AnimClipSetting
    {
        public string Name; 
        public AnimationClip Clip;
    }

    public class AnimationClips : MonoBehaviour
    {
        public List<AnimClipSetting> Clips = new();

        public AnimationClip GetClip(string name)
        {
            foreach (var item in Clips)
            {
                if (item.Name == name)
                {
                    return item.Clip;
                }
            }
            return null;
        }

        public void PlayAnimation(string animName)
        {
            var animancer = gameObject.GetComponent<AnimancerComponent>();
            if (animancer == null)
            {
                Debug.LogError("No animancer component found!");
                return;
            }

            var clip = GetClip(animName);
            if (clip == null)
            {
                Debug.LogError($"clip {animName} is null");
            }
            else
            {
                var state = animancer.Play(clip);
                state.Time = 0;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AnimationClips))]
    internal class AnimationClipsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GetNames();

            EditorGUILayout.BeginHorizontal();
            m_Index = EditorGUILayout.Popup("动画", m_Index, m_AnimNames);
            if (GUILayout.Button("播放"))
            {
                if (m_Index >= 0 && m_Index < m_AnimNames.Length)
                {
                    var animClips = target as AnimationClips;
                    animClips.PlayAnimation(m_AnimNames[m_Index]);
                }
            }
            EditorGUILayout.EndHorizontal();

            base.OnInspectorGUI();
        }

        private void GetNames()
        {
            var animClips = target as AnimationClips;
            var n = animClips.Clips.Count;
            if (m_AnimNames == null || m_AnimNames.Length != n)
            {
                m_AnimNames = new string[n];
            }
            for (var i = 0; i < n; ++i)
            {
                m_AnimNames[i] = animClips.Clips[i].Name;
            }
        }

        private string[] m_AnimNames;
        private int m_Index;
    }
#endif
}
