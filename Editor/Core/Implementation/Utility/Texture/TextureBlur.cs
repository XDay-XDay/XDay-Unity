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
using System.Threading.Tasks;
using UnityEngine;

namespace XDay.UtilityAPI.Editor
{
    internal class TextureBlur
    {
        public void Blur(int passCount, List<Texture2D> textures)
        {
            var payloads = CreatePayloads(textures);
            if (payloads == null)
            {
                return;
            }

            var tasks = new List<Task<Color[]>>();
            for (var i = 0; i < textures.Count; ++i)
            {
                var idx = i;
                tasks.Add(Task.Run(() =>
                {
                    return BlurInternal(passCount, m_KernelWeights.Length / 2, payloads[idx]);
                }));
            }
            Task.WaitAll(tasks.ToArray());

            for (var i = 0; i < tasks.Count; ++i)
            {
                textures[i].SetPixels(tasks[i].Result);
                textures[i].Apply();
            }
        }

        private Color[] BlurInternal(int passCount, int range, Payload payload)
        {
            for (var iter = 0; iter < passCount; ++iter)
            {
                HorizontalBlur(payload.Pixels, range, payload.Width, payload.Height, payload.HorizontalPassData);
                VerticalBlur(payload.HorizontalPassData, range, payload.Width, payload.Height, payload.VerticalPassData);
                (payload.VerticalPassData, payload.Pixels) = (payload.Pixels, payload.VerticalPassData);
            }
            return payload.Pixels;
        }

        private void VerticalBlur(Color[] input, int radius, int width, int height, Color[] output)
        {
            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    for (var i = -radius; i <= radius; ++i)
                    {
                        var k = Mathf.Clamp(y + i, 0, height - 1);
                        if (i == -radius)
                        {
                            output[y * width + x] = input[k * width + x] * m_KernelWeights[i + radius];
                        }
                        else
                        {
                            output[y * width + x] += input[k * width + x] * m_KernelWeights[i + radius];
                        }
                    }
                }
            }
        }

        private void HorizontalBlur(Color[] input, int radius, int width, int height, Color[] output)
        {
            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    for (var i = -radius; i <= radius; ++i)
                    {
                        var k = Mathf.Clamp(x + i, 0, width - 1);

                        if (i == -radius)
                        {
                            output[y * width + x] = input[y * width + k] * m_KernelWeights[i + radius];
                        }
                        else
                        {
                            output[y * width + x] += input[y * width + k] * m_KernelWeights[i + radius];
                        }
                    }
                }
            }
        }

        private List<Payload> CreatePayloads(List<Texture2D> textures)
        {
            var payloads = new List<Payload>();
            foreach (var texture in textures)
            {
                if (!texture.isReadable)
                {
                    Debug.LogError($"{texture.name} is not readable");
                    return null;
                }
                payloads.Add(new Payload(texture.GetPixels(), texture.width, texture.height));
            }
            return payloads;
        }

        private readonly float[] m_KernelWeights = { 0.006f, 0.061f, 0.242f, 0.383f, 0.242f, 0.061f, 0.006f };

        private class Payload
        {
            public Payload(Color[] pixels, int width, int height)
            {
                Pixels = pixels;
                Width = width;
                Height = height;
                HorizontalPassData = new Color[width * height];
                VerticalPassData = new Color[width * height];
            }

            public int Width;
            public int Height;
            public Color[] Pixels;
            public Color[] HorizontalPassData;
            public Color[] VerticalPassData;
        }
    }
}

//XDay