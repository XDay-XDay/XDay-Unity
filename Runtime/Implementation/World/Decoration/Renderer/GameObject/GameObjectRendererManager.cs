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
using UnityEngine.Pool;
using XDay.UtilityAPI;
using System.Collections.Generic;

namespace XDay.WorldAPI.Decoration
{
    internal class GameObjectRendererManager
    {
        public GameObjectRendererManager(GameObject root, IGameObjectPool pool)
        {
            m_Root = root;
            m_GameObjectPool = pool;
        }

        public void OnDestroy()
        {
            foreach (var kv in m_Renderers)
            {
                kv.Value.OnDestroy();
            }
        }

        public void CreateRenderer(DecorationObject decoration)
        {
            m_Renderers.TryGetValue(decoration.ID, out var renderer);
            if (renderer != null)
            {
                renderer.AddRef();
            }
            else
            {
                var gameObject = m_GameObjectPool.Get(decoration.Path);
                var transform = gameObject.transform;
                transform.localScale = decoration.Scale;
                transform.SetLocalPositionAndRotation(decoration.Position, decoration.Rotation);
                transform.SetParent(m_Root.transform);

                renderer = m_RendererPool.Get();
                renderer.Init(gameObject);
                m_Renderers[decoration.ID] = renderer;
            }
        }

        public void DestroyRenderer(DecorationObject decoration)
        {
            if (m_Renderers.TryGetValue(decoration.ID, out var renderer))
            {
                if (renderer.ReleaseRef())
                {
                    m_RendererPool.Release(renderer);
                    m_GameObjectPool.Release(decoration.Path, renderer.GameObject);
                    renderer.Uninit();
                    m_Renderers.Remove(decoration.ID);
                }
            }
        }

        private readonly GameObject m_Root;
        private readonly ObjectPool<GameObjectRenderer> m_RendererPool = new(createFunc: () =>
        {
            return new GameObjectRenderer();
        }, defaultCapacity: 200);
        private readonly IGameObjectPool m_GameObjectPool;
        private readonly Dictionary<int, GameObjectRenderer> m_Renderers = new();
    }
}

//XDay