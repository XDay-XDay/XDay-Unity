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

namespace XDay.WorldAPI.Decoration.Editor
{
    internal partial class DecorationSystemRenderer
    {
        public GameObject Root => m_Root;

        public DecorationSystemRenderer(Transform parent, IGameObjectPool pool, DecorationSystem system)
        {
            m_Root = new GameObject(system.Name);
            m_Root.transform.SetParent(parent, true);
            Selection.activeGameObject = m_Root;
            m_Pool = pool;
            m_System = system;
        }

        public void OnDestroy()
        {
            foreach (var obj in m_GameObjects.Values) 
            {
                Helper.DestroyUnityObject(obj);
            }
            Helper.DestroyUnityObject(m_Root);
        }

        public GameObject QueryGameObject(int objectID)
        {
            m_GameObjects.TryGetValue(objectID, out var gameObject);
            return gameObject;
        }

        public void UpdateObjectLOD(int objectID, int lod)
        {
            var gameObject = QueryGameObject(objectID);
            if (gameObject != null)
            {
                var decoration = m_System.World.QueryObject<DecorationObject>(objectID);
                SetLayer(decoration, gameObject, lod);
            }
        }

        public void SetAspect(int objectID, string name)
        {
            var decoration = m_System.World.QueryObject<DecorationObject>(objectID);

            if (name == DecorationDefine.ENABLE_DECORATION_NAME)
            {
                if (decoration.GetVisibility() == WorldObjectVisibility.Visible)
                {
                    ToggleVisibility(decoration, 0);
                }
                return;
            }

            if (name == DecorationDefine.ROTATION_NAME)
            {
                QueryGameObject(objectID).transform.rotation = decoration.Rotation;
                return;
            }

            if (name == DecorationDefine.SCALE_NAME)
            {
                QueryGameObject(objectID).transform.localScale = decoration.Scale;
                return;
            }

            if (name == DecorationDefine.POSITION_NAME)
            {
                QueryGameObject(objectID).transform.position = decoration.Position;
                return;
            }

            if (name == DecorationDefine.LOD_LAYER_MASK_NAME)
            {
                SetLayer(decoration, QueryGameObject(objectID), m_System.ActiveLOD);
                return;
            }

            Debug.Assert(false, $"OnSetAspect todo: {name}");
        }

        public void ToggleVisibility(DecorationObject obj, int lod)
        {
            if (obj.IsActive)
            {
                Create(obj, lod);
            }
            else
            {
                Destroy(obj, lod, false);
            }
        }

        public void Destroy(DecorationObject data, int lod, bool destroyGameObject)
        {
            if (m_GameObjects.TryGetValue(data.ID, out var gameObject))
            {
                Helper.Traverse(gameObject.transform, true, (obj) =>
                {
                    obj.gameObject.DestroyComponent<DecorationObjectChild>();
                });

                if (destroyGameObject)
                {
                    Helper.DestroyUnityObject(gameObject);
                }
                else
                {
                    ReleaseToPool(data, lod, gameObject);
                }
                m_GameObjects.Remove(data.ID);
            }
        }

        public void Create(DecorationObject decoration, int lod)
        {
            if (m_GameObjects.ContainsKey(decoration.ID))
            {
                return;
            }

            var gameObject = GetFromPool(decoration, lod);
            gameObject.AddOrGetComponent<NoKeyDeletion>();
            var transform = gameObject.transform;
            transform.localScale = decoration.Scale;
            transform.SetLocalPositionAndRotation(decoration.Position, decoration.Rotation);
            transform.SetParent(m_Root.transform, false);

            m_GameObjects.Add(decoration.ID, gameObject);
            var listener = gameObject.AddOrGetComponent<DecorationObjectBehaviour>();
            listener.Init(
                decoration.ID,
                getEnableHeightAdjust: (objectID) => 
                {
                    var dec = m_System.QueryObjectUndo(objectID) as DecorationObject;
                    return dec.EnableHeightAdjust;
                },
                setEnableHeightAdjust: (objectID, enable) =>
                {
                    var dec = m_System.QueryObjectUndo(objectID) as DecorationObject;
                    dec.EnableHeightAdjust = enable;
                },
                getEnableInstanceRendering: (objectID) =>
                {
                    var dec = m_System.QueryObjectUndo(objectID) as DecorationObject;
                    return dec.EnableInstanceRendering;
                },
                setEnableInstanceRendering: (objectID, enable) =>
                {
                    var dec = m_System.QueryObjectUndo(objectID) as DecorationObject;
                    dec.EnableInstanceRendering = enable;
                },
                transformChangeCallback: (objectID) => {
                m_System.NotifyObjectDirty(decoration.ID);
            });
            Helper.Traverse(gameObject.transform, true, (obj) =>
            {
                obj.gameObject.AddComponent<DecorationObjectChild>();
            });

            UpdateObjectLOD(decoration.ID, m_System.ActiveLOD);
        }

        private void SetLayer(DecorationObject decoration, GameObject obj, int lod)
        {
            var layer = 0;
            if (decoration.GetVisibility() == WorldObjectVisibility.Visible &&
                !decoration.ExistsInLOD(lod))
            {
                layer = LayerMask.NameToLayer(DecorationDefine.LOD_LAYER_LAYER_NAME);
                if (layer < 0)
                {
                    Debug.LogError($"Need add \"{DecorationDefine.LOD_LAYER_LAYER_NAME}\" layer");
                }
            }

            SetLayerInternal(obj, layer);
        }

        private void SetLayerInternal(GameObject obj, int layer)
        {
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
            {
                renderer.gameObject.layer = layer;
            }
        }

        private GameObject GetFromPool(DecorationObject decoration, int lod)
        {
            var obj = m_Pool.Get(decoration.ResourceDescriptor.GetPath(lod));
            SetLayer(decoration, obj, lod);
            return obj;
        }

        private void ReleaseToPool(DecorationObject data, int lod, GameObject obj)
        {
            SetLayerInternal(obj, 0);
            m_Pool.Release(data.ResourceDescriptor.GetPath(lod), obj);
        }

        private DecorationSystem m_System;
        private GameObject m_Root;
        private IGameObjectPool m_Pool;
        private Dictionary<int, GameObject> m_GameObjects = new();
    }
}


//XDay