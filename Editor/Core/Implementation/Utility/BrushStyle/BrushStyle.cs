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
using UnityEngine;

namespace XDay.UtilityAPI.Editor
{
    internal partial class BrushStyle : IBrushStyle
    {
        public BrushStyle(Texture2D texture, TextureRotation textureRotation)
        {
            m_TextureRotation = textureRotation;
            Recreate(texture);
        }

        public void OnDestroy(bool destroy)
        {            
            if (destroy)
            {
                Helper.DestroyUnityObject(m_OriginalTexture);
                m_OriginalTexture = null;
            }

            m_Original?.OnDestroy();
            m_Original = null;
            m_Rotated?.OnDestroy();
            m_Rotated = null;
        }

        public Texture2D GetTexture(bool rotated)
        {
            return rotated ? m_Rotated.Texture : m_Original.Texture;
        }

        public void Blur(int passCount)
        {
            if (m_Original.Texture == null)
            {
                Debug.LogError("blur failed, invalid texture");
                return;
            }

            var texture = EditorHelper.CreateReadableTexture(m_Original.Texture);
            new TextureBlur().Blur(passCount, new List<Texture2D>() { texture });
            Recreate(texture);
            Helper.DestroyUnityObject(texture);
        }

        public void UpdateRotation(TextureRotation rotation, float yRotation, bool onlyAlpha)
        {
            m_Rotated?.OnDestroy();
            var rotatedTexture = rotation.Rotate(yRotation, m_Original.Texture, onlyAlpha);
            m_Rotated = new TextureMipmaps(rotatedTexture);
            Object.DestroyImmediate(rotatedTexture);
        }

        public int CalculateMipMapLevel(bool rotated, int textureSize)
        {
            var mipmaps = GetMipmaps(rotated);
            var closestSize = Mathf.ClosestPowerOfTwo(Mathf.Max(32, textureSize));
            for (var i = 0; i < mipmaps.MipMapData.Length; ++i)
            {
                if (CalculateTextureMipResolution(mipmaps, i) == closestSize)
                {
                    return i;
                }
            }
            return 0;
        }

        public float TextureLODPointAlpha(bool rotated, float u, float v, int mipLevel)
        {
            var mipmaps = GetMipmaps(rotated);
            var resolution = CalculateTextureMipResolution(mipmaps, mipLevel);
            var x = Mathf.FloorToInt(u * (resolution - 1));
            var y = Mathf.FloorToInt(v * (resolution - 1));
            if (x < 0 || x >= resolution || y < 0 || y >= resolution)
            {
                return 0;
            }
            return mipmaps.MipMapData[mipLevel][y * resolution + x].a;
        }

        public float TextureLODLinearAlpha(bool rotated, float u, float v, int mipLevel)
        {
            var mipmaps = GetMipmaps(rotated);
            var resolution = CalculateTextureMipResolution(mipmaps, mipLevel);
            var resolutionm1 = resolution - 1;
            var x = (int)(u * resolutionm1);
            var y = (int)(v * resolutionm1);

            if (x < 0 || x >= resolution || y < 0 || y >= resolution)
            {
                return 0;
            }
            
            var cx = Mathf.Min(Mathf.CeilToInt(u * resolutionm1), resolutionm1);
            var cy = Mathf.Min(Mathf.CeilToInt(v * resolutionm1), resolutionm1);
            var ru = u - (int)u;
            var rv = v - (int)v;

            return Mathf.Lerp(
                Mathf.Lerp(
                    mipmaps.MipMapData[mipLevel][y * resolution + x].a,
                    mipmaps.MipMapData[mipLevel][y * resolution + cx].a, 
                    ru),
                Mathf.Lerp(
                    mipmaps.MipMapData[mipLevel][cy * resolution + x].a,
                    mipmaps.MipMapData[mipLevel][cy * resolution + cx].a, 
                    ru), 
                rv);
        }

        public Color TextureLODPoint(bool rotated, float u, float v, int mipLevel)
        {
            var mipmaps = GetMipmaps(rotated);
            var resolution = CalculateTextureMipResolution(mipmaps, mipLevel);
            var x = Mathf.FloorToInt(u * (resolution - 1));
            var y = Mathf.FloorToInt(v * (resolution - 1));
            if (x < 0 || x >= resolution || y < 0 || y >= resolution)
            {
                return m_BackgroundColor;
            }
            return mipmaps.MipMapData[mipLevel][y * resolution + x];
        }

        public Color TextureLODLinear(bool rotated, float u, float v, int mipLevel)
        {
            var mipmaps = GetMipmaps(rotated);
            var resolution = CalculateTextureMipResolution(mipmaps, mipLevel);
            var resolutionm1 = resolution - 1;
            var x = (int)(u * resolutionm1);
            var y = (int)(v * resolutionm1);
            if (x < 0 || x >= resolution || y < 0 || y >= resolution)
            {
                return m_BackgroundColor;
            }
            var cx = Mathf.Min(Mathf.CeilToInt(u * resolutionm1), resolutionm1);
            var cy = Mathf.Min(Mathf.CeilToInt(v * resolutionm1), resolutionm1);
            var ru = u - (int)u;
            var rv = v - (int)v;
            return Color.Lerp(
                Color.Lerp(
                    mipmaps.MipMapData[mipLevel][y * resolution + x],
                    mipmaps.MipMapData[mipLevel][y * resolution + cx],
                    ru),
                Color.Lerp(
                    mipmaps.MipMapData[mipLevel][cy * resolution + x],
                    mipmaps.MipMapData[mipLevel][cy * resolution + cx],
                    ru),
                rv);
        }

        public void RemoveBlur()
        {
            Recreate(m_OriginalTexture);
        }

        private TextureMipmaps GetMipmaps(bool rotated)
        {
            return rotated ? m_Rotated : m_Original;
        }

        private void Recreate(Texture2D texture)
        {
            Debug.Assert(texture != null && texture.isReadable);

            OnDestroy(false);

            m_Original = new TextureMipmaps(texture);
            if (m_OriginalTexture == null)
            {
                m_OriginalTexture = Object.Instantiate(m_Original.Texture);
            }
            UpdateRotation(m_TextureRotation, 0, true);
        }

        private int CalculateTextureMipResolution(TextureMipmaps mipmaps, int mipLevel)
        {
            return Mathf.FloorToInt(Mathf.Sqrt(mipmaps.MipMapData[mipLevel].Length));
        }

        private Color m_BackgroundColor = new(0, 0, 0, 0);
        private Texture2D m_OriginalTexture;
        private TextureMipmaps m_Original;
        private TextureMipmaps m_Rotated;
        private readonly TextureRotation m_TextureRotation;
    }
}

//XDay