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
    internal class FogView : MonoBehaviour
    {
        public Texture2D FogMask => m_Mask;

        public void Init(FogSystem system, FixedVector3 size, GameObject fogPrefab)
        {
            m_System = system;
            m_GameObject = Object.Instantiate(fogPrefab);
            m_GameObject.transform.localScale = size.X.FloatValue * 0.1f * Vector3.one;
            m_GameObject.transform.position = new Vector3(size.X.FloatValue * 0.5f, 0, size.Z.FloatValue * 0.5f);
            m_Mask = new Texture2D(system.Resolution, system.Resolution, TextureFormat.RGBA32, false);
            m_LastMask = new Texture2D(system.Resolution, system.Resolution, TextureFormat.RGBA32, false);
            m_LastPixels = new Color32[system.Resolution * system.Resolution];
            m_CurrentPixels = new Color32[system.Resolution * system.Resolution];
            var renderer = m_GameObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial.SetTexture("_MainTex", m_LastMask);

            InitMask();

            if (!system.Enable)
            {
                m_GameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            Helper.DestroyUnityObject(m_GameObject);
            Helper.DestroyUnityObject(m_Mask);
            Helper.DestroyUnityObject(m_LastMask);
        }

        private void Update()
        {
            if (!m_System.Enable)
            {
                return;
            }

            LerpMask(Time.deltaTime);
        }

        public void UpdateNewMask()
        {
            if (!m_System.Enable)
            {
                return;
            }

            var states = m_System.Visibility;
            var n = states.Length;
            for (var i = 0; i < n; i++)
            {
                var state = states[i];
                if (state == -1)
                {
                    m_CurrentPixels[i] = m_White;
                }
                else if (state > 0)
                {
                    m_CurrentPixels[i] = m_Black;
                }
                else if (state == 0)
                {
                    m_CurrentPixels[i] = m_Gray;
                }
                else
                {
                    Log.Instance?.Error($"Error");
                }
            }
            m_Mask.SetPixels32(m_CurrentPixels);
            m_Mask.Apply();
        }

        private void InitMask()
        {
            var n = m_CurrentPixels.Length;
            for (var i = 0; i < n; i++)
            {
                m_CurrentPixels[i] = m_White;
                m_LastPixels[i] = m_White;
            }
            m_LastMask.SetPixels32(m_LastPixels);
            m_Mask.SetPixels32(m_CurrentPixels);
            m_LastMask.Apply();
            m_Mask.Apply();
        }

        private void LerpMask(float dt)
        {
            var states = m_System.Visibility;
            float delta = m_LerpSpeed * dt;
            for (var i = 0; i < states.Length; i++)
            {
                m_LastPixels[i] = Helper.LerpColor(ref m_LastPixels[i], ref m_CurrentPixels[i], delta);
            }
            m_LastMask.SetPixels32(m_LastPixels);
            m_LastMask.Apply();
        }

        private GameObject m_GameObject;
        private Texture2D m_Mask;
        private Texture2D m_LastMask;
        private FogSystem m_System;
        private float m_LerpSpeed = 5.0f;
        private Color32[] m_LastPixels;
        private Color32[] m_CurrentPixels;
        private Color32 m_White = new Color32(0, 0, 0, 255);
        private Color32 m_Black = new Color32(0, 0, 0, 0);
        private Color32 m_Gray = new Color32(0, 0, 0, 123);
    }
}
