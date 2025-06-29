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

using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.GamePlayAPI
{
    internal class AudioSystem
    {
        public bool Mute { get => m_Mute; set => m_Mute = value; }
        public bool EnableUIAudioListener { set => m_UIListener.gameObject.SetActive(value); }

        public void Init()
        {
            m_UIAudioSource = CreateAudioSource("UI Audio", false, 0.5f);
            m_BGAudioSource = CreateAudioSource("BG Audio", true, 0.4f);
            var listenerObj = new GameObject("UI Audio Listener");
            m_UIListener = listenerObj.AddComponent<AudioListener>();
        }

        public void Uninit()
        {
            if (m_UIAudioSource != null)
            {
                Helper.DestroyUnityObject(m_UIAudioSource.gameObject);
            }

            if (m_BGAudioSource != null)
            {
                Helper.DestroyUnityObject(m_BGAudioSource.gameObject);
            }

            if (m_UIListener != null)
            {
                Helper.DestroyUnityObject(m_UIListener.gameObject);
            }
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null)
            {
                Log.Instance?.Error($"clip is null");
                return;
            }

            if (m_Mute)
            {
                return;
            }

            if (m_BGAudioSource == null || m_BGAudioSource.clip != clip)
            {
                m_BGAudioSource.clip = clip;
                m_BGAudioSource.Play();
            }
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null)
            {
                Log.Instance?.Error($"clip is null");
                return;
            }

            if (m_Mute)
            {
                return;
            }

            m_UIAudioSource.clip = clip;
            m_UIAudioSource.Play();
        }

        private AudioSource CreateAudioSource(string name, bool loop, float volume)
        {
            var obj = new GameObject(name);
            var source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = volume;
            source.spatialBlend = 0;
            source.loop = loop;
            return source;
        }

        private AudioSource m_BGAudioSource;
        private AudioSource m_UIAudioSource;
        private bool m_Mute = false;
        private AudioListener m_UIListener;
    }
}
