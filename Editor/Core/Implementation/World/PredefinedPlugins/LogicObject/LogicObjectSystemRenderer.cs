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

namespace XDay.WorldAPI.LogicObject.Editor
{
    internal partial class LogicObjectSystemRenderer
    {
        public GameObject Root => m_Root;

        public LogicObjectSystemRenderer(Transform parent, IGameObjectPool pool, LogicObjectSystem system)
        {
            m_Root = new GameObject(system.Name);
            m_Root.transform.SetParent(parent, true);
            Selection.activeGameObject = m_Root;
            m_Pool = pool;
            m_System = system;
        }

        public void OnDestroy()
        {
            foreach (var obj in m_Objects.Values)
            {
                Helper.DestroyUnityObject(obj);
            }

            foreach (var obj in m_Groups.Values)
            {
                Helper.DestroyUnityObject(obj);
            }
            Helper.DestroyUnityObject(m_Root);
        }

        public GameObject QueryObject(int objectID)
        {
            m_Objects.TryGetValue(objectID, out var gameObject);
            return gameObject;
        }

        public GameObject QueryGroup(int groupID)
        {
            m_Groups.TryGetValue(groupID, out var gameObject);
            return gameObject;
        }

        public int QueryGroupID(GameObject gameObject)
        {
            foreach (var kv in m_Groups)
            {
                if (kv.Value == gameObject) 
                {
                    return kv.Key;
                }
            }
            return 0;
        }

        public int QueryObjectID(GameObject gameObject)
        {
            m_ObjectToIDs.TryGetValue(gameObject, out var objectID);
            return objectID;
        }

        public void SetAspect(int objectID, string name)
        {
            var logicObject = m_System.World.QueryObject<LogicObject>(objectID);

            if (name == LogicObjectDefine.ENABLE_LOGIC_OBJECT_NAME)
            {
                if (logicObject.GetVisibility() == WorldObjectVisibility.Visible)
                {
                    ToggleVisibility(logicObject, 0);
                }
                return;
            }

            if (name == LogicObjectDefine.ROTATION_NAME)
            {
                QueryObject(objectID).transform.rotation = logicObject.Rotation;
                return;
            }

            if (name == LogicObjectDefine.SCALE_NAME)
            {
                QueryObject(objectID).transform.localScale = logicObject.Scale;
                return;
            }

            if (name == LogicObjectDefine.POSITION_NAME)
            {
                QueryObject(objectID).transform.position = logicObject.Position;
                return;
            }

            if (name == LogicObjectDefine.CHANGE_LOGIC_OBJECT_GROUP_NAME)
            {
                var group = m_System.World.QueryObject<LogicObjectGroup>(objectID);
                var groupObject = QueryGroup(objectID);
                groupObject.name = group.Name;
                return;
            }

            if (name == LogicObjectDefine.CHANGE_LOGIC_OBJECT_GROUP_VISIBILITY)
            {
                var group = m_System.World.QueryObject<LogicObjectGroup>(objectID);
                ToggleVisibility(group);
                return;
            }

            Debug.Assert(false, $"OnSetAspect todo: {name}");
        }

        public void ToggleVisibility(LogicObject obj, int lod)
        {
            if (obj.IsActive)
            {
                Create(obj, lod, -1);
            }
            else
            {
                Destroy(obj, lod, false);
            }
        }

        public void ToggleVisibility(LogicObjectGroup group)
        {
            var gameObject = QueryGroup(group.ID);
            gameObject.SetActive(group.IsActive);
        }

        public void Destroy(LogicObject data, int lod, bool destroyGameObject)
        {
            if (m_Objects.TryGetValue(data.ID, out var gameObject))
            {
                m_ObjectToIDs.Remove(gameObject);
                if (destroyGameObject)
                {
                    Helper.DestroyUnityObject(gameObject);
                }
                else
                {
                    ReleaseToPool(data, lod, gameObject);
                }
                m_Objects.Remove(data.ID);
            }
        }

        public void DestroyGroup(int groupID)
        {
            var obj = QueryGroup(groupID);
            m_Groups.Remove(groupID);
            Object.DestroyImmediate(obj);
        }

        public void MoveObjectToGroup(int objectID, int newGroupID)
        {
            m_Objects.TryGetValue(objectID, out var gameObject);
            if (gameObject != null)
            {
                gameObject.transform.SetParent(m_Groups[newGroupID].transform);
            }
        }

        public void Create(LogicObject obj, int lod, int objectIndex)
        {
            if (m_Objects.ContainsKey(obj.ID))
            {
                return;
            }

            var gameObject = GetFromPool(obj, lod);
            gameObject.AddOrGetComponent<NoKeyDeletion>();
            gameObject.name = obj.Name;
            var transform = gameObject.transform;
            transform.localScale = obj.Scale;
            transform.SetLocalPositionAndRotation(obj.Position, obj.Rotation);
            var groupObject = QueryGroup(obj.Group.ID);
            transform.SetParent(groupObject.transform, false);
            if (objectIndex >= 0)
            {
                transform.SetSiblingIndex(objectIndex);
            }

            m_Objects.Add(obj.ID, gameObject);
            m_ObjectToIDs.Add(gameObject, obj.ID);
            var listener = gameObject.AddOrGetComponent<LogicObjectBehaviour>();
            listener.Init(obj.ID, (objectID) => {
                m_System.NotifyObjectDirty(obj.ID);
            });
        }

        public void Create(LogicObjectGroup group, int objectIndex)
        {
            var obj = new GameObject(group.Name);
            obj.AddComponent<NoKeyDeletion>();
            obj.transform.SetParent(m_Root.transform, false);
            obj.transform.SetSiblingIndex(objectIndex);
            obj.SetActive(group.IsEnabled());
            m_Groups.Add(group.ID, obj);
        }

        private GameObject GetFromPool(LogicObject obj, int lod)
        {
            if (obj.ResourceDescriptor != null)
            {
                return m_Pool.Get(obj.ResourceDescriptor.GetPath(lod));
            }

            Debug.LogWarning("resource descriptor is null, create sphere instead");
            return GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        private void ReleaseToPool(LogicObject data, int lod, GameObject obj)
        {
            if (data.ResourceDescriptor != null)
            {
                m_Pool.Release(data.ResourceDescriptor.GetPath(lod), obj);
            }
            else
            {
                Debug.LogWarning("resource descriptor is null, destroy game object immediately");
                Object.DestroyImmediate(obj);
            }
        }

        private readonly LogicObjectSystem m_System;
        private readonly GameObject m_Root;
        private readonly IGameObjectPool m_Pool;
        private readonly Dictionary<int, GameObject> m_Objects = new();
        private readonly Dictionary<GameObject, int> m_ObjectToIDs = new();
        private readonly Dictionary<int, GameObject> m_Groups = new();
    }
}
