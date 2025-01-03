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

namespace XDay.UtilityAPI.Editor
{
    internal partial class BrushStyle
    {
        private class TextureMipmaps
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public Color[][] MipMapData { get; set; }
            public Texture2D Texture { get; set; }

            public TextureMipmaps(Texture2D texture)
            {
                Texture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    alphaIsTransparency = true,
                    name = texture.name,
                };

                Width = texture.width;
                Height = texture.height;

                Debug.Assert(Width == Height);

                var pixels = texture.GetPixels();
                GenerateMipmaps(pixels);

                Texture.SetPixels(pixels);
                Texture.Apply();
            }

            public void OnDestroy()
            {
                Object.DestroyImmediate(Texture);
            }

            private void GenerateMipmaps(Color[] pixels)
            {
                var mipLevelCount = GetMipLevelCount();
                MipMapData = new Color[mipLevelCount][];

                SetMipmap0(pixels);

                SetOtherMipmap(mipLevelCount, pixels);
            }

            private int GetMipLevelCount()
            {
                var n = 0;
                var size = Width;
                while (size > 0)
                {
                    ++n;
                    size /= 2;
                }
                return n;
            }

            private void SetMipmap0(Color[] pixels)
            {
                MipMapData[0] = new Color[Width * Height];
                for (var i = 0; i < Width * Height; ++i)
                {
                    MipMapData[0][i] = pixels[i];
                }
            }

            private void SetOtherMipmap(int mipLevelCount, Color[] pixels)
            {
                var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                tex.SetPixels(pixels);
                tex.Apply();

                var mipMapWidth = Width;
                var mipMapHeight = Height;
                for (var mipLevel = 1; mipLevel < mipLevelCount; ++mipLevel)
                {
                    mipMapWidth /= 2;
                    mipMapHeight /= 2;

                    var scaled = new TextureScale().CreateAndScale(tex, new Vector2Int(mipMapWidth, mipMapHeight), true);
                    var scaledPixels = scaled.GetPixels();
                    Object.DestroyImmediate(tex);
                    tex = scaled;

                    var pixelCount = mipMapWidth * mipMapHeight;
                    MipMapData[mipLevel] = new Color[pixelCount];
                    for (var i = 0; i < pixelCount; ++i)
                    {
                        MipMapData[mipLevel][i] = scaledPixels[i];
                    }
                }

                Object.DestroyImmediate(tex);
            }
        }
    }
}

//XDay