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
    public class FogBlurSlow : FogBlur
    {
        public override Texture Output => m_Output;

        public FogBlurSlow(int horizontalResolution, int verticalResolution)
        {
            m_HorizontalResolution = horizontalResolution;
            m_VerticalResolution = verticalResolution;
            m_Temp = new Color[m_HorizontalResolution * m_VerticalResolution];
            m_Output = new Texture2D(m_HorizontalResolution, m_VerticalResolution, TextureFormat.RGBA32, false);
        }

        public override void OnDestroy()
        {
            Helper.DestroyUnityObject(m_Output);
        }

        public override void Blur(Texture texture, float texelSkip)
        {
            var tex = texture as Texture2D;
            var pixels = tex.GetPixels();

            HorizontalBlur(pixels, m_HorizontalResolution, m_VerticalResolution, m_Temp, texelSkip);
            VerticalBlur(m_Temp, m_HorizontalResolution, m_VerticalResolution, pixels, texelSkip);

            m_Output.SetPixels(pixels);
            m_Output.Apply();
        }

        private void VerticalBlur(Color[] input, int width, int height, Color[] output, float texelSkip)
        {
            var skip = (int)texelSkip;
            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    var sum = new Color(0, 0, 0, 0);
                    var totalWeight = 0.0f;
                    for (var i = 0; i < m_Radius; ++i)
                    {
                        var up = Mathf.Clamp(y - i * skip, 0, height - 1);
                        var down = Mathf.Clamp(y + i * skip, 0, height - 1);
                        var upIdx = up * width + x;
                        var downIdx = down * width + x;
                        sum += input[upIdx] * m_KernelWeights[i];
                        if (i != 0)
                        {
                            sum += input[downIdx] * m_KernelWeights[i];
                        }
                        totalWeight += (i == 0) ? m_KernelWeights[i] : 2 * m_KernelWeights[i];
                    }

                    output[y * width + x] = sum / totalWeight;
                }
            }
        }

        private void HorizontalBlur(Color[] input, int width, int height, Color[] output, float texelSkip)
        {
            var skip = (int)texelSkip;
            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    var sum = new Color(0, 0, 0, 0);
                    var totalWeight = 0.0f;
                    for (var i = 0; i < m_Radius; ++i)
                    {
                        var leftX = Mathf.Clamp(x - i * skip, 0, width - 1);
                        var rightX = Mathf.Clamp(x + i * skip, 0, width - 1);
                        var leftIdx = y * width + leftX;
                        var rightIdx = y * width + rightX;
                        sum += input[leftIdx] * m_KernelWeights[i];
                        if (i != 0)
                        {
                            sum += input[rightIdx] * m_KernelWeights[i];
                        }
                        totalWeight += (i == 0) ? m_KernelWeights[i] : 2 * m_KernelWeights[i];
                    }

                    output[y * width + x] = sum / totalWeight;
                }
            }
        }

        private Color[] m_Temp;
        private Texture2D m_Output;
        private int m_HorizontalResolution;
        private int m_VerticalResolution;
        private const int m_Radius = 5;
        private float[] m_KernelWeights = new float[m_Radius]
        {
            0.227027f,
            0.1945946f,
            0.1216216f,
            0.054054f,
            0.016216f,
        };
    }
}
