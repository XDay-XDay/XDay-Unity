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

namespace XDay.UtilityAPI
{
    [ExecuteInEditMode]
    public class DisplayName : MonoBehaviour
    {
        public void Create(string text, Camera camera, bool show)
        {
            m_Camera = camera;
            m_TextGameObject = new GameObject("Text");
            m_TextMesh = m_TextGameObject.AddComponent<TextMesh>();
            m_TextMesh.text = text;
            m_TextMesh.color = Color.black;
            m_TextMesh.fontSize = 40;
            m_TextMesh.font.material.renderQueue = 3500;
            m_TextGameObject.transform.SetParent(gameObject.transform, false);
            m_TextGameObject.transform.localScale = Vector3.one * 0.2f;
            m_TextGameObject.transform.localPosition = new Vector3(0, 2, 0);

            Show(show);
        }

        public void Show(bool show)
        {
            m_TextGameObject?.SetActive(show);
        }

        public void SetText(string text)
        {
            m_TextMesh.text = text;
        }

        private void OnDestroy()
        {
            DestroyImmediate(m_TextGameObject);
        }

        void Update()
        {
            if (m_Camera != null)
            {
                m_TextGameObject.transform.rotation = m_Camera.transform.rotation;
            }
        }

        private GameObject m_TextGameObject;
        private Camera m_Camera;
        private TextMesh m_TextMesh;
    }
}
