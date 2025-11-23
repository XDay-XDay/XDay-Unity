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

namespace XDay.GUIAPI
{
    internal class DynamicAtlasMgr : Singleton<DynamicAtlasMgr>
    {
        public bool Pause { get; internal set; } = false;
        internal Texture2D ClearTexture => m_ClearTexture;

        [RuntimeInitializeOnLoadMethod]
        private static void RuntimeInit()
        {
            S.Init();
        }

        private void Init()
        {
            ClearAllCache();

            m_SetPool.Add(new DynamicAtlasSet("Set 0"));
            m_SetPool.Add(new DynamicAtlasSet("Set 1"));
            m_SetPool.Add(new DynamicAtlasSet("Set 2"));

            m_Empty = new("DynamicAtlasMgr");
            m_Empty.AddComponent<DynamicAtlasMgrBehaviour>();

            var config = Object.FindAnyObjectByType<DynamicAtlasConfig>();
            if (config == null)
            {
                Log.Instance?.Error($"DynamicAtlasConfig not created!");
            }
            else
            {
                m_ClearTexture = config.ClearTexture;
                Log.Instance?.Assert(m_ClearTexture != null, $"Clear texture not set!");
            }
        }

        public void AddToQueue(UIImage image)
        {
            m_Queue.Add(image);
        }

        public void RemoveFromQueue(UIImage image)
        {
            m_Queue.Remove(image);
        }

        public DynamicAtlas GetDynamicAtlas(DynamicAtlasController atlas, DynamicAtlasGroup group)
        {
            Debug.Assert(atlas != null);

            var set = GetSet(atlas);
            if (set == null)
            {
                if (m_SetPool.Count == 0)
                {
                    set = new DynamicAtlasSet($"Set {++m_SetIndex}");
                }
                else
                {
                    set = m_SetPool[^1];
                    m_SetPool.RemoveAt(m_SetPool.Count - 1);
                }
                m_Sets[atlas] = set;
            }
            return set.GetDynamicAtlas(group, "");
        }

        public SaveTextureData AllocateSaveTextureData()
        {
            if (m_SaveTextureDataList.Count > 0)
            {
                return m_SaveTextureDataList.Pop();
            }
            SaveTextureData data = new SaveTextureData();
            return data;
        }

        public void ReleaseSaveTextureData(SaveTextureData data)
        {
            m_SaveTextureDataList.Add(data);
        }

        public GetTextureData AllocateGetTextureData()
        {
            if (m_GetTextureDataList.Count > 0)
            {
                return m_GetTextureDataList.Pop();
            }
            GetTextureData data = new GetTextureData();
            return data;
        }

        public void ReleaseGetTextureData(GetTextureData data)
        {
            m_GetTextureDataList.Add(data);
        }
		
        public void ClearSet(DynamicAtlasController atlas)
        {
            Debug.Assert(atlas != null);

            var set = GetSet(atlas);
            if (set != null)
            {
                set.Clear();
                m_SetPool.Add(set);
                m_Sets.Remove(atlas);

                var images = atlas.gameObject.GetComponentsInChildren<UIImage>(true);
                foreach (var image in images)
                {
                    image.ResetSprite();
                }
            }
        }

        public void ClearAllCache()
        {
            foreach (var kv in m_Sets)
            {
                kv.Value.Clear();
            }
            m_Sets.Clear();

            foreach (var set in m_SetPool)
            {
                set.OnDestroy();
            }
            m_SetPool.Clear();

            m_GetTextureDataList.Clear();
            m_SaveTextureDataList.Clear();
        }

        private DynamicAtlasSet GetSet(DynamicAtlasController atlas)
        {
            m_Sets.TryGetValue(atlas, out var set);
            return set;
        }

        internal void Update()
        {
            m_Queue.Update();
        }

        internal Dictionary<DynamicAtlasController, DynamicAtlasSet> Sets => m_Sets;

        private readonly Dictionary<DynamicAtlasController, DynamicAtlasSet> m_Sets = new();
        private readonly List<DynamicAtlasSet> m_SetPool = new();
        private readonly List<GetTextureData> m_GetTextureDataList = new();
        private readonly List<SaveTextureData> m_SaveTextureDataList = new();
        private readonly AtlasOperationQueue m_Queue = new();
        private int m_SetIndex = 2;
        private GameObject m_Empty;
        private Texture2D m_ClearTexture;
    }

    public static class ListExtension
    {
        static public T Pop<T>(this List<T> list)
        {
            T result = default(T);
            int index = list.Count - 1;
            if (index >= 0)
            {
                result = list[index];
                list.RemoveAt(index);
                return result;
            }
            return result;
        }
    }
}