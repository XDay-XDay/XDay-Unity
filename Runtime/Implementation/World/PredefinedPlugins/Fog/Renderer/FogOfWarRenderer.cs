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

using System;
using UnityEngine;
using XDay.AssetAPI;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Fog
{
    /// <summary>
    /// 绘制RTS类型游戏的动态迷雾
    /// </summary>
    public class FogOfWarRenderer : IFogRenderer
    {
        public FogOfWarRenderer(string name, Transform parent, int horizontalResolution, int verticalResolution, float gridWidth, float gridHeight, Vector3 origin, IAssetLoader assetLoader, string fogPrefabPath, string blurShaderPath, Func<int, int, bool> isFogOpen)
        {
            m_HorizontalResolution = horizontalResolution;
            m_VerticalResolution = verticalResolution;
            m_AssetLoader = assetLoader;
            m_IsFogOpenFunc = isFogOpen;

            m_FogGameObject = m_AssetLoader.LoadGameObject(fogPrefabPath);
            m_FogGameObject.name = name;
            m_FogGameObject.transform.SetParent(parent);

            var width = horizontalResolution * gridWidth;
            var height = verticalResolution * gridHeight;
            m_FogGameObject.transform.localScale = new Vector3(width, 1, height);
            m_FogGameObject.transform.position = origin + new Vector3(width * 0.5f, 0, height * 0.5f);
            var fogRenderer = m_FogGameObject.transform.GetChild(0).GetComponent<MeshRenderer>();
            m_FogMaterial = fogRenderer.sharedMaterial;

            CreateMask(blurShaderPath);

            m_FogMaterial.SetTexture("_Mask", m_Blur.BlurredOutput);

#if false
            var cur = Helper.FindChildNoAlloc(m_FogGameObject.transform, "Cur");
            if (cur != null)
            {
                cur.GetComponentInChildren<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", m_Blur.Cur);
            }
            var next = Helper.FindChildNoAlloc(m_FogGameObject.transform, "Next");
            if (next != null)
            {
                next.GetComponentInChildren<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", m_Blur.Next);
            }
#endif
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_FogGameObject);
            Helper.DestroyUnityObject(m_MaskTexture);
            m_Blur.OnDestroy();
        }

        private void CreateMask(string blurShaderPath)
        {
            if (SupportCS())
            {
                m_Blur = new BlendAndBlur(m_HorizontalResolution, m_VerticalResolution, m_AssetLoader.Load<ComputeShader>(blurShaderPath), false);
            }
            else
            {
                Debug.Assert(false);
            }

            m_MaskData = new Color32[m_HorizontalResolution * m_VerticalResolution];
            m_MaskTexture = new Texture2D(m_HorizontalResolution, m_VerticalResolution, TextureFormat.RGBA32, false);

            UpdateMask(true);
        }

        private bool SupportCS()
        {
            return
                SystemInfo.supportsComputeShaders &&
                SystemInfo.npotSupport == NPOTSupport.Full &&
                SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) &&
                SystemInfo.SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat.ARGBHalf);
        }

        public void UpdateMask(bool reset)
        {
            var idx = 0;
            for (var i = 0; i < m_VerticalResolution; ++i)
            {
                for (var j = 0; j < m_HorizontalResolution; ++j)
                {
                    var r = m_IsFogOpenFunc.Invoke(j, i) ? (byte)0 : (byte)255;
                    m_MaskData[idx] = new Color32(r, 0, 0, 0);
                    ++idx;
                }
            }

            m_MaskTexture.SetPixels32(m_MaskData);
            m_MaskTexture.Apply();

            if (reset)
            {
                Graphics.Blit(m_MaskTexture, m_Blur.Cur);
            }

            m_Dirty = true;
        }

        public void Update(float dt)
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                Graphics.Blit(m_MaskTexture, m_Blur.Next);
            }
            m_Blur.Blur(m_MaskTexture, m_LerpSpeed * dt);
        }

        private readonly GameObject m_FogGameObject;
        private readonly Material m_FogMaterial;
        private readonly int m_HorizontalResolution;
        private readonly int m_VerticalResolution;
        private Color32[] m_MaskData;
        private Texture2D m_MaskTexture;
        private readonly IAssetLoader m_AssetLoader;
        private readonly Func<int, int, bool> m_IsFogOpenFunc;
        private BlendAndBlur m_Blur;
        private bool m_Dirty;
        private float m_LerpSpeed = 5f;
    }
}