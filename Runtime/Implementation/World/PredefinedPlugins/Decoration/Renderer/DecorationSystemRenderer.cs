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
using XDay.RenderingAPI.BRG;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Decoration
{
    internal sealed partial class DecorationSystemRenderer
    {
        public GameObject Root => m_Root;
        public bool IsActive { get => m_Root.activeSelf;  set => m_Root.SetActive(value); }

        public DecorationSystemRenderer(DecorationSystem system)
        {
            m_Root = new GameObject(system.Name);
#if UNITY_EDITOR
            var debugger = m_Root.AddComponent<DecorationSystemDebugger>();
            debugger.Init(system.World);
            debugger.Show = false;
#endif
            m_Root.transform.SetParent(system.World.Root.transform, true);

            m_InstanceRendererManager = new InstanceRenderManager(m_Root.transform, 
                system.World.AssetLoader.Load<GPUBatchInfoRegistry>($"{system.World.GameFolder}/DecorationBatchInfo.asset"),
                system.World.AssetLoader.Load<InstanceAnimatorBatchInfoRegistry>($"{system.World.GameFolder}/DecorationInstanceBatchInfo.asset"));
            m_GameObjectRendererManager = new GameObjectRendererManager(m_Root, system.World.GameObjectPool);

#if DECORATION_DEBUG
            var drawBounds = m_RootGameObject.AddComponent<DrawBounds>();
            drawBounds.Init(system);
#endif
        }

        public void OnDestroy()
        {
            m_GameObjectRendererManager.OnDestroy();
            m_InstanceRendererManager.OnDestroy();
            Helper.DestroyUnityObject(m_Root);
        }
        
        public void Update()
        {
            m_InstanceRendererManager.Update();
        }

        public void DrawGridGizmo(DecorationSystem system)
        {
            for (var y = 0; y < system.YGridCount; ++y)
            {
                for (var x = 0; x < system.XGridCount; ++x)
                {
                    Gizmos.DrawWireCube(system.CoordinateToCenterPosition(x, y), new Vector3(system.GridWidth, 0, system.GridHeight));
                }
            }
        }

        public void ToggleActiveState(DecorationObject decoration)
        {
            if (decoration.IsActive)
            {
                CreateRenderer(decoration);
            }
            else
            {
                DestroyRenderer(decoration);
            }
        }

        public void PlayAnimation(int id, string name, bool alwaysPlay)
        {
            m_InstanceRendererManager.PlayAnimation(id, name, alwaysPlay);
        }

        private void DestroyRenderer(DecorationObject decoration)
        {
            if (decoration.GPUBatchID < 0)
            {
                m_GameObjectRendererManager.DestroyRenderer(decoration);
            }
            else
            {
                m_InstanceRendererManager.DestroyRenderer(decoration);
            }
        }

        private void CreateRenderer(DecorationObject decoration)
        {
            if (decoration.GPUBatchID < 0)
            {
                m_GameObjectRendererManager.CreateRenderer(decoration);
            }
            else
            {
                m_InstanceRendererManager.CreateRenderer(decoration);
            }
        }

        private readonly GameObject m_Root;
        private readonly GameObjectRendererManager m_GameObjectRendererManager;
        private readonly InstanceRenderManager m_InstanceRendererManager;
    }
}

//XDay