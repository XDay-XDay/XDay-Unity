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

namespace XDay.WorldAPI.FOW
{
    public class FogRenderer
    {
        public FogRenderer(string name, Transform parent, int horizontalResolution, int verticalResolution, float gridWidth, float gridHeight, Vector3 origin, IAssetLoader assetLoader, string fogPrefabPath, string fogConfigPath, string blurShaderPath, Func<int, int, bool> isFogOpen)
        {
            m_HorizontalResolution = horizontalResolution;
            m_VerticalResolution = verticalResolution;
            m_AssetLoader = assetLoader;
            m_IsFogOpen = isFogOpen;

            m_FogConfig = m_AssetLoader.Load<FogConfig>(fogConfigPath);
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
            m_FogMaterial.SetTexture("_Mask", m_Blur.Output);
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_FogGameObject);
            Helper.DestroyUnityObject(m_Mask);
            m_Blur.OnDestroy();
        }

        private void CreateMask(string blurShaderPath)
        {
            Debug.Log($"support compute shaders: {SystemInfo.supportsComputeShaders}. random write: {SystemInfo.SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat.ARGBFloat)}, support format: {SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat)}, support NPOT: {SystemInfo.npotSupport == NPOTSupport.Full}");

            if (SystemInfo.supportsComputeShaders &&
                SystemInfo.SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat.ARGBFloat) &&
                SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) &&
                SystemInfo.npotSupport == NPOTSupport.Full)
            {
                m_Blur = new BlurCS(m_HorizontalResolution, m_VerticalResolution, m_AssetLoader.Load<ComputeShader>(blurShaderPath), repeat: false);
            }
            else
            {
                m_Blur = new BlurCPU(m_HorizontalResolution, m_VerticalResolution);
            }

            m_Mask = new Texture2D(m_HorizontalResolution, m_VerticalResolution, TextureFormat.RGBA32, false);

            m_MaskPixels = new Color32[m_HorizontalResolution * m_VerticalResolution];

            UpdateMask(true);
        }

        //r:修改前状态,g:修改前状态加偏移,b:修改后状态,a:修改后状态加偏移
        public void UpdateMask(bool reset)
        {
            if (reset)
            {
                //直接到修改后状态
                var idx = 0;
                for (var i = 0; i < m_VerticalResolution; ++i)
                {
                    for (var j = 0; j < m_HorizontalResolution; ++j)
                    {
                        var r = m_IsFogOpen.Invoke(j, i) ? (byte)0 : (byte)255;
                        m_MaskPixels[idx] = new Color32(r, 0, r, 0);
                        ++idx;
                    }
                }

                //偏移
                idx = 0;
                for (var i = 0; i < m_VerticalResolution; ++i)
                {
                    for (var j = 0; j < m_HorizontalResolution; ++j)
                    {
                        var offsetX = j + m_FogConfig.MaskOffset.x;
                        var offsetY = i + m_FogConfig.MaskOffset.y;
                        if (offsetX >= 0 && offsetX < m_HorizontalResolution &&
                            offsetY >= 0 && offsetY < m_VerticalResolution)
                        {
                            var offsetIdx = offsetY * m_HorizontalResolution + offsetX;
                            var pixel = m_MaskPixels[idx];
                            var offsetPixel = m_MaskPixels[offsetIdx];
                            offsetPixel.g = pixel.r;
                            offsetPixel.a = pixel.b;
                            m_MaskPixels[offsetIdx] = offsetPixel;
                        }
                        ++idx;
                    }
                }
            }
            else
            {
                var idx = 0;
                for (var i = 0; i < m_VerticalResolution; ++i)
                {
                    for (var j = 0; j < m_HorizontalResolution; ++j)
                    {
                        var pixel = m_MaskPixels[idx];
                        var r = pixel.b;
                        var b = m_IsFogOpen.Invoke(j, i) ? (byte)0 : (byte)255;
                        var g = pixel.a;

                        m_MaskPixels[idx] = new Color32(r, g, b, 0);
                        ++idx;
                    }
                }

                //偏移
                idx = 0;
                for (var i = 0; i < m_VerticalResolution; ++i)
                {
                    for (var j = 0; j < m_HorizontalResolution; ++j)
                    {
                        var offsetX = j + m_FogConfig.MaskOffset.x;
                        var offsetY = i + m_FogConfig.MaskOffset.y;
                        if (offsetX >= 0 && offsetX < m_HorizontalResolution &&
                            offsetY >= 0 && offsetY < m_VerticalResolution)
                        {
                            var offsetIdx = offsetY * m_HorizontalResolution + offsetX;
                            var pixel = m_MaskPixels[idx];
                            var offsetPixel = m_MaskPixels[offsetIdx];
                            offsetPixel.a = pixel.b;
                            m_MaskPixels[offsetIdx] = offsetPixel;
                        }
                        ++idx;
                    }
                }
            }

            m_Mask.SetPixels32(m_MaskPixels);
            m_Mask.Apply();
            m_Blur.Blur(m_Mask, 1);

            if (!reset)
            {
                StartFade();
            }
        }

        public void Update(float dt)
        {
            if (m_IsOpening)
            {
                var finish = m_Ticker.Step(dt);
                m_FogMaterial.SetFloat("_ElapsedTime", m_Ticker.NormalizedTime * m_FogConfig.FadeDuration);
                if (finish)
                {
                    OnFadeFinish();
                }
            }
        }

        private void StartFade()
        {
            m_FogMaterial.SetFloat("_FadeDuration", m_FogConfig.FadeDuration);
            m_FogMaterial.EnableKeyword("FADEFOGWAR");
            m_IsOpening = true;
            m_Ticker.Start(m_FogConfig.FadeDuration);
        }

        private void OnFadeFinish()
        {
            m_IsOpening = false;
            m_FogMaterial.DisableKeyword("FADEFOGWAR");
            SwapChannel();
        }

        private void SwapChannel()
        {
            var idx = 0;
            for (var i = 0; i < m_VerticalResolution; ++i)
            {
                for (var j = 0; j < m_HorizontalResolution; ++j)
                {
                    var pixel = m_MaskPixels[idx];
                    pixel.r = pixel.b;
                    pixel.g = pixel.a;
                    m_MaskPixels[idx] = pixel;
                    ++idx;
                }
            }

            m_Mask.SetPixels32(m_MaskPixels);
            m_Mask.Apply();
            m_Blur.Blur(m_Mask, 1);
        }

        private readonly int m_HorizontalResolution;
        private readonly int m_VerticalResolution;
        private readonly GameObject m_FogGameObject;
        private Texture2D m_Mask;
        //r:修改前状态,g:修改前状态加偏移,b:修改后状态,a:修改后状态加偏移
        private Color32[] m_MaskPixels;
        private BlurBase m_Blur;
        private readonly FogConfig m_FogConfig;
        private readonly Material m_FogMaterial;
        private bool m_IsOpening = false;
        private readonly Ticker m_Ticker = new();
        private readonly IAssetLoader m_AssetLoader;
        private readonly Func<int, int, bool> m_IsFogOpen;
    }
}