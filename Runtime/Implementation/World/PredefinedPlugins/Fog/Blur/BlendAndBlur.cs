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

namespace XDay.WorldAPI.Fog
{
    public class BlendAndBlur
    {
        public RenderTexture BlurredOutput => m_Temp2;
        public RenderTexture Cur => m_Cur;
        public RenderTexture Next => m_Next;

        public BlendAndBlur(int horizontalResolution, int verticalResolution, ComputeShader blurShader, bool repeat)
        {
            m_BlurShader = blurShader;

            m_HorizontalKernel = m_BlurShader.FindKernel("HorizontalBlur");
            m_VerticalKernel = m_BlurShader.FindKernel("VerticalBlur");
            m_BlendKernel = m_BlurShader.FindKernel("Blend");

            CreateRenderTextures(horizontalResolution, verticalResolution, repeat);
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_Temp1);
            Helper.DestroyUnityObject(m_Temp2);
            Helper.DestroyUnityObject(m_Cur);
            Helper.DestroyUnityObject(m_Next);
        }

        public void Blur(Texture2D texture, float ratio)
        {
            ratio = Mathf.Clamp01(ratio);

            //blend
            m_BlurShader.SetFloat("Ratio", ratio);
            m_BlurShader.SetTexture(m_BlendKernel, "CurTex", m_Cur);
            m_BlurShader.SetTexture(m_BlendKernel, "NextTex", m_Next);
            m_BlurShader.SetTexture(m_BlendKernel, "OutputTex", m_Temp1);
            DispatchShader(m_BlendKernel, m_Temp1);
            (m_Cur, m_Temp1) = (m_Temp1, m_Cur);

            //blur
            m_BlurShader.SetTexture(m_HorizontalKernel, "NextTex", m_Cur);
            m_BlurShader.SetTexture(m_HorizontalKernel, "OutputTex", m_Temp1);
            DispatchShader(m_HorizontalKernel, m_Temp1);

            m_BlurShader.SetTexture(m_VerticalKernel, "NextTex", m_Temp1);
            m_BlurShader.SetTexture(m_VerticalKernel, "OutputTex", m_Temp2);
            DispatchShader(m_VerticalKernel, m_Temp2);
        }

        private void CreateRenderTextures(int width, int height, bool repeat)
        {
            m_Cur = new RenderTexture(width, height, 0)
            {
                filterMode = FilterMode.Bilinear,
                format = RenderTextureFormat.ARGBHalf,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = true,
            };
            var ok = m_Cur.Create();
            Debug.Assert(ok);

            m_Next = new RenderTexture(width, height, 0)
            {
                filterMode = FilterMode.Bilinear,
                format = RenderTextureFormat.ARGBHalf,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = true,
            };
            ok = m_Next.Create();
            Debug.Assert(ok);

            m_Temp1 = new RenderTexture(width, height, 0)
            {
                filterMode = FilterMode.Bilinear,
                format = RenderTextureFormat.ARGBHalf,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = true,
            };
            ok = m_Temp1.Create();
            Debug.Assert(ok);

            m_Temp2 = new RenderTexture(width, height, 0)
            {
                filterMode = FilterMode.Bilinear,
                format = RenderTextureFormat.ARGBHalf,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = true,
            };
            ok = m_Temp2.Create();
            Debug.Assert(ok);
        }

        private void DispatchShader(int kernelIndex, RenderTexture targetRT)
        {
            var threadGroupsX = Mathf.CeilToInt(targetRT.width / 8.0f);
            var threadGroupsY = Mathf.CeilToInt(targetRT.height / 8.0f);
            m_BlurShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);
        }

        private RenderTexture m_Temp1;
        private RenderTexture m_Temp2;
        private RenderTexture m_Cur;
        private RenderTexture m_Next;
        private readonly ComputeShader m_BlurShader;
        private int m_BlendKernel;
        private int m_HorizontalKernel;
        private int m_VerticalKernel;
    }
}