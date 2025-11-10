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

//#define ENABLE_TEXTURE_LOAD

using System.Collections.Generic;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile
{
    internal class MaskTextureManager
    {
        public MaskTextureManager(ITextureLoader loader)
        {
            m_Loader = loader;
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_PlaceholderMask);
            m_PlaceholderMask = null;
        }

        public void LoadMask(Material material, GameObject tileGameObject, string texturePath)
        {
#if ENABLE_TEXTURE_LOAD
            m_LoadingItems.Add(new LoadItem() { MaskTexturePath = texturePath, Material = material, TileGameObject = tileGameObject });
#endif
        }

        public void UnloadMask(Material material)
        {
#if ENABLE_TEXTURE_LOAD
            var texture = material.GetTexture(m_MaskTexturePropertyName);
            if (texture != m_PlaceholderMask)
            {
                m_Loader.Unload(texture);
                material.SetTexture(m_MaskTexturePropertyName, m_PlaceholderMask);
            }
#endif
        }

        public void Update()
        {
#if ENABLE_TEXTURE_LOAD
            foreach (var item in m_LoadingItems)
            {
                m_Loader.Load(item.MaskTexturePath, item.OnTextureLoaded);
            }
            m_LoadingItems.Clear();
#endif
        }

        private class LoadItem
        {
            public Material Material;
            public GameObject TileGameObject;
            public string MaskTexturePath;

            public void OnTextureLoaded(Texture2D texture)
            {
                Material.SetTexture(m_MaskTexturePropertyName, texture);
            }
        }

        private List<LoadItem> m_LoadingItems = new();
        private Texture2D m_PlaceholderMask;
        private const string m_MaskTexturePropertyName = "_SplatMask";
        private ITextureLoader m_Loader;
    }
}
