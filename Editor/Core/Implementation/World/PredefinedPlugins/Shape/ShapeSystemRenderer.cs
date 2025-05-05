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
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Shape.Editor
{
    internal partial class ShapeSystemRenderer
    {
        public GameObject Root => m_Root;

        public ShapeSystemRenderer(Transform parent, ShapeSystem system)
        {
            m_Root = new GameObject(system.Name);
            m_Root.transform.SetParent(parent, true);
            Selection.activeGameObject = m_Root;
            m_System = system;
        }

        public void OnDestroy()
        {
            foreach (var renderer in m_Renderers.Values)
            {
                renderer.OnDestroy();
            }
            Helper.DestroyUnityObject(m_Root);
        }

        public GameObject QueryGameObject(int objectID)
        {
            m_Renderers.TryGetValue(objectID, out var renderer);
            if (renderer != null)
            {
                return renderer.Root;
            }
            return null;
        }

        public void SetAspect(int objectID, string name)
        {
            var shape = m_System.World.QueryObject<ShapeObject>(objectID);

            if (name == ShapeDefine.ENABLE_SHAPE_NAME)
            {
                ToggleVisibility(shape);
                return;
            }

            if (name == ShapeDefine.SHAPE_NAME)
            {
                if (m_Renderers.TryGetValue(objectID, out var renderer))
                {
                    renderer.Root.name = shape.Name;
                }
                return;
            }

            if (name == ShapeDefine.ROTATION_NAME)
            {
                QueryGameObject(objectID).transform.rotation = shape.Rotation;
                return;
            }

            if (name == ShapeDefine.SCALE_NAME)
            {
                QueryGameObject(objectID).transform.localScale = shape.Scale;
                return;
            }

            if (name == ShapeDefine.POSITION_NAME)
            {
                QueryGameObject(objectID).transform.position = shape.Position;
                return;
            }

            if (name == ShapeDefine.COLOR_NAME)
            {
                return;
            }

            Debug.Assert(false, $"OnSetAspect todo: {name}");
        }

        public void Destroy(ShapeObject data)
        {
            if (m_Renderers.TryGetValue(data.ID, out var renderer))
            {
                renderer.OnDestroy();
                m_Renderers.Remove(data.ID);
            }
        }

        public void Create(ShapeObject shape)
        {
            if (m_Renderers.ContainsKey(shape.ID))
            {
                return;
            }

            var renderer = new ShapeRenderer(shape, m_Root.transform);
            m_Renderers.Add(shape.ID, renderer);

            foreach (var kv in m_Renderers)
            {
                var obj = m_System.QueryObjectUndo(kv.Key);
                kv.Value.Root.transform.SetSiblingIndex(obj.ObjectIndex);
            }
        }

        public void ToggleVisibility(ShapeObject obj)
        {
            if (m_Renderers.TryGetValue(obj.ID, out var renderer))
            {
                renderer?.SetActive(obj.IsActive);
            }
            else
            {
                Create(obj);
            }
        }

        public void SetDirty(int objectID)
        {
            if (m_Renderers.TryGetValue(objectID, out var renderer))
            {
                renderer?.SetDirty();
            }
        }

        public int QueryObjectID(GameObject gameObject)
        {
            foreach (var kv in m_Renderers)
            {
                if (kv.Value.Root == gameObject)
                {
                    return kv.Key;
                }
            }
            return 0;
        }

        public void Update()
        {
            foreach (var renderer in m_Renderers.Values)
            {
                renderer.Draw(true);
            }
        }

        private readonly ShapeSystem m_System;
        private readonly GameObject m_Root;
        private readonly Dictionary<int, ShapeRenderer> m_Renderers = new();
    }
}

//XDay