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

namespace XDay.WorldAPI.FOW
{
    internal abstract class BlurBase
    {
        public abstract Texture Output { get; }
        public abstract void Blur(Texture texture, float texelSkip);
        public abstract void OnDestroy();
    }

    internal class BlurCS : BlurBase
    {
        public override Texture Output => m_VertBlur;

        public BlurCS(int horizontalResolution, int verticalResolution, ComputeShader blurShader, bool repeat)
        {
            m_BlurShader = blurShader;

            CreateRenderTextures(horizontalResolution, verticalResolution, repeat);
        }

        public override void OnDestroy()
        {
            Helper.DestroyUnityObject(m_HorizBlur);
            Helper.DestroyUnityObject(m_VertBlur);
        }

        public override void Blur(Texture texture, float texelSkip)
        {
            var horizontalKernel = m_BlurShader.FindKernel("HorizontalBlur");
            var verticalKernel = m_BlurShader.FindKernel("VerticalBlur");

            m_BlurShader.SetFloat("TexelSkip", texelSkip);

            m_BlurShader.SetTexture(horizontalKernel, "InputTex", texture);
            m_BlurShader.SetTexture(horizontalKernel, "OutputTex", m_HorizBlur);
            DispatchShader(horizontalKernel, m_HorizBlur);

            m_BlurShader.SetTexture(verticalKernel, "InputTex", m_HorizBlur);
            m_BlurShader.SetTexture(verticalKernel, "OutputTex", m_VertBlur);

            DispatchShader(verticalKernel, m_VertBlur);
        }

        private void DispatchShader(int kernelIndex, RenderTexture targetRT)
        {
            var threadGroupsX = Mathf.CeilToInt(targetRT.width / 8.0f);
            var threadGroupsY = Mathf.CeilToInt(targetRT.height / 8.0f);
            m_BlurShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);
        }

        private void CreateRenderTextures(int width, int height, bool repeat)
        {
            m_HorizBlur = new RenderTexture(width, height, 0)
            {
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false,
                format = RenderTextureFormat.ARGBHalf,
                filterMode = FilterMode.Bilinear,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
            };
            var ok = m_HorizBlur.Create();
            Debug.Assert(ok);

            m_VertBlur = new RenderTexture(width, height, 0)
            {
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false,
                format = RenderTextureFormat.ARGBHalf,
                filterMode = FilterMode.Bilinear,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
            };
            ok = m_VertBlur.Create();
            Debug.Assert(ok);
        }

        private RenderTexture m_HorizBlur;
        private RenderTexture m_VertBlur;
        private readonly ComputeShader m_BlurShader;
    }
}
